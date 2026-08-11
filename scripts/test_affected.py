#!/usr/bin/env python3
"""Tests for the affected-component analysis.

Selective CI is only safe if its rules are themselves verified — an untested filter that
quietly skips a job is worse than no filter at all. Every scenario below is one the pipeline
must get right, including the ones where the correct answer is "run everything".

Run with::

    python3 scripts/test_affected.py
"""

from __future__ import annotations

import unittest
from pathlib import Path

from affected import DependencyGraph, analyse

REPO_ROOT = Path(__file__).resolve().parent.parent

DOMAIN = "src/VectorViewer.Domain"
APPLICATION = "src/VectorViewer.Application"
INFRASTRUCTURE = "src/VectorViewer.Infrastructure"
WPF = "src/VectorViewer.Wpf"

DOMAIN_TESTS = "tests/VectorViewer.Domain.Tests"
APPLICATION_TESTS = "tests/VectorViewer.Application.Tests"
INFRASTRUCTURE_TESTS = "tests/VectorViewer.Infrastructure.Tests"
INTEGRATION_TESTS = "tests/VectorViewer.IntegrationTests"

ALL_TESTS = {DOMAIN_TESTS, APPLICATION_TESTS, INFRASTRUCTURE_TESTS, INTEGRATION_TESTS}


class AnalysisTestCase(unittest.TestCase):
    graph: DependencyGraph

    @classmethod
    def setUpClass(cls) -> None:
        cls.graph = DependencyGraph(REPO_ROOT)

    def decide(self, *changed: str):
        return analyse(self.graph, list(changed))

    def affected_tests(self, *changed: str) -> set[str]:
        decision = self.decide(*changed)
        return {p for p in decision.affected_projects if p.startswith("tests/")}


class GraphDiscovery(AnalysisTestCase):
    """The graph must be read from the project files, not hard-coded here."""

    def test_every_project_is_discovered(self):
        self.assertEqual(
            set(self.graph.projects),
            {DOMAIN, APPLICATION, INFRASTRUCTURE, WPF} | ALL_TESTS)

    def test_reference_edges_match_the_csproj_files(self):
        self.assertEqual(self.graph.references[APPLICATION], {DOMAIN})
        self.assertEqual(self.graph.references[INFRASTRUCTURE], {APPLICATION})
        self.assertEqual(self.graph.references[WPF], {INFRASTRUCTURE})
        self.assertEqual(self.graph.references[DOMAIN], set())

    def test_transitive_dependents_are_closed(self):
        # Nothing references Domain except Application directly, but the closure must reach
        # the WPF app four hops away.
        self.assertEqual(
            self.graph.dependents_of(DOMAIN),
            {APPLICATION, INFRASTRUCTURE, WPF} | ALL_TESTS)

    def test_shared_asset_edges_are_discovered(self):
        # samples/example.json is linked by two projects via Content Include. No folder-name
        # heuristic would find this.
        self.assertEqual(
            self.graph.assets.get("samples/example.json"),
            {WPF, INTEGRATION_TESTS})


class Scenarios(AnalysisTestCase):
    """The scenarios the pipeline is required to handle correctly."""

    def test_1_documentation_only_skips_everything_expensive(self):
        decision = self.decide("README.md", "docs/architecture.md", "CHANGELOG.md")

        self.assertTrue(decision.docs_only)
        self.assertFalse(decision.full_validation)
        self.assertFalse(decision.build_docker)
        self.assertEqual(decision.affected_projects, set())

    def test_2_domain_change_propagates_to_every_dependent(self):
        decision = self.decide(f"{DOMAIN}/Primitives/Circle.cs")

        # A domain change is not "run domain tests only": everything sits on top of it.
        self.assertEqual(self.affected_tests(f"{DOMAIN}/Primitives/Circle.cs"), ALL_TESTS)
        self.assertIn(WPF, decision.affected_projects)
        self.assertTrue(decision.build_docker)

    def test_3_json_reader_change_reaches_integration_and_dependents(self):
        decision = self.decide(f"{INFRASTRUCTURE}/Json/JsonVectorDocumentReader.cs")

        self.assertEqual(
            self.affected_tests(f"{INFRASTRUCTURE}/Json/JsonVectorDocumentReader.cs"),
            {INFRASTRUCTURE_TESTS, INTEGRATION_TESTS})
        self.assertIn(WPF, decision.affected_projects, "the viewer consumes the reader")
        self.assertNotIn(DOMAIN_TESTS, decision.affected_projects,
                         "the domain does not depend on infrastructure")

    def test_4_viewport_change_hits_application_and_everything_above(self):
        changed = f"{APPLICATION}/Viewport/ViewportTransform.cs"

        self.assertEqual(
            self.affected_tests(changed),
            {APPLICATION_TESTS, INFRASTRUCTURE_TESTS, INTEGRATION_TESTS})
        self.assertIn(WPF, self.decide(changed).affected_projects)
        self.assertNotIn(DOMAIN_TESTS, self.decide(changed).affected_projects)

    def test_5_wpf_only_change_builds_wpf_and_the_image_that_contains_it(self):
        decision = self.decide(f"{WPF}/MainWindow.xaml")

        self.assertIn(WPF, decision.affected_projects)
        self.assertEqual(self.affected_tests(f"{WPF}/MainWindow.xaml"), set(),
                         "nothing depends on the WPF app, so no test project is affected")
        # Not an unrelated image: this repository's Dockerfile builds the whole solution,
        # WPF included, so the container genuinely depends on these files.
        self.assertTrue(decision.build_docker)

    def test_6_dockerfile_change_validates_the_container(self):
        decision = self.decide("Dockerfile")

        self.assertTrue(decision.build_docker)
        self.assertFalse(decision.docs_only)

    def test_7_dockerignore_change_validates_the_container(self):
        # The ignore file decides what enters the image, so it is a container input.
        self.assertTrue(self.decide(".dockerignore").build_docker)
        self.assertTrue(self.decide("docker-compose.yml").build_docker)

    def test_8_shared_build_configuration_forces_full_validation(self):
        for path in ("Directory.Build.props", "Directory.Packages.props", "global.json",
                     "VectorViewer.sln"):
            with self.subTest(path=path):
                decision = self.decide(path)
                self.assertTrue(decision.full_validation, path)
                self.assertEqual(
                    {p for p in decision.affected_projects if p.startswith("tests/")}, ALL_TESTS)
                self.assertTrue(decision.build_docker)

    def test_9_project_reference_change_forces_full_validation(self):
        # The edit may add or remove an edge, so the graph used to scope it is already stale.
        decision = self.decide(f"{APPLICATION}/VectorViewer.Application.csproj")

        self.assertTrue(decision.full_validation)
        self.assertEqual(
            {p for p in decision.affected_projects if p.startswith("tests/")}, ALL_TESTS)

    def test_10_multiple_components_produce_the_union_without_duplication(self):
        decision = self.decide(
            f"{DOMAIN}/Scene.cs",
            f"{INFRASTRUCTURE}/Text/ArgbColorParser.cs",
            "README.md")

        self.assertEqual(
            {p for p in decision.affected_projects if p.startswith("tests/")}, ALL_TESTS)
        self.assertFalse(decision.docs_only, "a docs file alongside code is not docs-only")
        # affected_projects is a set, so the union cannot contain duplicates by construction.
        self.assertEqual(len(decision.affected_projects), len(set(decision.affected_projects)))

    def test_11_full_validation_covers_everything(self):
        decision = analyse(self.graph, ["README.md"], force_full=True)

        self.assertTrue(decision.full_validation)
        self.assertEqual(decision.affected_projects, set(self.graph.projects))
        self.assertTrue(decision.build_docker)
        self.assertFalse(decision.docs_only)


class FailSafeBehaviour(AnalysisTestCase):
    """When in doubt, the analysis must escalate rather than skip."""

    def test_ci_configuration_change_forces_full_validation(self):
        self.assertTrue(self.decide(".github/workflows/ci.yml").full_validation)

    def test_changing_the_analysis_itself_forces_full_validation(self):
        # Otherwise a bug in this file could be merged by the very logic it broke.
        self.assertTrue(self.decide("scripts/affected.py").full_validation)

    def test_an_unrecognised_path_forces_full_validation(self):
        decision = self.decide("some/new/toplevel/thing.cs")

        self.assertTrue(decision.full_validation)
        self.assertTrue(any("could not attribute" in r for r in decision.reasons))

    def test_an_empty_diff_forces_full_validation(self):
        # An empty list usually means the comparison failed, not that nothing changed.
        self.assertTrue(analyse(self.graph, []).full_validation)

    def test_shared_sample_reaches_both_of_its_consumers(self):
        decision = self.decide("samples/example.json")

        self.assertIn(INTEGRATION_TESTS, decision.affected_projects)
        self.assertIn(WPF, decision.affected_projects)
        self.assertNotIn(DOMAIN_TESTS, decision.affected_projects)

    def test_a_test_project_change_does_not_drag_in_unrelated_tests(self):
        decision = self.decide(f"{DOMAIN_TESTS}/SceneTests.cs")

        self.assertEqual(
            {p for p in decision.affected_projects if p.startswith("tests/")}, {DOMAIN_TESTS})
        self.assertNotIn(WPF, decision.affected_projects)


if __name__ == "__main__":
    unittest.main(verbosity=2)
