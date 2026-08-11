#!/usr/bin/env python3
"""Work out which components a set of changed files actually affects.

The pipeline's selective execution is only as trustworthy as this file, so the graph is
*derived*, never assumed:

  * project edges come from the ``ProjectReference`` elements in the ``.csproj`` files;
  * asset edges come from ``Content Include`` elements, which is how ``samples/example.json``
    reaches both the WPF app and the integration tests — a dependency no folder-name
    heuristic would ever find;
  * the container's dependencies come from what the Dockerfile actually copies.

Two rules keep it honest:

  1. Anything that cannot be attributed to a component escalates to full validation.
     Guessing wrong costs a broken ``master``; running extra tests costs a minute.
  2. Selective execution applies to pull requests only. Pushes to the default branch always
     run everything, so a mistake here is caught before it can hide.

Usage::

    scripts/affected.py --base origin/master      # analyse a branch against its base
    scripts/affected.py --changed-files -         # read a newline-separated list on stdin
    scripts/affected.py --full                    # force full validation
    scripts/affected.py --print-graph             # show the derived dependency graph
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent

# Files that change how *everything* is built, so their blast radius is the whole repository.
# Listed explicitly rather than pattern-matched: a reviewer must be able to see the set.
GLOBAL_TRIGGERS = {
    "Directory.Build.props",
    "Directory.Packages.props",
    "Directory.Build.targets",
    "global.json",
    "NuGet.config",
    "nuget.config",
    "VectorViewer.sln",
}

GLOBAL_TRIGGER_PREFIXES = (
    ".github/workflows/",  # a CI change must be validated by the full pipeline
    "scripts/",            # including this file: it decides what runs
)

DOCS_SUFFIXES = (".md",)
DOCS_PREFIXES = ("docs/",)

DOCKER_FILES = {".dockerignore", "docker-compose.yml", "compose.yml"}
DOCKER_PREFIXES = ("Dockerfile",)


class DependencyGraph:
    """The projects in the repository and who depends on whom."""

    def __init__(self, root: Path):
        self.root = root
        self.projects: dict[str, Path] = {}       # project dir (repo-relative) -> csproj path
        self.references: dict[str, set[str]] = {}  # project dir -> project dirs it references
        self.assets: dict[str, set[str]] = {}      # asset file (repo-relative) -> project dirs
        self._discover()

    def _discover(self) -> None:
        for csproj in sorted(self.root.glob("*/*/*.csproj")):
            project_dir = csproj.parent.relative_to(self.root).as_posix()
            if not project_dir.startswith(("src/", "tests/")):
                continue
            self.projects[project_dir] = csproj
            self.references[project_dir] = set()

        for project_dir, csproj in self.projects.items():
            text = csproj.read_text(encoding="utf-8")
            base = csproj.parent

            for raw in re.findall(r'ProjectReference\s+Include="([^"]+)"', text):
                target = (base / raw.replace("\\", "/")).resolve()
                self.references[project_dir].add(
                    target.parent.relative_to(self.root).as_posix())

            # Assets pulled in from outside the project directory, e.g. the shared sample.
            for raw in re.findall(r'Content\s+Include="([^"]+)"', text):
                asset = (base / raw.replace("\\", "/")).resolve()
                try:
                    key = asset.relative_to(self.root).as_posix()
                except ValueError:
                    continue
                if not key.startswith(project_dir):
                    self.assets.setdefault(key, set()).add(project_dir)

    @property
    def test_projects(self) -> list[str]:
        return sorted(p for p in self.projects if p.startswith("tests/"))

    def dependents_of(self, project: str) -> set[str]:
        """Every project that depends on ``project``, directly or transitively."""
        found: set[str] = set()
        pending = [project]
        while pending:
            current = pending.pop()
            for candidate, refs in self.references.items():
                if current in refs and candidate not in found:
                    found.add(candidate)
                    pending.append(candidate)
        return found

    def owning_project(self, path: str) -> str | None:
        """The project a repository-relative file belongs to, if any."""
        best: str | None = None
        for project_dir in self.projects:
            if path.startswith(project_dir + "/") and (best is None or len(project_dir) > len(best)):
                best = project_dir
        return best

    def describe(self) -> str:
        lines = ["Project dependency graph (derived from ProjectReference elements):", ""]
        for project in sorted(self.projects):
            refs = sorted(self.references[project]) or ["(none)"]
            lines.append(f"  {project}")
            for ref in refs:
                lines.append(f"      -> {ref}")
        lines += ["", "Reverse closure (a change here validates all of these):", ""]
        for project in sorted(self.projects):
            dependents = sorted(self.dependents_of(project)) or ["(nothing depends on it)"]
            lines.append(f"  {project}")
            for dependent in dependents:
                lines.append(f"      <- {dependent}")
        if self.assets:
            lines += ["", "Shared assets (from Content Include, not folder names):", ""]
            for asset, consumers in sorted(self.assets.items()):
                lines.append(f"  {asset}")
                for consumer in sorted(consumers):
                    lines.append(f"      -> {consumer}")
        return "\n".join(lines)


class Decision:
    """What the pipeline should run, and why."""

    def __init__(self) -> None:
        self.full_validation = False
        self.docs_only = False
        self.affected_projects: set[str] = set()
        self.build_docker = False
        self.reasons: list[str] = []

    def explain(self, reason: str) -> None:
        if reason not in self.reasons:
            self.reasons.append(reason)


def analyse(graph: DependencyGraph, changed: list[str], force_full: bool = False) -> Decision:
    decision = Decision()

    if force_full:
        decision.full_validation = True
        decision.explain("full validation requested explicitly")
    elif not changed:
        # No detectable diff. Could mean an empty change or a failed comparison; either way,
        # assuming "nothing to do" is the one mistake that silently lets a regression through.
        decision.full_validation = True
        decision.explain("no changed files could be determined - validating everything")

    docs = []
    unattributed = []

    for path in changed:
        name = os.path.basename(path)

        if path in GLOBAL_TRIGGERS or name in GLOBAL_TRIGGERS:
            decision.full_validation = True
            decision.explain(f"{path} changes how the whole solution is built")
            continue

        if path.startswith(GLOBAL_TRIGGER_PREFIXES):
            decision.full_validation = True
            decision.explain(f"{path} defines the pipeline itself")
            continue

        # A .csproj edit can add or remove a ProjectReference, which changes the very graph
        # this analysis relies on. Never trust the old graph to scope a change to the new one.
        if path.endswith(".csproj"):
            decision.full_validation = True
            decision.explain(f"{path} may alter the dependency graph")
            continue

        if path.startswith(DOCKER_PREFIXES) or path in DOCKER_FILES or name in DOCKER_FILES:
            decision.build_docker = True
            decision.explain(f"{path} changes the container definition")
            continue

        if path.startswith(DOCS_PREFIXES) or path.endswith(DOCS_SUFFIXES):
            docs.append(path)
            continue

        if path in graph.assets:
            for consumer in graph.assets[path]:
                decision.affected_projects.add(consumer)
                decision.affected_projects |= graph.dependents_of(consumer)
            decision.explain(f"{path} is an asset consumed by {', '.join(sorted(graph.assets[path]))}")
            continue

        owner = graph.owning_project(path)
        if owner:
            decision.affected_projects.add(owner)
            decision.affected_projects |= graph.dependents_of(owner)
            decision.explain(f"{path} belongs to {owner}")
            continue

        unattributed.append(path)

    if unattributed:
        decision.full_validation = True
        decision.explain(
            f"could not attribute {len(unattributed)} file(s) to a component "
            f"(e.g. {unattributed[0]}) - validating everything")

    if decision.full_validation:
        decision.affected_projects = set(graph.projects)
        decision.build_docker = True
    elif decision.affected_projects:
        # The image is built from `COPY . .` followed by a solution build, so every project
        # is one of its inputs. Any affected project therefore invalidates it.
        decision.build_docker = True

    decision.docs_only = (
        bool(changed)
        and len(docs) == len(changed)
        and not decision.full_validation
        and not decision.build_docker
        and not decision.affected_projects
    )
    if decision.docs_only:
        decision.explain("documentation only - no build, test or container work required")

    return decision


def changed_files_from_git(base: str) -> list[str]:
    try:
        merge_base = subprocess.run(
            ["git", "merge-base", base, "HEAD"],
            cwd=REPO_ROOT, capture_output=True, text=True, check=True).stdout.strip()
        diff = subprocess.run(
            ["git", "diff", "--name-only", merge_base, "HEAD"],
            cwd=REPO_ROOT, capture_output=True, text=True, check=True).stdout
    except subprocess.CalledProcessError as error:
        print(f"warning: could not diff against {base}: {error}", file=sys.stderr)
        return []
    return [line.strip() for line in diff.splitlines() if line.strip()]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--base", help="git ref to compare against, e.g. origin/master")
    parser.add_argument("--changed-files", help="file with one changed path per line, or '-'")
    parser.add_argument("--full", action="store_true", help="force full validation")
    parser.add_argument("--print-graph", action="store_true", help="print the derived graph")
    args = parser.parse_args()

    graph = DependencyGraph(REPO_ROOT)

    if args.print_graph:
        print(graph.describe())
        return 0

    if args.changed_files == "-":
        changed = [line.strip() for line in sys.stdin.read().splitlines() if line.strip()]
    elif args.changed_files:
        changed = [line.strip() for line in Path(args.changed_files).read_text().splitlines()
                   if line.strip()]
    elif args.base:
        changed = changed_files_from_git(args.base)
    else:
        changed = []

    decision = analyse(graph, changed, force_full=args.full)

    test_projects = sorted(p for p in decision.affected_projects if p.startswith("tests/"))
    build_wpf = "src/VectorViewer.Wpf" in decision.affected_projects

    outputs = {
        "full_validation": decision.full_validation,
        "docs_only": decision.docs_only,
        "run_tests": bool(test_projects),
        "build_wpf": build_wpf,
        "build_docker": decision.build_docker,
        "test_projects": json.dumps(test_projects),
    }

    print("Changed files:")
    for path in changed or ["(none)"]:
        print(f"  {path}")
    print("\nReasoning:")
    for reason in decision.reasons or ["no changes require validation"]:
        print(f"  - {reason}")
    print("\nDecision:")
    for key, value in outputs.items():
        print(f"  {key}={value if not isinstance(value, bool) else str(value).lower()}")

    github_output = os.environ.get("GITHUB_OUTPUT")
    if github_output:
        with open(github_output, "a", encoding="utf-8") as handle:
            for key, value in outputs.items():
                rendered = str(value).lower() if isinstance(value, bool) else value
                handle.write(f"{key}={rendered}\n")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write("## Affected components\n\n")
            for reason in decision.reasons or ["No changes require validation."]:
                handle.write(f"- {reason}\n")
            handle.write("\n| Job | Runs |\n| --- | --- |\n")
            handle.write(f"| Tests | {', '.join(test_projects) if test_projects else 'skipped'} |\n")
            handle.write(f"| WPF build (Windows) | {'yes' if build_wpf else 'skipped'} |\n")
            handle.write(f"| Docker build | {'yes' if decision.build_docker else 'skipped'} |\n")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
