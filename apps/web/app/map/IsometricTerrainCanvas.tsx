'use client';

import {useEffect, useRef} from 'react';
import type {PointerEvent, WheelEvent, KeyboardEvent} from 'react';
import {buildTerrainGrid, buildTerrainMarkerData, clampTerrainZoom, decodeTerrainPick, terrainViewHeight, unprojectTerrainPoint, TERRAIN_ELEVATION, TERRAIN_HEIGHT_EXAGGERATION, TERRAIN_YAW} from '@/lib/world-map-3d';
import type {WorldMapManifest} from '@/lib/world-map';
import type {TerrainMarker} from '@/lib/world-map-3d';
import styles from './world-map.module.css';

type MapPoint = {x: number; z: number; label?: string};
type Props = {
  manifest: WorldMapManifest;
  markers?: TerrainMarker[];
  zoom: number;
  resetToken: number;
  onZoomChange: (zoom: number) => void;
  onSelect: (point: MapPoint) => void;
  onCursor: (point: MapPoint) => void;
  onCenter: (point: MapPoint) => void;
  onStatus: (status: {ready: boolean; error?: string}) => void;
};
type Pan = {x: number; y: number};

const VERTEX_SHADER = `#version 300 es
precision highp float;
precision highp int;
precision highp usampler2D;
in vec2 a_grid;
in float a_markerKind;
uniform usampler2D u_heightmap;
uniform bool u_markerMode;
uniform ivec2 u_mapSize;
uniform vec2 u_canvasSize;
uniform vec2 u_pan;
uniform float u_viewHeight;
uniform float u_minY;
uniform float u_buildMinY;
uniform float u_heightExaggeration;
out vec2 v_grid;
out vec3 v_world;
out float v_markerKind;
void main() {
  ivec2 pixel = ivec2(a_grid + vec2(0.5));
  uint code = texelFetch(u_heightmap, pixel, 0).r;
  float y = code == 0u ? 0.0 : (float(code) - 1.0 + u_buildMinY - u_minY) * u_heightExaggeration;
  vec3 world = vec3(a_grid.x - (float(u_mapSize.x) - 1.0) * 0.5, y,
                    a_grid.y - (float(u_mapSize.y) - 1.0) * 0.5);
  float yaw = ${TERRAIN_YAW};
  float elevation = ${TERRAIN_ELEVATION};
  vec3 right = vec3(cos(yaw), 0.0, -sin(yaw));
  vec3 up = vec3(-sin(elevation) * sin(yaw), cos(elevation), -sin(elevation) * cos(yaw));
  vec3 towardCamera = vec3(cos(elevation) * sin(yaw), sin(elevation), cos(elevation) * cos(yaw));
  vec2 projected = vec2(dot(world, right), dot(world, up));
  float aspect = u_canvasSize.x / u_canvasSize.y;
  float depthRange = float(u_mapSize.x + u_mapSize.y) + u_heightExaggeration * 512.0;
  gl_Position = vec4(2.0 * (projected.x + u_pan.x) / (u_viewHeight * aspect),
                     2.0 * (projected.y + u_pan.y) / u_viewHeight,
                     -dot(world, towardCamera) / depthRange, 1.0);
  if (u_markerMode) gl_PointSize = 24.0;
  v_grid = a_grid;
  v_world = world;
  v_markerKind = a_markerKind;
}`;

const FRAGMENT_SHADER = `#version 300 es
precision highp float;
precision highp int;
precision highp usampler2D;
uniform sampler2D u_terrain;
uniform usampler2D u_heightmap;
uniform ivec2 u_mapSize;
uniform float u_zoom;
uniform bool u_pickMode;
uniform bool u_markerMode;
in vec2 v_grid;
in vec3 v_world;
in float v_markerKind;
out vec4 outColor;
void main() {
  ivec2 pixel = clamp(ivec2(floor(v_grid + vec2(0.5))), ivec2(0), u_mapSize - ivec2(1));
  uint heightCode = texelFetch(u_heightmap, pixel, 0).r;
  if (heightCode == 0u) discard;
  if (u_markerMode) {
    vec2 point = gl_PointCoord - vec2(0.5);
    if (dot(point, point) > 0.25) discard;
    vec3 markerColor = v_markerKind < 0.5 ? vec3(0.91, 0.68, 0.35) : vec3(0.57, 0.70, 0.65);
    outColor = vec4(markerColor, 1.0);
    return;
  }
  if (u_pickMode) {
    uint x = uint(pixel.x), z = uint(pixel.y);
    uint packedHigh = ((x >> 8u) & 15u) | (((z >> 8u) & 15u) << 4u);
    outColor = vec4(float(x & 255u), float(packedHigh), float(z & 255u), 255.0) / 255.0;
    return;
  }
  vec3 color = texelFetch(u_terrain, pixel, 0).rgb;
  vec3 normal = normalize(cross(dFdx(v_world), dFdy(v_world)));
  if (normal.y < 0.0) normal = -normal;
  vec3 lightDirection = normalize(vec3(-0.32, 0.86, -0.39));
  float lighting = 0.68 + 0.34 * max(dot(normal, lightDirection), 0.0);
  vec2 cell = fract(v_grid);
  float edge = min(min(cell.x, 1.0 - cell.x), min(cell.y, 1.0 - cell.y));
  float blockLine = (1.0 - smoothstep(0.025, 0.095, edge)) * clamp((u_zoom - 3.0) / 8.0, 0.0, 0.34);
  outColor = vec4(color * lighting * (1.0 - blockLine), 1.0);
}`;

function compileShader(gl: WebGL2RenderingContext, kind: number, source: string): WebGLShader {
  const shader = gl.createShader(kind);
  if (!shader) throw new Error('Không thể tạo shader WebGL.');
  gl.shaderSource(shader, source);
  gl.compileShader(shader);
  if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
    const message = gl.getShaderInfoLog(shader) || 'Shader không biên dịch được.';
    gl.deleteShader(shader);
    throw new Error(message);
  }
  return shader;
}

function loadImage(url: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () => resolve(image);
    image.onerror = () => reject(new Error('Không tải được ảnh màu địa hình cục bộ.'));
    image.src = url;
  });
}

export default function IsometricTerrainCanvas({manifest, markers = [], zoom, resetToken, onZoomChange, onSelect, onCursor, onCenter, onStatus}: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawRef = useRef<(() => void) | null>(null);
  const pickRef = useRef<((x: number, y: number) => MapPoint | null) | null>(null);
  const panRef = useRef<Pan>({x: 0, y: 0});
  const zoomRef = useRef(zoom);
  const markersRef = useRef(markers);
  const reportCenterRef = useRef<() => void>(() => {});
  const lastCursorPickRef = useRef(0);
  const callbacksRef = useRef({onZoomChange, onSelect, onCursor, onCenter, onStatus});
  const dragRef = useRef<{id: number; x: number; y: number; moved: boolean} | null>(null);

  zoomRef.current = zoom;
  markersRef.current = markers;
  callbacksRef.current = {onZoomChange, onSelect, onCursor, onCenter, onStatus};

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    let disposed = false;
    let observer: ResizeObserver | null = null;
    let program: WebGLProgram | null = null;
    let vertexBuffer: WebGLBuffer | null = null;
    let indexBuffer: WebGLBuffer | null = null;
    let markerBuffer: WebGLBuffer | null = null;
    let terrainTexture: WebGLTexture | null = null;
    let heightTexture: WebGLTexture | null = null;
    let drawScene: (() => void) | null = null;
    let pickPoint: ((x: number, y: number) => MapPoint | null) | null = null;

    const fail = (message: string) => {
      if (!disposed) callbacksRef.current.onStatus({ready: false, error: message});
    };

    async function initialize() {
      try {
        if (!manifest.heightmapUrl || !Number.isInteger(manifest.minY) || !Number.isInteger(manifest.maxY)) {
          throw new Error('Save chưa có heightmap cho chế độ địa hình 3D.');
        }
        if (manifest.width > 4095 || manifest.height > 4095) throw new Error('Kích thước vượt giới hạn chọn tọa độ của thiết bị.');
        const gl = canvas!.getContext('webgl2', {alpha: true, antialias: true, powerPreference: 'high-performance'});
        if (!gl) throw new Error('Thiết bị không hỗ trợ WebGL2; đã chuyển sang bản đồ phẳng.');
        const maximumTextureSize = gl.getParameter(gl.MAX_TEXTURE_SIZE) as number;
        if (manifest.width > maximumTextureSize || manifest.height > maximumTextureSize) {
          throw new Error('Thiết bị không đủ kích thước texture; đã chuyển sang bản đồ phẳng.');
        }

        const [response, image] = await Promise.all([
          fetch(manifest.heightmapUrl, {cache: 'force-cache'}),
          loadImage(manifest.imageUrl),
        ]);
        if (!response.ok) throw new Error('Không tải được heightmap local.');
        if (image.naturalWidth !== manifest.width || image.naturalHeight !== manifest.height) {
          throw new Error('Kích thước texture địa hình không khớp manifest.');
        }
        const heightBytes = await response.arrayBuffer();
        if (heightBytes.byteLength !== manifest.width * manifest.height * 2 || heightBytes.byteLength % 2 !== 0) {
          throw new Error('Dung lượng heightmap không hợp lệ; đã chuyển sang bản đồ phẳng.');
        }
        if (disposed) return;
        const samples = new Uint16Array(heightBytes);
        const validMask = new Uint8Array(samples.length);
        for (let index = 0; index < samples.length; index++) validMask[index] = samples[index] === 0 ? 0 : 1;
        const grid = buildTerrainGrid(manifest.width, manifest.height, validMask);
        if (grid.indices.length === 0) throw new Error('Heightmap không có mặt địa hình hợp lệ.');

        const vertex = compileShader(gl, gl.VERTEX_SHADER, VERTEX_SHADER);
        const fragment = compileShader(gl, gl.FRAGMENT_SHADER, FRAGMENT_SHADER);
        program = gl.createProgram();
        if (!program) throw new Error('Không thể tạo chương trình WebGL.');
        gl.attachShader(program, vertex);
        gl.attachShader(program, fragment);
        gl.linkProgram(program);
        gl.deleteShader(vertex);
        gl.deleteShader(fragment);
        if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(program) || 'Không thể liên kết shader địa hình.');

        vertexBuffer = gl.createBuffer();
        indexBuffer = gl.createBuffer();
        markerBuffer = gl.createBuffer();
        terrainTexture = gl.createTexture();
        heightTexture = gl.createTexture();
        if (!vertexBuffer || !indexBuffer || !markerBuffer || !terrainTexture || !heightTexture) throw new Error('Không cấp phát được bộ nhớ WebGL.');
        gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, grid.positions, gl.STATIC_DRAW);
        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
        gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, grid.indices, gl.STATIC_DRAW);

        gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, 0);
        gl.bindTexture(gl.TEXTURE_2D, terrainTexture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, image);
        gl.bindTexture(gl.TEXTURE_2D, heightTexture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.R16UI, manifest.width, manifest.height, 0, gl.RED_INTEGER, gl.UNSIGNED_SHORT, samples);

        gl.useProgram(program);
        const attribute = gl.getAttribLocation(program, 'a_grid');
        const markerKindAttribute = gl.getAttribLocation(program, 'a_markerKind');
        const uniforms = {
          height: gl.getUniformLocation(program, 'u_heightmap'),
          terrain: gl.getUniformLocation(program, 'u_terrain'),
          mapSize: gl.getUniformLocation(program, 'u_mapSize'),
          canvasSize: gl.getUniformLocation(program, 'u_canvasSize'),
          pan: gl.getUniformLocation(program, 'u_pan'),
          viewHeight: gl.getUniformLocation(program, 'u_viewHeight'),
          minY: gl.getUniformLocation(program, 'u_minY'),
          buildMinY: gl.getUniformLocation(program, 'u_buildMinY'),
          exaggeration: gl.getUniformLocation(program, 'u_heightExaggeration'),
          zoom: gl.getUniformLocation(program, 'u_zoom'),
          pick: gl.getUniformLocation(program, 'u_pickMode'),
          markerMode: gl.getUniformLocation(program, 'u_markerMode'),
        };
        gl.enableVertexAttribArray(attribute);
        gl.vertexAttribPointer(attribute, 2, gl.FLOAT, false, 0, 0);
        gl.uniform1i(uniforms.height, 1);
        gl.uniform1i(uniforms.terrain, 0);
        gl.uniform2i(uniforms.mapSize, manifest.width, manifest.height);
        gl.uniform1f(uniforms.minY, manifest.minY!);
        gl.uniform1f(uniforms.buildMinY, -64);
        gl.uniform1f(uniforms.exaggeration, TERRAIN_HEIGHT_EXAGGERATION);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, terrainTexture);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, heightTexture);
        gl.enable(gl.DEPTH_TEST);
        gl.depthFunc(gl.LEQUAL);
        gl.enable(gl.CULL_FACE);
        gl.clearColor(0.10, 0.15, 0.12, 1);

        const draw = (pick = false) => {
          if (disposed) return;
          const pixelRatio = Math.min(window.devicePixelRatio || 1, 1.5);
          const maximumPixels = 4_000_000;
          const scale = Math.min(pixelRatio, Math.sqrt(maximumPixels / Math.max(1, canvas!.clientWidth * canvas!.clientHeight)));
          const width = Math.max(1, Math.round(canvas!.clientWidth * scale));
          const height = Math.max(1, Math.round(canvas!.clientHeight * scale));
          if (canvas!.width !== width || canvas!.height !== height) {
            canvas!.width = width;
            canvas!.height = height;
          }
          gl.viewport(0, 0, width, height);
          gl.useProgram(program);
          gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
          gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indexBuffer);
          gl.enableVertexAttribArray(attribute);
          gl.vertexAttribPointer(attribute, 2, gl.FLOAT, false, 0, 0);
          gl.activeTexture(gl.TEXTURE0);
          gl.bindTexture(gl.TEXTURE_2D, terrainTexture);
          gl.activeTexture(gl.TEXTURE1);
          gl.bindTexture(gl.TEXTURE_2D, heightTexture);
          const currentZoom = zoomRef.current;
          const viewHeight = terrainViewHeight(manifest.width, manifest.height, width / height,
            currentZoom, manifest.minY!, manifest.maxY!);
          gl.uniform2f(uniforms.canvasSize, width, height);
          gl.uniform2f(uniforms.pan, panRef.current.x, panRef.current.y);
          gl.uniform1f(uniforms.viewHeight, viewHeight);
          gl.uniform1f(uniforms.zoom, currentZoom);
          gl.uniform1i(uniforms.pick, pick ? 1 : 0);
          gl.uniform1i(uniforms.markerMode, 0);
          gl.disableVertexAttribArray(markerKindAttribute);
          gl.vertexAttrib1f(markerKindAttribute, 0);
          gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
          gl.drawElements(gl.TRIANGLES, grid.indices.length, gl.UNSIGNED_INT, 0);
          if (!pick) {
            const markerData = buildTerrainMarkerData(markersRef.current, manifest.bounds);
            if (markerData.length > 0) {
              gl.uniform1i(uniforms.markerMode, 1);
              gl.bindBuffer(gl.ARRAY_BUFFER, markerBuffer);
              gl.bufferData(gl.ARRAY_BUFFER, markerData, gl.DYNAMIC_DRAW);
              gl.enableVertexAttribArray(attribute);
              gl.vertexAttribPointer(attribute, 2, gl.FLOAT, false, 12, 0);
              gl.enableVertexAttribArray(markerKindAttribute);
              gl.vertexAttribPointer(markerKindAttribute, 1, gl.FLOAT, false, 12, 8);
              gl.drawArrays(gl.POINTS, 0, markerData.length / 3);
              gl.disableVertexAttribArray(markerKindAttribute);
              gl.vertexAttrib1f(markerKindAttribute, 0);
            }
            gl.uniform1i(uniforms.markerMode, 0);
          }
        };
        drawScene = draw;

        pickPoint = (clientX, clientY) => {
          const rect = canvas!.getBoundingClientRect();
          const x = Math.min(canvas!.width - 1, Math.max(0, Math.floor((clientX - rect.left) * canvas!.width / rect.width)));
          const y = Math.min(canvas!.height - 1, Math.max(0, canvas!.height - 1 - Math.floor((clientY - rect.top) * canvas!.height / rect.height)));
          gl.clearColor(0, 0, 0, 0);
          draw(true);
          const bytes = new Uint8Array(4);
          gl.readPixels(x, y, 1, 1, gl.RGBA, gl.UNSIGNED_BYTE, bytes);
          gl.clearColor(0.10, 0.15, 0.12, 1);
          draw(false);
          const point = decodeTerrainPick(bytes, manifest.bounds, manifest.width, manifest.height);
          return point ? {...point} : null;
        };

        const resize = () => {
          drawScene?.();
          reportCenterRef.current();
        };
        const reportCenter = () => {
          const rect = canvas!.getBoundingClientRect();
          if (!rect.width || !rect.height) return;
          const point = pickRef.current?.(rect.left + rect.width / 2, rect.top + rect.height / 2);
          callbacksRef.current.onCenter(point ?? unprojectTerrainPoint({x: -panRef.current.x, y: -panRef.current.y},
            manifest.bounds, manifest.minY!, manifest.minY!));
        };
        reportCenterRef.current = reportCenter;
        drawScene = () => draw(false);
        drawRef.current = drawScene;
        pickRef.current = pickPoint;
        observer = new ResizeObserver(resize);
        const resizeTarget = canvas!.parentElement || canvas!;
        observer.observe(resizeTarget);
        resize();
        if (!disposed) callbacksRef.current.onStatus({ready: true});
      } catch (error) {
        fail(error instanceof Error ? error.message : 'Không thể khởi tạo bản đồ 3D.');
      }
    }

    void initialize();
    return () => {
      disposed = true;
      observer?.disconnect();
      drawRef.current = null;
      pickRef.current = null;
      if (program) {
        const gl = canvas!.getContext('webgl2');
        if (gl) {
          if (vertexBuffer) gl.deleteBuffer(vertexBuffer);
          if (indexBuffer) gl.deleteBuffer(indexBuffer);
          if (markerBuffer) gl.deleteBuffer(markerBuffer);
          if (terrainTexture) gl.deleteTexture(terrainTexture);
          if (heightTexture) gl.deleteTexture(heightTexture);
          gl.deleteProgram(program);
        }
      }
    };
  }, [manifest, manifest.bounds, manifest.heightmapUrl, manifest.imageUrl, manifest.maxY, manifest.minY]);

  useEffect(() => {
    drawRef.current?.();
  }, [markers]);

  useEffect(() => {
    drawRef.current?.();
    reportCenterRef.current();
  }, [zoom]);

  useEffect(() => {
    panRef.current = {x: 0, y: 0};
    drawRef.current?.();
    reportCenterRef.current();
  }, [resetToken]);

  function reportCursor(clientX: number, clientY: number) {
    const now = performance.now();
    if (now - lastCursorPickRef.current < 120) return;
    lastCursorPickRef.current = now;
    const point = pickRef.current?.(clientX, clientY);
    if (point) callbacksRef.current.onCursor(point);
  }

  function onPointerDown(event: PointerEvent<HTMLCanvasElement>) {
    if (event.button !== 0) return;
    dragRef.current = {id: event.pointerId, x: event.clientX, y: event.clientY, moved: false};
    event.currentTarget.setPointerCapture(event.pointerId);
  }

  function onPointerMove(event: PointerEvent<HTMLCanvasElement>) {
    const drag = dragRef.current;
    if (drag && drag.id === event.pointerId) {
      const dx = event.clientX - drag.x, dy = event.clientY - drag.y;
      if (Math.abs(dx) + Math.abs(dy) > 3) drag.moved = true;
      if (drag.moved) {
        const canvas = canvasRef.current;
        if (canvas?.clientHeight) {
          const viewHeight = terrainViewHeight(manifest.width, manifest.height, canvas.clientWidth / canvas.clientHeight,
            zoom, manifest.minY ?? 0, manifest.maxY ?? 0);
          const unitsPerPixel = viewHeight / canvas.clientHeight;
          panRef.current = {x: panRef.current.x + dx * unitsPerPixel, y: panRef.current.y - dy * unitsPerPixel};
          drag.x = event.clientX;
          drag.y = event.clientY;
          drawRef.current?.();
        }
      }
    }
    reportCursor(event.clientX, event.clientY);
  }

  function onPointerUp(event: PointerEvent<HTMLCanvasElement>) {
    const drag = dragRef.current;
    if (!drag || drag.id !== event.pointerId) return;
    if (!drag.moved) {
      const point = pickRef.current?.(event.clientX, event.clientY);
      if (point) callbacksRef.current.onSelect(point);
    }
    dragRef.current = null;
    reportCenterRef.current();
  }

  function onWheel(event: WheelEvent<HTMLCanvasElement>) {
    event.preventDefault();
    callbacksRef.current.onZoomChange(clampTerrainZoom(zoom * (event.deltaY < 0 ? 1.25 : 0.8)));
  }

  function onKeyDown(event: KeyboardEvent<HTMLCanvasElement>) {
    const canvas = canvasRef.current;
    if (!canvas) return;
    if (event.key === '+' || event.key === '=' || event.key === 'Add') callbacksRef.current.onZoomChange(clampTerrainZoom(zoom * 1.25));
    else if (event.key === '-' || event.key === 'Subtract') callbacksRef.current.onZoomChange(clampTerrainZoom(zoom / 1.25));
    else if (event.key === 'Home') {
      callbacksRef.current.onZoomChange(1);
      panRef.current = {x: 0, y: 0};
      drawRef.current?.();
      reportCenterRef.current();
    } else if (event.key.startsWith('Arrow')) {
      if (!canvas.clientHeight) return;
      const viewHeight = terrainViewHeight(manifest.width, manifest.height, canvas.clientWidth / canvas.clientHeight,
        zoom, manifest.minY ?? 0, manifest.maxY ?? 0);
      const step = viewHeight * 56 / canvas.clientHeight;
      if (event.key === 'ArrowLeft') panRef.current.x += step;
      if (event.key === 'ArrowRight') panRef.current.x -= step;
      if (event.key === 'ArrowUp') panRef.current.y -= step;
      if (event.key === 'ArrowDown') panRef.current.y += step;
      drawRef.current?.();
      reportCenterRef.current();
    } else return;
    event.preventDefault();
  }

  return <canvas
    ref={canvasRef}
    className={styles.terrainCanvas}
    role="img"
    aria-label="Bản đồ địa hình isometric, camera cố định. Dùng kéo hoặc phím mũi tên để di chuyển, cộng trừ để thu phóng; nhấn địa hình để đọc tọa độ block."
    tabIndex={0}
    onPointerDown={onPointerDown}
    onPointerMove={onPointerMove}
    onPointerUp={onPointerUp}
    onPointerCancel={() => { dragRef.current = null; }}
    onWheel={onWheel}
    onKeyDown={onKeyDown}
  />;
}
