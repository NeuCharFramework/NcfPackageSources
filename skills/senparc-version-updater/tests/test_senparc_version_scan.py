import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


sys.dont_write_bytecode = True
SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "senparc_version_scan.py"
SPEC = importlib.util.spec_from_file_location("senparc_version_scan", SCRIPT)
scanner = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = scanner
SPEC.loader.exec_module(scanner)


class TestProjectClassification(unittest.TestCase):
    def test_test_project_names_and_directories(self):
        root = Path("/repo")
        for project in (
            "src/Library.Tests/Library.Tests.csproj",
            "src/LibraryTests/LibraryTests.csproj",
            "src/Library.Tests/Library.Tests.net10.csproj",
            "src/Tests/Fixture/Fixture.csproj",
        ):
            with self.subTest(project=project):
                self.assertTrue(scanner.is_test_project(root / project, root))
        for project in (
            "src/Library/Library.csproj",
            "src/UnitTestExtension/UnitTestExtension.csproj",
        ):
            with self.subTest(project=project):
                self.assertFalse(scanner.is_test_project(root / project, root))

    def test_ancestor_directory_outside_repository_does_not_classify_project(self):
        root = Path("/repo/Tests/project")
        self.assertFalse(scanner.is_test_project(root / "src/App/App.csproj", root))


class TestScanDependencyTraversal(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.git("init", "--quiet", "-b", "master")
        self.git("config", "user.name", "Test")
        self.git("config", "user.email", "test@example.invalid")
        self.write_project("Core/Core.csproj")
        self.write_project("Core.Tests/Core.Tests.csproj", "../Core/Core.csproj")
        self.write_project("Consumer/Consumer.csproj", "../Core.Tests/Core.Tests.csproj")
        self.write_file("Core/Feature.cs", "class Feature {}\n")
        self.write_file("Core.Tests/FeatureTests.cs", "class FeatureTests {}\n")
        self.git("add", ".")
        self.git("commit", "--quiet", "-m", "baseline")
        self.git("checkout", "--quiet", "-b", "feature")

    def git(self, *args):
        return subprocess.run(
            ["git", "-C", str(self.root), *args],
            check=True,
            capture_output=True,
            text=True,
        )

    def write_file(self, path, text):
        target = self.root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8")

    def write_project(self, path, reference=None):
        reference_xml = (
            f'<ItemGroup><ProjectReference Include="{reference}" /></ItemGroup>'
            if reference else ""
        )
        self.write_file(
            path,
            '<Project Sdk="Microsoft.NET.Sdk">'
            "<PropertyGroup><TargetFramework>net10.0</TargetFramework>"
            "<Version>1.0.0</Version></PropertyGroup>"
            f"{reference_xml}</Project>\n",
        )

    def scan(self):
        result = subprocess.run(
            [
                sys.executable, str(SCRIPT), "--root", str(self.root),
                "--project", "Core/Core.csproj",
            ],
            check=True,
            capture_output=True,
            text=True,
        )
        return json.loads(result.stdout)

    def test_test_only_change_does_not_promote_unrelated_primary(self):
        self.write_file("Core.Tests/FeatureTests.cs", "class FeatureTests { int Value; }\n")
        self.git("add", ".")
        self.git("commit", "--quiet", "-m", "change test")

        result = self.scan()

        self.assertEqual([], result["changed_csprojs"])
        self.assertEqual([], result["dependency_roots"])
        self.assertEqual(["Core.Tests/Core.Tests.csproj"], result["skipped_test_csprojs"])
        self.assertEqual(["Consumer/Consumer.csproj"], result["dependent_csprojs"])

    def test_traverses_test_project_to_non_test_dependent(self):
        self.write_file("Core/Feature.cs", "class Feature { int Value; }\n")
        self.git("add", ".")
        self.git("commit", "--quiet", "-m", "change core")

        result = self.scan()

        self.assertEqual(["Core/Core.csproj"], result["changed_csprojs"])
        self.assertEqual(["Core/Core.csproj"], result["dependency_roots"])
        self.assertEqual(["Core.Tests/Core.Tests.csproj"], result["skipped_test_csprojs"])
        self.assertEqual(["Consumer/Consumer.csproj"], result["dependent_csprojs"])


class TestGeneratedMigrationHeaderSkip(unittest.TestCase):
    def test_migration_directory_is_tracked_without_header_candidates(self):
        self.assertTrue(scanner.is_generated_migration_cs(
            "Module/Domain/Migrations/Oracle/20260101000000_AddColumn.cs"
        ))
        self.assertTrue(scanner.is_generated_migration_cs(
            "Module/Domain/Migrations/SqlServer/ModuleModelSnapshot.Designer.cs"
        ))
        self.assertFalse(scanner.is_generated_migration_cs(
            "Module/Domain/Services/Feature.cs"
        ))
        self.assertFalse(scanner.is_generated_migration_cs(
            "Module/Domain/Migrations/README.md"
        ))

    def test_scan_keeps_migration_project_while_skipping_headers(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        root = Path(temp.name)
        subprocess.run(["git", "-C", str(root), "init", "--quiet", "-b", "master"], check=True)
        subprocess.run(["git", "-C", str(root), "config", "user.name", "Test"], check=True)
        subprocess.run(["git", "-C", str(root), "config", "user.email", "test@example.invalid"], check=True)

        def write(path, text):
            target = root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(text, encoding="utf-8")

        write(
            "Module/Module.csproj",
            '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
            "<TargetFramework>net10.0</TargetFramework><Version>1.0.0</Version>"
            "</PropertyGroup></Project>\n",
        )
        write("Module/Domain/Services/Feature.cs", "class Feature {}\n")
        write("Module/Domain/Migrations/Oracle/20260101000000_AddColumn.cs", "class AddColumn {}\n")
        subprocess.run(["git", "-C", str(root), "add", "."], check=True)
        subprocess.run(["git", "-C", str(root), "commit", "--quiet", "-m", "baseline"], check=True)
        subprocess.run(["git", "-C", str(root), "checkout", "--quiet", "-b", "feature"], check=True)
        write("Module/Domain/Services/Feature.cs", "class Feature { int Value; }\n")
        write(
            "Module/Domain/Migrations/Oracle/20260101000000_AddColumn.cs",
            "class AddColumn { string Type; }\n",
        )
        subprocess.run(["git", "-C", str(root), "add", "."], check=True)
        subprocess.run(["git", "-C", str(root), "commit", "--quiet", "-m", "change migration"], check=True)

        result = subprocess.run(
            [sys.executable, str(SCRIPT), "--root", str(root), "--project", "Module/Module.csproj"],
            check=True,
            capture_output=True,
            text=True,
        )
        payload = json.loads(result.stdout)
        header_paths = [item["path"] for item in payload["changed_cs_files"]]

        self.assertEqual(["Module/Domain/Services/Feature.cs"], header_paths)
        self.assertEqual(
            ["Module/Domain/Migrations/Oracle/20260101000000_AddColumn.cs"],
            payload["skipped_migration_cs_files"],
        )
        self.assertIn(
            "Module/Domain/Migrations/Oracle/20260101000000_AddColumn.cs",
            payload["changed_csproj_to_files"]["Module/Module.csproj"],
        )
        self.assertEqual(["Module/Module.csproj"], payload["changed_csprojs"])


if __name__ == "__main__":
    unittest.main()
