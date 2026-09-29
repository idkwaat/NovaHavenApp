import pathlib
import sys
import tempfile
import unittest
import zipfile
from unittest.mock import patch


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))

from scripts.bluemap_local import (  # noqa: E402
    build_map_config,
    extract_world_from_zip,
    _paths_overlap,
    _quarantine_unused_default_maps,
    parse_java_major,
    require_mojang_download_consent,
    require_java_21,
    resolve_world_path,
    _snapshot_world_directory,
    validate_loopback_origin,
    validate_local_root,
)


class BlueMapLocalTests(unittest.TestCase):
    def test_java_major_parser_accepts_java_21_and_rejects_other_major(self):
        output = 'java version "21.0.11" 2026-04-21 LTS\nJava(TM) SE Runtime Environment'

        self.assertEqual(parse_java_major(output), 21)
        self.assertEqual(parse_java_major('openjdk version "25.0.1" 2026-03-17'), 25)
        self.assertEqual(require_java_21(output), 21)
        with self.assertRaisesRegex(RuntimeError, 'Java 21'):
            require_java_21('openjdk version "25.0.1" 2026-03-17')

    def test_map_config_defaults_to_tilted_perspective_without_free_flight(self):
        config = build_map_config(pathlib.Path(r'C:\Nova Haven\world'), 191, -71)

        self.assertIn('world: "C:/Nova Haven/world"', config)
        self.assertIn('dimension: "minecraft:overworld"', config)
        self.assertIn('name: "Nova Haven"', config)
        self.assertIn('sorting: -100', config)
        self.assertIn('start-pos: { x: 191, z: -71 }', config)
        self.assertIn('enable-flat-view: false', config)
        self.assertIn('enable-hires: true', config)
        self.assertIn('enable-perspective-view: true', config)
        self.assertIn('enable-free-flight-view: false', config)

    def test_mojang_resource_download_requires_explicit_consent_or_existing_opt_in(self):
        with self.assertRaisesRegex(RuntimeError, 'chấp nhận tải tài nguyên Minecraft'):
            require_mojang_download_consent('accept-download: false\n', False)
        require_mojang_download_consent('accept-download: true\n', False)
        require_mojang_download_consent('accept-download: false\n', True)

    def test_web_origin_must_be_plain_http_loopback(self):
        self.assertEqual(validate_loopback_origin('http://127.0.0.1:8100/'), 'http://127.0.0.1:8100')
        self.assertEqual(validate_loopback_origin('http://localhost:8123'), 'http://localhost:8123')
        self.assertEqual(validate_loopback_origin('http://[::1]:8123'), 'http://[::1]:8123')
        for unsafe in (
            'http://192.168.1.2:8100',
            'https://127.0.0.1:8100',
            'http://user@127.0.0.1:8100',
            'http://127.0.0.1:8100/maps',
        ):
            with self.subTest(origin=unsafe), self.assertRaises(ValueError):
                validate_loopback_origin(unsafe)

    def test_local_root_is_confined_to_private_project_storage_even_through_links(self):
        with tempfile.TemporaryDirectory() as temporary:
            repository = pathlib.Path(temporary) / 'repo'
            approved = repository / '.local' / 'bluemap'
            public = repository / 'apps' / 'web' / 'public'
            public.mkdir(parents=True)
            private_cache = repository / '.local' / 'bluemap'
            self.assertTrue(_paths_overlap(private_cache, private_cache / 'world-cache'))
            self.assertTrue(_paths_overlap(private_cache / 'world-cache', private_cache))

            self.assertEqual(validate_local_root(approved, repository), approved.resolve())
            for unsafe in (public, approved / '..' / '..' / '..' / 'apps' / 'web' / 'public'):
                with self.subTest(path=unsafe), self.assertRaises(ValueError):
                    validate_local_root(unsafe, repository)

            approved.mkdir(parents=True, exist_ok=True)
            linked_root = approved / 'public-link'
            try:
                linked_root.symlink_to(public, target_is_directory=True)
            except (OSError, NotImplementedError):
                return
            with self.assertRaises(ValueError):
                validate_local_root(linked_root, repository)

            redirected_repository = pathlib.Path(temporary) / 'redirected-repo'
            (redirected_repository / 'docs').mkdir(parents=True)
            (redirected_repository / '.local').mkdir()
            try:
                (redirected_repository / '.local' / 'bluemap').symlink_to(
                    redirected_repository / 'docs', target_is_directory=True,
                )
            except (OSError, NotImplementedError):
                return
            with self.assertRaises(ValueError):
                validate_local_root(redirected_repository / '.local' / 'bluemap', redirected_repository)

            exposed_repository = pathlib.Path(temporary) / 'exposed-repo'
            exposed_cache = exposed_repository / '.local' / 'bluemap' / 'world-cache'
            exposed_cache.mkdir(parents=True)
            public_parent = exposed_repository / 'apps' / 'web'
            public_parent.mkdir(parents=True)
            try:
                (public_parent / 'public').symlink_to(exposed_cache, target_is_directory=True)
            except (OSError, NotImplementedError):
                return
            with self.assertRaises(ValueError):
                validate_local_root(exposed_repository / '.local' / 'bluemap', exposed_repository)

    def test_zip_world_path_is_reused_only_for_same_archive(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            archive = root / 'world.zip'
            archive.write_bytes(b'world archive')
            extractions = []

            def extract(_archive, destination):
                (destination / 'region').mkdir(parents=True)
                (destination / 'level.dat').write_bytes(b'cached level')
                (destination / 'region' / 'r.0.0.mca').write_bytes(b'cached region')
                extractions.append(destination)
                return destination

            with patch('scripts.bluemap_local.extract_world_from_zip', side_effect=extract), \
                    patch('scripts.bluemap_local.inspect_world_directory', return_value=(3955, 191, -71)):
                first = resolve_world_path(str(archive), root / 'bluemap')
                second = resolve_world_path(str(archive), root / 'bluemap')

            self.assertEqual(first, second)
            self.assertEqual(len(extractions), 1)
            archive.write_bytes(b'updated world archive')
            with patch('scripts.bluemap_local.extract_world_from_zip', side_effect=extract), \
                    patch('scripts.bluemap_local.inspect_world_directory', return_value=(3955, 191, -71)):
                third = resolve_world_path(str(archive), root / 'bluemap')
            self.assertNotEqual(first, third)
            self.assertEqual(len(extractions), 2)

    def test_zip_cache_rejects_modified_snapshot_instead_of_reusing_it(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            archive = root / 'world.zip'
            archive.write_bytes(b'world archive')

            def extract(_archive, destination):
                (destination / 'region').mkdir(parents=True)
                (destination / 'level.dat').write_bytes(b'cached level')
                (destination / 'region' / 'r.0.0.mca').write_bytes(b'original region')
                return destination

            with patch('scripts.bluemap_local.extract_world_from_zip', side_effect=extract), \
                    patch('scripts.bluemap_local.inspect_world_directory', return_value=(3955, 191, -71)):
                cached = resolve_world_path(str(archive), root / 'bluemap')
                (cached / 'region' / 'r.0.0.mca').write_bytes(b'modified but region-shaped')
                with self.assertRaisesRegex(RuntimeError, 'Snapshot ZIP không khớp'):
                    resolve_world_path(str(archive), root / 'bluemap')

    def test_extracted_world_is_snapshotted_without_player_data_or_source_mutation(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            source = root / 'source-world'
            (source / 'region').mkdir(parents=True)
            (source / 'level.dat').write_bytes(b'level metadata')
            (source / 'region' / 'r.0.0.mca').write_bytes(b'overworld blocks')
            (source / 'playerdata').mkdir()
            (source / 'playerdata' / 'private.dat').write_bytes(b'private player data')

            with patch('scripts.bluemap_local.inspect_world_directory', return_value=(3955, 191, -71)):
                snapshot = _snapshot_world_directory(source, root / 'cache')

            self.assertEqual((snapshot / 'level.dat').read_bytes(), b'level metadata')
            self.assertEqual((snapshot / 'region' / 'r.0.0.mca').read_bytes(), b'overworld blocks')
            self.assertFalse((snapshot / 'playerdata').exists())
            self.assertEqual((source / 'region' / 'r.0.0.mca').read_bytes(), b'overworld blocks')

    def test_archive_extraction_keeps_only_level_and_overworld_regions(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            archive = root / 'world.zip'
            with zipfile.ZipFile(archive, 'w') as world_zip:
                world_zip.writestr('world/level.dat', b'private-world-metadata')
                world_zip.writestr('world/region/r.0.0.mca', b'overworld-region')
                world_zip.writestr('world/playerdata/private.dat', b'player-inventory')
                world_zip.writestr('world/entities/r.0.0.mca', b'entity-data')
                world_zip.writestr('world/region/not-a-region.txt', b'ignored')

            destination = root / 'extracted-world'
            extracted = extract_world_from_zip(archive, destination)

            self.assertEqual(extracted, destination)
            self.assertEqual((destination / 'level.dat').read_bytes(), b'private-world-metadata')
            self.assertEqual((destination / 'region' / 'r.0.0.mca').read_bytes(), b'overworld-region')
            self.assertFalse((destination / 'playerdata').exists())
            self.assertFalse((destination / 'entities').exists())

    def test_unsafe_archive_is_rejected_before_any_file_is_extracted(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            archive = root / 'unsafe.zip'
            with zipfile.ZipFile(archive, 'w') as world_zip:
                world_zip.writestr('world/level.dat', b'level')
                world_zip.writestr('world/region/r.0.0.mca', b'region')
                world_zip.writestr('../outside.txt', b'do-not-write')

            destination = root / 'extracted-world'
            with self.assertRaises(ValueError):
                extract_world_from_zip(archive, destination)

            self.assertFalse(destination.exists())
            self.assertFalse((root / 'outside.txt').exists())

    def test_quarantine_preserves_only_generated_placeholder_dimensions_outside_map_list(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            maps = root / 'config' / 'maps'
            maps.mkdir(parents=True)
            dimensions = {
                'overworld': 'minecraft:overworld',
                'nether': 'minecraft:the_nether',
                'end': 'minecraft:the_end',
            }
            for map_id, dimension in dimensions.items():
                (maps / f'{map_id}.conf').write_text(
                    f'## Map-Config\nworld: "world"\ndimension: "{dimension}"\n', encoding='utf-8'
                )
            (maps / 'custom.conf').write_text('world: "C:/real/world"\n', encoding='utf-8')

            _quarantine_unused_default_maps(root, maps)

            self.assertFalse((maps / 'overworld.conf').exists())
            self.assertTrue((root / 'config' / 'example-map-configs' / 'overworld.conf.template').is_file())
            self.assertTrue((maps / 'custom.conf').is_file())

    def test_quarantine_also_runs_when_zip_world_was_extracted_under_local_root(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = pathlib.Path(temporary)
            (root / 'world').mkdir()
            (root / 'world' / 'level.dat').write_bytes(b'local extracted world')
            maps = root / 'config' / 'maps'
            maps.mkdir(parents=True)
            (maps / 'overworld.conf').write_text(
                '## Map-Config\nworld: "world"\ndimension: "minecraft:overworld"\n', encoding='utf-8'
            )

            _quarantine_unused_default_maps(root, maps)

            self.assertFalse((maps / 'overworld.conf').exists())
            self.assertTrue((root / 'config' / 'example-map-configs' / 'overworld.conf.template').is_file())


if __name__ == '__main__':
    unittest.main()
