#!/usr/bin/env python3
"""Render a safe, compact top-down atlas from a supplied Java world ZIP."""

from __future__ import annotations

import argparse
import gzip
import json
import math
import pathlib
import re
import struct
import sys
import zipfile
import zlib
from dataclasses import dataclass
from typing import Any


BLOCKS_PER_PIXEL = 1
MIN_BUILD_Y = -64
HEIGHTMAP_BITS = 9
MAP_VOID_COLOR = (39, 48, 43)
MAX_ZIP_ENTRIES = 10_000
MAX_ZIP_BYTES = 1_500_000_000
MAX_ZIP_ENTRY_BYTES = 64 * 1024 * 1024
MAX_NBT_BYTES = 16 * 1024 * 1024
MAX_NBT_DEPTH = 64
MAX_NBT_ARRAY = 2_000_000
MAX_SIGN_MARKERS = 2_500
MAX_RENDER_PIXELS = 40_000_000

BLOCK_COLORS: tuple[tuple[str, tuple[int, int, int]], ...] = (
    ("water", (85, 125, 128)), ("bubble_column", (85, 125, 128)),
    ("ice", (142, 166, 157)), ("snow", (199, 198, 181)),
    ("sand", (183, 155, 105)), ("terracotta", (151, 102, 76)),
    ("gravel", (125, 117, 101)), ("grass", (120, 137, 86)),
    ("moss", (91, 119, 73)), ("leaves", (80, 105, 67)),
    ("sapling", (88, 115, 70)), ("flower", (128, 130, 80)),
    ("fern", (91, 117, 71)), ("dirt", (125, 96, 66)),
    ("mud", (103, 91, 72)), ("wood", (139, 105, 69)),
    ("planks", (143, 111, 73)), ("brick", (138, 83, 65)),
    ("copper", (155, 108, 76)), ("crimson", (122, 74, 68)),
    ("warped", (73, 115, 108)), ("deepslate", (75, 77, 75)),
    ("stone", (126, 122, 111)), ("ore", (133, 122, 103)),
)


class NBTReader:
    def __init__(self, data: bytes):
        if len(data) > MAX_NBT_BYTES:
            raise ValueError("NBT payload exceeds the 16 MiB safety limit")
        self.data, self.offset = data, 0

    def _require(self, length: int) -> None:
        if length < 0 or self.offset + length > len(self.data):
            raise ValueError(f"truncated NBT payload at byte {self.offset}")

    def read(self, fmt: str) -> Any:
        size = struct.calcsize(fmt)
        self._require(size)
        result = struct.unpack_from(fmt, self.data, self.offset)
        self.offset += size
        return result[0] if len(result) == 1 else result

    def read_string(self) -> str:
        size = self.read(">H")
        self._require(size)
        value = self.data[self.offset:self.offset + size].decode("utf-8", "replace")
        self.offset += size
        return value

    def read_array_length(self, item_size: int = 1) -> int:
        length = self.read(">i")
        if length < 0 or length > MAX_NBT_ARRAY:
            raise ValueError(f"invalid NBT array length: {length}")
        self._require(length * item_size)
        return length

    def read_long_array(self) -> list[int]:
        length = self.read_array_length(8)
        values = list(struct.unpack_from(f">{length}q", self.data, self.offset)) if length else []
        self.offset += length * 8
        return values

    def read_payload(self, tag: int, depth: int = 0) -> Any:
        if depth > MAX_NBT_DEPTH:
            raise ValueError("NBT nesting exceeds the safety limit")
        if tag == 1: return self.read(">b")
        if tag == 2: return self.read(">h")
        if tag == 3: return self.read(">i")
        if tag == 4: return self.read(">q")
        if tag == 5: return self.read(">f")
        if tag == 6: return self.read(">d")
        if tag == 7:
            length = self.read_array_length()
            value = self.data[self.offset:self.offset + length]
            self.offset += length
            return value
        if tag == 8: return self.read_string()
        if tag == 9:
            element_tag, length = self.read(">B"), self.read_array_length()
            if element_tag == 0 and length:
                raise ValueError("non-empty NBT list cannot contain TAG_End")
            return [self.read_payload(element_tag, depth + 1) for _ in range(length)]
        if tag == 10:
            values = {}
            while True:
                child_tag = self.read(">B")
                if child_tag == 0: return values
                name = self.read_string()
                values[name] = self.read_payload(child_tag, depth + 1)
        if tag == 11:
            length = self.read_array_length(4)
            values = list(struct.unpack_from(f">{length}i", self.data, self.offset)) if length else []
            self.offset += length * 4
            return values
        if tag == 12: return self.read_long_array()
        raise ValueError(f"unknown NBT tag type: {tag}")

    def skip_payload(self, tag: int, depth: int = 0) -> None:
        if depth > MAX_NBT_DEPTH:
            raise ValueError("NBT nesting exceeds the safety limit")
        sizes = {1: 1, 2: 2, 3: 4, 4: 8, 5: 4, 6: 8}
        if tag in sizes:
            self._require(sizes[tag])
            self.offset += sizes[tag]
        elif tag == 7:
            length = self.read_array_length()
            self.offset += length
        elif tag == 8:
            length = self.read(">H")
            self._require(length)
            self.offset += length
        elif tag == 9:
            element_tag, length = self.read(">B"), self.read_array_length()
            if element_tag == 0 and length:
                raise ValueError("non-empty NBT list cannot contain TAG_End")
            for _ in range(length): self.skip_payload(element_tag, depth + 1)
        elif tag == 10:
            while True:
                child_tag = self.read(">B")
                if child_tag == 0: break
                self.read_string()
                self.skip_payload(child_tag, depth + 1)
        elif tag in (11, 12):
            length = self.read_array_length(4 if tag == 11 else 8)
            self.offset += length * (4 if tag == 11 else 8)
        else:
            raise ValueError(f"unknown NBT tag type: {tag}")

    def read_root(self) -> Any:
        tag = self.read(">B")
        if tag != 10: raise ValueError("NBT root tag must be a compound")
        self.read_string()
        value = self.read_payload(tag)
        if self.offset != len(self.data):
            raise ValueError("unexpected bytes after the NBT root compound")
        return value


def parse_nbt(data: bytes) -> Any:
    """Parse a bounded NBT document, rejecting truncation and invalid tag data."""
    return NBTReader(data).read_root()


def decode_packed_value(words: list[int], index: int, bits: int, padded: bool = True) -> int:
    if index < 0 or bits < 1 or bits > 32:
        raise ValueError("invalid packed-value index or width")
    per_word = 64 // bits if padded else 0
    bit_index = (index % per_word) * bits if padded else index * bits
    word_index = index // per_word if padded else bit_index // 64
    bit_offset, mask = bit_index % 64, (1 << bits) - 1
    if word_index >= len(words): raise ValueError("packed-value index exceeds the long array")
    first = words[word_index] & 0xFFFFFFFFFFFFFFFF
    if bit_offset + bits <= 64: return (first >> bit_offset) & mask
    if word_index + 1 >= len(words): raise ValueError("packed value crosses a missing long")
    second = words[word_index + 1] & 0xFFFFFFFFFFFFFFFF
    return ((first >> bit_offset) | (second << (64 - bit_offset))) & mask


def is_safe_zip_member(name: str) -> bool:
    if not name or "\\" in name or ":" in name: return False
    path = pathlib.PurePosixPath(name)
    return bool(path.parts) and not path.is_absolute() and all(part not in ("", ".", "..") for part in path.parts)


def validate_region_header(data: bytes) -> list[tuple[int, int, int]]:
    if len(data) < 8192 or len(data) % 4096:
        raise ValueError("Anvil region must contain two complete 4096-byte headers and sectors")
    sector_count, entries, occupied = len(data) // 4096, [], {0, 1}
    for index in range(1024):
        location = struct.unpack_from(">I", data, index * 4)[0]
        offset, count = location >> 8, location & 0xFF
        if offset == 0 and count == 0: continue
        if offset < 2 or count == 0 or offset + count > sector_count:
            raise ValueError(f"invalid Anvil sector pointer for chunk slot {index}")
        sectors = set(range(offset, offset + count))
        if occupied.intersection(sectors):
            raise ValueError(f"overlapping Anvil sectors at chunk slot {index}")
        occupied.update(sectors)
        entries.append((index, offset, count))
    return entries


def _compound(reader: NBTReader, handlers: dict[str, Any], depth: int = 0) -> dict[str, Any]:
    values = {}
    while True:
        tag = reader.read(">B")
        if tag == 0: return values
        name = reader.read_string()
        handler = handlers.get(name)
        values[name] = handler(reader, tag, depth + 1) if handler else reader.skip_payload(tag, depth + 1)


def _string(reader: NBTReader, tag: int, depth: int) -> str:
    if tag != 8: raise ValueError("expected NBT string")
    return reader.read_string()


def _int(reader: NBTReader, tag: int, depth: int) -> int:
    if tag != 3: raise ValueError("expected NBT int")
    return reader.read(">i")


def _heightmaps(reader: NBTReader, tag: int, depth: int) -> list[int] | None:
    if tag != 10:
        reader.skip_payload(tag, depth)
        return None
    values = _compound(reader, {
        "WORLD_SURFACE": lambda r, t, d: r.read_long_array() if t == 12 else r.skip_payload(t, d)
    }, depth)
    result = values.get("WORLD_SURFACE")
    return result if isinstance(result, list) else None


def _palette_entry(reader: NBTReader, depth: int) -> str:
    name = _compound(reader, {"Name": _string}, depth).get("Name")
    if not isinstance(name, str): raise ValueError("block palette entry has no block name")
    return name


def _palette(reader: NBTReader, tag: int, depth: int) -> list[str]:
    if tag != 9:
        reader.skip_payload(tag, depth)
        return []
    element_tag, count = reader.read(">B"), reader.read_array_length()
    if element_tag != 10 or count > 4096: raise ValueError("invalid block palette list")
    return [_palette_entry(reader, depth + 1) for _ in range(count)]


def _block_states(reader: NBTReader, tag: int, depth: int) -> dict[str, Any]:
    if tag != 10:
        reader.skip_payload(tag, depth)
        return {"palette": [], "data": []}
    return _compound(reader, {
        "palette": _palette,
        "data": lambda r, t, d: r.read_long_array() if t == 12 else r.skip_payload(t, d)
    }, depth)


def _section(reader: NBTReader, depth: int) -> dict[str, Any]:
    return _compound(reader, {
        "Y": lambda r, t, d: r.read(">b") if t == 1 else r.skip_payload(t, d),
        "block_states": _block_states,
    }, depth)


def _sections(reader: NBTReader, tag: int, depth: int) -> list[dict[str, Any]]:
    if tag != 9:
        reader.skip_payload(tag, depth)
        return []
    element_tag, count = reader.read(">B"), reader.read_array_length()
    if element_tag == 0 and count == 0: return []
    if element_tag != 10 or count > 64: raise ValueError("invalid chunk section list")
    return [_section(reader, depth + 1) for _ in range(count)]


def _sign_text(reader: NBTReader, tag: int, depth: int) -> list[str]:
    if tag != 10:
        reader.skip_payload(tag, depth)
        return []

    def messages(r: NBTReader, child_tag: int, child_depth: int) -> list[str]:
        if child_tag != 9:
            r.skip_payload(child_tag, child_depth)
            return []
        element_tag, count = r.read(">B"), r.read_array_length()
        if element_tag != 8 or count > 16: raise ValueError("unsupported sign message list")
        return [r.read_string() for _ in range(count)]

    result = _compound(reader, {"messages": messages}, depth).get("messages")
    return result if isinstance(result, list) else []


def _block_entity(reader: NBTReader, depth: int) -> dict[str, Any]:
    return _compound(reader, {
        "id": _string, "x": _int, "y": _int, "z": _int, "front_text": _sign_text,
        "Text1": _string, "Text2": _string, "Text3": _string, "Text4": _string,
    }, depth)


def _block_entities(reader: NBTReader, tag: int, depth: int) -> list[dict[str, Any]]:
    if tag != 9:
        reader.skip_payload(tag, depth)
        return []
    element_tag, count = reader.read(">B"), reader.read_array_length()
    if element_tag == 0 and count == 0: return []
    if element_tag != 10 or count > 100_000: raise ValueError("invalid block-entity list")
    return [_block_entity(reader, depth + 1) for _ in range(count)]


def parse_chunk(data: bytes) -> dict[str, Any]:
    reader = NBTReader(data)
    if reader.read(">B") != 10: raise ValueError("chunk NBT root is not a compound")
    reader.read_string()
    chunk = _compound(reader, {
        "xPos": _int, "zPos": _int, "Heightmaps": _heightmaps,
        "sections": _sections, "block_entities": _block_entities, "Status": _string,
    })
    if reader.offset != len(data): raise ValueError("unexpected bytes after chunk NBT")
    if "xPos" not in chunk or "zPos" not in chunk: raise ValueError("chunk does not identify X/Z")
    if not isinstance(chunk.get("Heightmaps"), list):
        if chunk.get("Status") == "minecraft:full":
            raise ValueError("fully generated chunk has no WORLD_SURFACE heightmap")
        chunk["Heightmaps"] = None
    return chunk


def _bounded_decompress(data: bytes, mode: int) -> bytes:
    if mode == 3:
        if len(data) > MAX_NBT_BYTES: raise ValueError("uncompressed chunk exceeds 16 MiB")
        return data
    if mode not in (1, 2): raise ValueError(f"unsupported Minecraft chunk compression type: {mode}")
    decoder = zlib.decompressobj(16 + zlib.MAX_WBITS if mode == 1 else zlib.MAX_WBITS)
    result = decoder.decompress(data, MAX_NBT_BYTES + 1)
    if len(result) > MAX_NBT_BYTES or decoder.unconsumed_tail:
        raise ValueError("decompressed chunk exceeds 16 MiB")
    result += decoder.flush()
    if len(result) > MAX_NBT_BYTES or not decoder.eof: raise ValueError("invalid or oversized compressed chunk")
    return result


def _component_text(value: Any) -> str:
    if isinstance(value, str):
        try: parsed = json.loads(value)
        except (json.JSONDecodeError, TypeError): return value
        if isinstance(parsed, (dict, list)): return _component_text(parsed)
        return _component_text(parsed) if isinstance(parsed, str) and parsed != value else (parsed if isinstance(parsed, str) else "")
    if isinstance(value, list):
        return " ".join(part for part in (_component_text(item) for item in value) if part)
    if isinstance(value, dict):
        own = value.get("text") if isinstance(value.get("text"), str) else ""
        return own + _component_text(value.get("extra", []))
    return ""


def sanitize_sign_text(value: str) -> str:
    text = _component_text(value)
    text = re.sub(r"§[0-9a-fk-or]", "", text, flags=re.IGNORECASE)
    text = " ".join("".join(char for char in text if char.isprintable()).split())
    if len(text) < 2 or len(text) > 80 or text.startswith(("/", "http://", "https://", "www.")):
        return ""
    return text if any(char.isalpha() for char in text) else ""


def block_color(block_name: str) -> tuple[int, int, int]:
    name = block_name.removeprefix("minecraft:").lower()
    for token, color in BLOCK_COLORS:
        if token in name: return color
    return (123, 118, 105)


def _read_height(words: list[int], block_index: int) -> int:
    padded_count = math.ceil(256 / (64 // HEIGHTMAP_BITS))
    continuous_count = math.ceil(256 * HEIGHTMAP_BITS / 64)
    if len(words) not in (padded_count, continuous_count):
        raise ValueError("unsupported WORLD_SURFACE heightmap packing")
    return decode_packed_value(words, block_index, HEIGHTMAP_BITS, len(words) == padded_count)


def _section_block_name(section: dict[str, Any], block_index: int) -> str:
    states = section.get("block_states")
    palette = states.get("palette") if isinstance(states, dict) else None
    if not isinstance(palette, list) or not palette: return "minecraft:air"
    if len(palette) == 1: return palette[0]
    words = states.get("data")
    if not isinstance(words, list) or not words: raise ValueError("multi-entry palette has no packed states")
    bits = max(4, (len(palette) - 1).bit_length())
    padded_count = math.ceil(4096 / (64 // bits))
    continuous_count = math.ceil(4096 * bits / 64)
    if len(words) not in (padded_count, continuous_count): raise ValueError("unsupported block palette packing")
    padded = len(words) == padded_count
    index = decode_packed_value(words, block_index, bits, padded)
    if index >= len(palette): raise ValueError("block palette index is out of range")
    return palette[index]


def _sample_chunk(chunk: dict[str, Any]) -> list[tuple[int, int, int, tuple[int, int, int]]]:
    if not isinstance(chunk.get("Heightmaps"), list): return []
    sections = {part["Y"]: part for part in chunk.get("sections", []) if isinstance(part.get("Y"), int)}
    pixels = []
    sample_positions = range(BLOCKS_PER_PIXEL // 2, 16, BLOCKS_PER_PIXEL)
    for z in sample_positions:
        for x in sample_positions:
            height = _read_height(chunk["Heightmaps"], z * 16 + x)
            if height == 0: continue
            y = MIN_BUILD_Y + height - 1
            section = sections.get(y // 16)
            if section is None: continue
            name = _section_block_name(section, (y & 15) * 256 + z * 16 + x)
            if name.endswith((":air", ":cave_air")): continue
            world_x, world_z = chunk["xPos"] * 16 + x, chunk["zPos"] * 16 + z
            pixels.append((world_x // BLOCKS_PER_PIXEL, world_z // BLOCKS_PER_PIXEL, y, block_color(name)))
    return pixels


def _pack_chunk_samples(pixels: list[tuple[int, int, int, tuple[int, int, int]]]) -> bytes:
    """Pack a chunk's 16x16 surface into 16-bit height codes and RGB bytes."""
    packed = bytearray(16 * 16 * 5)
    for x, z, y, color in pixels:
        height_code = y - MIN_BUILD_Y + 1
        if not 1 <= height_code <= 0xFFFF:
            raise ValueError("surface height cannot be represented in the map height field")
        offset = ((z % 16) * 16 + (x % 16)) * 5
        struct.pack_into("<H", packed, offset, height_code)
        packed[offset + 2:offset + 5] = bytes(color)
    return bytes(packed)


def _rasterize_block_chunks(
    chunks: list[tuple[int, int, bytes]], *, min_x: int, min_z: int, width: int, height: int,
) -> tuple[bytes, bytes, int, int]:
    """Build one RGBA color texel and one compact unsigned height per block."""
    if width < 1 or height < 1 or width * height > MAX_RENDER_PIXELS:
        raise ValueError("invalid block raster dimensions")
    terrain = bytearray(bytes((*MAP_VOID_COLOR, 255)) * width * height)
    heightmap = bytearray(width * height * 2)
    minimum_y, maximum_y = None, None
    for chunk_x, chunk_z, packed in chunks:
        if len(packed) != 16 * 16 * 5:
            raise ValueError("invalid packed chunk surface length")
        for local_z in range(16):
            world_z = chunk_z + local_z
            if not min_z <= world_z < min_z + height:
                continue
            for local_x in range(16):
                offset = (local_z * 16 + local_x) * 5
                height_code = struct.unpack_from("<H", packed, offset)[0]
                if height_code == 0:
                    continue
                world_x = chunk_x + local_x
                if not min_x <= world_x < min_x + width:
                    continue
                y = height_code + MIN_BUILD_Y - 1
                minimum_y = y if minimum_y is None else min(minimum_y, y)
                maximum_y = y if maximum_y is None else max(maximum_y, y)
                pixel = (world_z - min_z) * width + world_x - min_x
                terrain_offset = pixel * 4
                terrain[terrain_offset:terrain_offset + 3] = packed[offset + 2:offset + 5]
                heightmap_offset = pixel * 2
                heightmap[heightmap_offset:heightmap_offset + 2] = packed[offset:offset + 2]
    if minimum_y is None or maximum_y is None:
        raise ValueError("world save contains no renderable overworld surface blocks")
    return bytes(terrain), bytes(heightmap), minimum_y, maximum_y


def _marker_from_entity(entity: dict[str, Any]) -> dict[str, Any] | None:
    identifier = entity.get("id", "")
    if not isinstance(identifier, str) or not identifier.endswith("_sign"): return None
    lines = entity.get("front_text", [])
    if not isinstance(lines, list) or not lines: lines = [entity.get(f"Text{i}", "") for i in range(1, 5)]
    label = sanitize_sign_text(" ".join(line for line in lines if isinstance(line, str)))
    if not label or not all(isinstance(entity.get(k), int) for k in ("x", "y", "z")): return None
    return {"id": f"sign-{entity['x']}-{entity['y']}-{entity['z']}", "x": entity["x"], "y": entity["y"],
            "z": entity["z"], "label": label, "kind": "sign"}


def build_manifest(*, bounds: dict[str, int], block_scale: int, width: int, height: int,
                   spawn: dict[str, int] | None, signs: list[dict[str, Any]]) -> dict[str, Any]:
    if block_scale < 1 or width < 1 or height < 1 or width * height > MAX_RENDER_PIXELS:
        raise ValueError("invalid map dimensions or pixel scale")
    if any(not isinstance(bounds.get(k), int) for k in ("minX", "maxX", "minZ", "maxZ")):
        raise ValueError("map bounds must use integer block coordinates")
    if bounds["minX"] > bounds["maxX"] or bounds["minZ"] > bounds["maxZ"]:
        raise ValueError("map bounds are inverted")
    safe_spawn = None
    if (isinstance(spawn, dict) and isinstance(spawn.get("x"), int) and isinstance(spawn.get("z"), int)
        and bounds["minX"] <= spawn["x"] <= bounds["maxX"]
        and bounds["minZ"] <= spawn["z"] <= bounds["maxZ"]):
        safe_spawn = {"x": spawn["x"], "z": spawn["z"], "label": "Điểm xuất hiện"}
    markers = []
    for marker in signs:
        if not isinstance(marker, dict): continue
        label, x, y, z = sanitize_sign_text(str(marker.get("label", ""))), marker.get("x"), marker.get("y"), marker.get("z")
        if not label or not all(isinstance(v, int) for v in (x, y, z)): continue
        if not (bounds["minX"] <= x <= bounds["maxX"] and bounds["minZ"] <= z <= bounds["maxZ"]): continue
        markers.append({"id": f"sign-{x}-{y}-{z}", "x": x, "y": y, "z": z, "label": label, "kind": "sign"})
    unique = {marker["id"] + "-" + marker["label"]: marker for marker in markers}
    return {
        "version": 1, "imageUrl": "/maps/nova-haven/terrain.png", "blockScale": block_scale,
        "width": width, "height": height, "bounds": bounds,
        "coordinateConvention": {"unit": "block", "xDirection": "right", "zDirection": "down"},
        "spawn": safe_spawn,
        "markers": sorted(unique.values(), key=lambda m: (m["z"], m["x"], m["label"]))[:MAX_SIGN_MARKERS],
    }


def _png_chunk(kind: bytes, payload: bytes) -> bytes:
    return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)


def encode_png(width: int, height: int, rgba: bytes) -> bytes:
    if width < 1 or height < 1 or width * height > MAX_RENDER_PIXELS or len(rgba) != width * height * 4:
        raise ValueError("invalid PNG dimensions or pixel buffer")
    compressor, compressed, stride = zlib.compressobj(level=7), bytearray(), width * 4
    for row in range(height):
        start = row * stride
        compressed.extend(compressor.compress(b"\0" + rgba[start:start + stride]))
    compressed.extend(compressor.flush())
    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + _png_chunk(b"IHDR", header) + _png_chunk(b"IDAT", bytes(compressed)) + _png_chunk(b"IEND", b"")


def _rasterize_surface(samples: dict[tuple[int, int], tuple[int, tuple[int, int, int]]],
                       min_px: int, min_pz: int, width: int, height: int) -> bytes:
    if width < 1 or height < 1 or width * height > MAX_RENDER_PIXELS:
        raise ValueError("invalid terrain raster dimensions")
    void_pixel = bytes((*MAP_VOID_COLOR, 255))
    rgba = bytearray(void_pixel * width * height)

    def neighbor(px: int, pz: int, fallback: int) -> int:
        point = samples.get((px, pz))
        return point[0] if point else fallback

    for (px, pz), (y, color) in samples.items():
        if not min_px <= px < min_px + width or not min_pz <= pz < min_pz + height:
            continue
        slope = neighbor(px - 1, pz, y) - neighbor(px + 1, pz, y)
        slope += neighbor(px, pz - 1, y) - neighbor(px, pz + 1, y)
        shade = max(0.72, min(1.18, 0.94 + slope / 80))
        offset = ((pz - min_pz) * width + px - min_px) * 4
        rgba[offset:offset + 4] = bytes((*[max(0, min(255, round(c * shade))) for c in color], 255))
    return bytes(rgba)


def _discover_world(zip_file: zipfile.ZipFile) -> tuple[str, list[zipfile.ZipInfo]]:
    infos = zip_file.infolist()
    if len(infos) > MAX_ZIP_ENTRIES: raise ValueError("world ZIP contains too many entries")
    total, seen = 0, set()
    for info in infos:
        if not is_safe_zip_member(info.filename): raise ValueError(f"unsafe ZIP path: {info.filename!r}")
        if info.filename in seen: raise ValueError(f"duplicate ZIP path: {info.filename!r}")
        seen.add(info.filename)
        if info.is_dir(): continue
        if info.file_size > MAX_ZIP_ENTRY_BYTES: raise ValueError(f"ZIP entry exceeds 64 MiB: {info.filename}")
        if info.compress_type not in (zipfile.ZIP_STORED, zipfile.ZIP_DEFLATED):
            raise ValueError(f"unsupported ZIP compression in {info.filename}")
        if info.file_size and not info.compress_size: raise ValueError(f"invalid compressed size in {info.filename}")
        if info.compress_size and info.file_size / info.compress_size > 2_000:
            raise ValueError(f"suspicious ZIP compression ratio in {info.filename}")
        total += info.file_size
        if total > MAX_ZIP_BYTES: raise ValueError("world ZIP expands beyond the 1.5 GiB safety limit")
    roots = [name for name in seen if pathlib.PurePosixPath(name).name == "level.dat"]
    if len(roots) != 1: raise ValueError("world ZIP must contain exactly one level.dat")
    prefix = (pathlib.PurePosixPath(roots[0]).parent / "region").as_posix() + "/"
    regions = [info for info in infos if not info.is_dir() and info.filename.startswith(prefix)
               and re.fullmatch(r"r\.-?\d+\.-?\d+\.mca", pathlib.PurePosixPath(info.filename).name)]
    if not regions: raise ValueError("world ZIP has no overworld region/*.mca files beside level.dat")
    return roots[0], sorted(regions, key=lambda item: item.filename)


def _read_level(zip_file: zipfile.ZipFile, name: str) -> tuple[int, int, int]:
    info = zip_file.getinfo(name)
    if info.file_size > 8 * 1024 * 1024: raise ValueError("level.dat exceeds the 8 MiB safety limit")
    try:
        decoder = zlib.decompressobj(16 + zlib.MAX_WBITS)
        data = decoder.decompress(zip_file.read(info), MAX_NBT_BYTES + 1)
        if len(data) > MAX_NBT_BYTES or decoder.unconsumed_tail: raise ValueError("level.dat exceeds 16 MiB")
        data += decoder.flush()
        if len(data) > MAX_NBT_BYTES or not decoder.eof: raise ValueError("invalid or oversized gzip level.dat")
    except zlib.error as error:
        raise ValueError(f"invalid gzip level.dat: {error}") from error
    root = parse_nbt(data)
    world = root.get("Data", root) if isinstance(root, dict) else None
    if not isinstance(world, dict): raise ValueError("level.dat has no world metadata compound")
    try:
        version, spawn_x, spawn_z = int(world["DataVersion"]), int(world["SpawnX"]), int(world["SpawnZ"])
    except (KeyError, TypeError, ValueError) as error:
        raise ValueError("level.dat is missing data version or saved spawn coordinates") from error
    if version < 2860: raise ValueError(f"unsupported legacy world data version: {version}")
    return version, spawn_x, spawn_z


@dataclass
class RenderResult:
    png: bytes
    heightmap: bytes
    manifest: dict[str, Any]
    chunks_rendered: int


def render_world(zip_path: pathlib.Path) -> RenderResult:
    try: archive = zipfile.ZipFile(zip_path)
    except (OSError, zipfile.BadZipFile) as error: raise ValueError(f"cannot read world ZIP: {error}") from error
    with archive as zip_file:
        level_name, regions = _discover_world(zip_file)
        version, spawn_x, spawn_z = _read_level(zip_file, level_name)
        chunk_samples: list[tuple[int, int, bytes]] = []
        min_x = min_z = None
        max_x = max_z = None
        signs: dict[tuple[int, int, int, str], dict[str, Any]] = {}
        chunks_rendered = 0
        for region_info in regions:
            region_bytes = zip_file.read(region_info)
            match = re.fullmatch(r"r\.(-?\d+)\.(-?\d+)\.mca", pathlib.PurePosixPath(region_info.filename).name)
            if not match: continue
            region_x, region_z = int(match.group(1)), int(match.group(2))
            for slot, sector, sectors in validate_region_header(region_bytes):
                start = sector * 4096
                chunk_length = struct.unpack_from(">I", region_bytes, start)[0]
                if chunk_length < 1 or chunk_length > sectors * 4096 - 4:
                    raise ValueError(f"invalid chunk length in {region_info.filename}, slot {slot}")
                compression = region_bytes[start + 4]
                if compression & 0x80: raise ValueError(f"external chunk compression in {region_info.filename}")
                nbt = _bounded_decompress(region_bytes[start + 5:start + 4 + chunk_length], compression)
                chunk = parse_chunk(nbt)
                if chunk["xPos"] != region_x * 32 + slot % 32 or chunk["zPos"] != region_z * 32 + slot // 32:
                    raise ValueError(f"chunk position disagrees with region slot in {region_info.filename}")
                pixels = _sample_chunk(chunk)
                if pixels:
                    chunks_rendered += 1
                    chunk_x, chunk_z = chunk["xPos"] * 16, chunk["zPos"] * 16
                    chunk_samples.append((chunk_x, chunk_z, _pack_chunk_samples(pixels)))
                    for px, pz, _, _ in pixels:
                        min_x = px if min_x is None else min(min_x, px)
                        max_x = px if max_x is None else max(max_x, px)
                        min_z = pz if min_z is None else min(min_z, pz)
                        max_z = pz if max_z is None else max(max_z, pz)
                for entity in chunk.get("block_entities", []):
                    marker = _marker_from_entity(entity)
                    if marker: signs[(marker["x"], marker["y"], marker["z"], marker["label"])] = marker
        if min_x is None or max_x is None or min_z is None or max_z is None:
            raise ValueError("world save contains no renderable overworld surface blocks")
        width, height = max_x - min_x + 1, max_z - min_z + 1
        if width * height > MAX_RENDER_PIXELS: raise ValueError(f"render bounds too large: {width}×{height}")
        rgba, heightmap, minimum_y, maximum_y = _rasterize_block_chunks(
            chunk_samples, min_x=min_x, min_z=min_z, width=width, height=height,
        )
        png = encode_png(width, height, rgba)
        bounds = {"minX": min_x, "maxX": max_x, "minZ": min_z, "maxZ": max_z}
        included_signs = [marker for marker in signs.values()
                          if bounds["minX"] <= marker["x"] <= bounds["maxX"]
                          and bounds["minZ"] <= marker["z"] <= bounds["maxZ"]]
        manifest = build_manifest(bounds=bounds, block_scale=BLOCKS_PER_PIXEL, width=width, height=height,
                                  spawn={"x": spawn_x, "z": spawn_z}, signs=included_signs)
        manifest["dataVersion"], manifest["chunksRendered"] = version, chunks_rendered
        manifest["heightmapUrl"] = "/maps/nova-haven/heightmap.bin"
        manifest["minY"], manifest["maxY"] = minimum_y, maximum_y
        return RenderResult(png, heightmap, manifest, chunks_rendered)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--world-zip", required=True, type=pathlib.Path, help="path to a Minecraft Java world ZIP")
    parser.add_argument("--output-dir", required=True, type=pathlib.Path, help="directory for terrain.png and manifest.json")
    args = parser.parse_args(argv)
    output = args.output_dir.resolve()
    try:
        result = render_world(args.world_zip)
        output.mkdir(parents=True, exist_ok=True)
        image_temp = output / ".terrain.png.tmp"
        heightmap_temp = output / ".heightmap.bin.tmp"
        manifest_temp = output / ".manifest.json.tmp"
        image_temp.write_bytes(result.png)
        heightmap_temp.write_bytes(result.heightmap)
        manifest_temp.write_text(json.dumps(result.manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        image_temp.replace(output / "terrain.png")
        heightmap_temp.replace(output / "heightmap.bin")
        manifest_temp.replace(output / "manifest.json")
    except (OSError, ValueError, zipfile.BadZipFile, struct.error, zlib.error, EOFError) as error:
        for temporary in (output / ".terrain.png.tmp", output / ".heightmap.bin.tmp", output / ".manifest.json.tmp"):
            try: temporary.unlink(missing_ok=True)
            except OSError: pass
        print(f"world map render failed: {error}", file=sys.stderr)
        return 1
    print(f"Rendered {result.chunks_rendered} terrain chunks; {result.manifest['width']}×{result.manifest['height']} pixels, "
          f"{len(result.manifest['markers'])} sign markers, terrain {len(result.png)} bytes, "
          f"height field {len(result.heightmap)} bytes, Y {result.manifest['minY']}…{result.manifest['maxY']}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
