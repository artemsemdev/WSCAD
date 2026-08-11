#!/usr/bin/env bash
#
# Full validation, identical to what CI runs on the default branch.
#
# The pipeline orchestrates these commands rather than owning them, so a developer can
# reproduce a CI failure locally without reading any YAML:
#
#   scripts/ci.sh              restore, build, test, analysis self-tests, docker build
#   scripts/ci.sh --no-docker  skip the container build (e.g. no Docker daemon available)
#
set -euo pipefail

cd "$(dirname "$0")/.."

WITH_DOCKER=1
[[ "${1:-}" == "--no-docker" ]] && WITH_DOCKER=0

step() { printf '\n\033[1m==> %s\033[0m\n' "$1"; }

step "Change-analysis self-tests"
python3 scripts/test_affected.py

step "dotnet restore"
dotnet restore

step "dotnet build (Release)"
dotnet build --configuration Release --no-restore

step "dotnet test (Release)"
dotnet test --configuration Release --no-build \
    --logger "trx;LogFileName=results.trx" \
    --results-directory TestResults \
    --collect:"XPlat Code Coverage"

if [[ $WITH_DOCKER -eq 1 ]]; then
    step "docker build"
    # Same target CI builds. Not pushed anywhere: there is no registry and no deployable.
    docker build --target test --tag vectorviewer-ci:local .
else
    step "docker build (skipped)"
fi

printf '\n\033[1;32mAll validation passed.\033[0m\n'
