import importlib.util
import pathlib
import struct
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "render_minecraft_map.py"
SPEC = importlib.util.spec_from_file_location("render_minecraft_map", SCRIPT)
RENDERER = None
if SCRIPT.is_file() and SPEC and SPEC.loader:
    RENDERER = importlib.util.module_from_spec(SPEC)
    sys.modules[SPEC.name] = RENDERER
    SPEC.loader.exec_module(RENDERER)


class MinecraftMapRendererTests(unittest.TestCase):
    def require_renderer(self):
        self.assertIsNotNone(RENDERER, "Minecraft world map renderer is not implemented yet")
        return RENDERER

    def test_padded_palette_values_are_decoded_at_the_right_slots(self):
        renderer = self.require_renderer()
        words = [(3 << 0) | (17 << 9) | (257 << 18)]

        self.assertEqual(renderer.decode_packed_value(words, 0, 9), 3)
        self.assertEqual(renderer.decode_packed_value(words, 1, 9), 17)
        self.assertEqual(renderer.decode_packed_value(words, 2, 9), 257)

    def test_contiguous_palette_value_can_cross_a_long_boundary(self):
        renderer = self.require_renderer()
        value = 23
        words = [((value & 0xF) << 60), value >> 4]

        self.assertEqual(renderer.decode_packed_value(words, 12, 5, padded=False), value)

    def test_sign_text_keeps_only_visible_text_not_click_commands(self):
        renderer = self.require_renderer()
        value = '{"text":"Ragni","extra":[{"text":" Gate","clickEvent":{"action":"run_command","value":"/op"}}]}'

        self.assertEqual(renderer.sanitize_sign_text(value), "Ragni Gate")

    def test_zip_member_validation_rejects_traversal_and_accepts_world_region(self):
        renderer = self.require_renderer()

        self.assertFalse(renderer.is_safe_zip_member("world/../../outside.txt"))
        self.assertFalse(renderer.is_safe_zip_member("C:/temp/world/level.dat"))
        self.assertTrue(renderer.is_safe_zip_member("world/region/r.-1.0.mca"))

    def test_region_header_rejects_sector_overlap_and_out_of_file_offsets(self):
        renderer = self.require_renderer()
        region = bytearray(3 * 4096)
        struct.pack_into(">I", region, 0, (2 << 8) | 1)
        struct.pack_into(">I", region, 4, (2 << 8) | 1)

        with self.assertRaises(ValueError):
            renderer.validate_region_header(region)

        struct.pack_into(">I", region, 4, (8 << 8) | 1)
        with self.assertRaises(ValueError):
            renderer.validate_region_header(region)

    def test_nbt_parser_rejects_truncated_compound(self):
        renderer = self.require_renderer()

        with self.assertRaises(ValueError):
            renderer.parse_nbt(bytes.fromhex("0a0000030001780000"))

    def test_skipping_byte_array_consumes_length_prefix_and_payload(self):
        renderer = self.require_renderer()
        reader = renderer.NBTReader(struct.pack(">i", 3) + b"abc")

        reader.skip_payload(7)

        self.assertEqual(reader.offset, 7)

    def test_empty_block_entity_list_accepts_tag_end_element_type(self):
        renderer = self.require_renderer()
        reader = renderer.NBTReader(bytes.fromhex("0000000000"))

        self.assertEqual(renderer._block_entities(reader, 9, 0), [])

    def test_empty_section_list_accepts_tag_end_element_type(self):
        renderer = self.require_renderer()
        reader = renderer.NBTReader(bytes.fromhex("0000000000"))

        self.assertEqual(renderer._sections(reader, 9, 0), [])

    def test_sign_marker_uses_front_label_and_ignores_non_sign_entities(self):
        renderer = self.require_renderer()
        sign = {"id": "minecraft:oak_sign", "x": -2, "y": 64, "z": 9,
                "front_text": ['{"text":"Ragni Gate"}']}

        marker = renderer._marker_from_entity(sign)

        self.assertEqual((marker["x"], marker["y"], marker["z"], marker["label"]), (-2, 64, 9, "Ragni Gate"))
        self.assertIsNone(renderer._marker_from_entity({**sign, "id": "minecraft:chest"}))

    def test_incomplete_carvers_chunk_without_surface_heightmap_is_ignored(self):
        renderer = self.require_renderer()

        def name(value):
            encoded = value.encode("utf-8")
            return struct.pack(">H", len(encoded)) + encoded

        nbt = (b"\x0a\x00\x00" + b"\x03" + name("xPos") + struct.pack(">i", -9)
               + b"\x03" + name("zPos") + struct.pack(">i", -160)
               + b"\x08" + name("Status") + name("minecraft:carvers")
               + b"\x0a" + name("Heightmaps") + b"\x00"
               + b"\x09" + name("sections") + b"\x00\x00\x00\x00\x00"
               + b"\x09" + name("block_entities") + b"\x00\x00\x00\x00\x00"
               + b"\x00")

        chunk = renderer.parse_chunk(nbt)

        self.assertEqual(chunk["Status"], "minecraft:carvers")
        self.assertEqual(renderer._sample_chunk(chunk), [])

    def test_manifest_includes_only_in_bounds_spawn_and_whitelisted_fields(self):
        renderer = self.require_renderer()
        bounds = {"minX": -16, "maxX": 15, "minZ": -16, "maxZ": 15}
        signs = [{"x": 0, "y": 64, "z": 1, "label": "Ragni Gate"}]

        manifest = renderer.build_manifest(
            bounds=bounds,
            block_scale=8,
            width=4,
            height=4,
            spawn={"x": 2, "z": -3},
            signs=signs,
        )

        self.assertEqual(manifest["spawn"], {"x": 2, "z": -3, "label": "Điểm xuất hiện"})
        self.assertEqual(manifest["markers"][0]["label"], "Ragni Gate")
        self.assertEqual((manifest["markers"][0]["x"], manifest["markers"][0]["y"], manifest["markers"][0]["z"]), (0, 64, 1))
        self.assertNotIn("seed", manifest)
        self.assertNotIn("playerdata", manifest)

        outside = renderer.build_manifest(
            bounds=bounds,
            block_scale=8,
            width=4,
            height=4,
            spawn={"x": 100, "z": 100},
            signs=[],
        )
        self.assertIsNone(outside["spawn"])

    def test_one_block_pixels_sample_each_surface_block(self):
        renderer = self.require_renderer()
        height = 129  # surface Y=64 for a modern overworld whose minimum build Y is -64
        packed_heights = [
            sum(height << (slot * renderer.HEIGHTMAP_BITS) for slot in range(64 // renderer.HEIGHTMAP_BITS))
            for _ in range((256 + 64 // renderer.HEIGHTMAP_BITS - 1) // (64 // renderer.HEIGHTMAP_BITS))
        ]
        chunk = {
            "xPos": 0,
            "zPos": 0,
            "Heightmaps": packed_heights,
            "sections": [{"Y": 4, "block_states": {"palette": ["minecraft:grass_block"]}}],
        }

        pixels = renderer._sample_chunk(chunk)

        self.assertEqual(len(pixels), 256)
        self.assertEqual({(px, pz) for px, pz, _, _ in pixels}, {
            (x, z) for x in range(16) for z in range(16)
        })

    def test_block_raster_preserves_signed_height_and_zero_marks_missing_cells(self):
        renderer = self.require_renderer()
        pixels = [
            (0, 0, -64, (82, 119, 54)),
            (1, 0, 319, (112, 89, 61)),
        ]
        packed = renderer._pack_chunk_samples(pixels)

        terrain, heightmap, minimum_y, maximum_y = renderer._rasterize_block_chunks(
            [(0, 0, packed)], min_x=0, min_z=0, width=3, height=1,
        )

        self.assertEqual((minimum_y, maximum_y), (-64, 319))
        self.assertEqual(struct.unpack_from("<H", heightmap, 0)[0], 1)
        self.assertEqual(struct.unpack_from("<H", heightmap, 2)[0], 384)
        self.assertEqual(struct.unpack_from("<H", heightmap, 4)[0], 0)
        self.assertEqual(terrain[0:4], bytes((82, 119, 54, 255)))
        self.assertEqual(terrain[8:12], bytes((*renderer.MAP_VOID_COLOR, 255)))

    def test_unsampled_raster_cells_use_the_map_void_color_not_transparent_black(self):
        renderer = self.require_renderer()
        rgba = renderer._rasterize_surface({(0, 0): (64, (120, 137, 86))}, 0, 0, 2, 1)

        self.assertEqual(rgba[4:8], bytes((*renderer.MAP_VOID_COLOR, 255)))
        self.assertEqual(rgba[3], 255)


if __name__ == "__main__":
    unittest.main()
