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


if __name__ == "__main__":
    unittest.main()
