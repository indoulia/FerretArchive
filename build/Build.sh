#!/usr/bin/env bash
# build/Build.sh — local build and test script for Linux/macOS.
# Usage: ./build/Build.sh [Debug|Release] [--test] [--clean] [--format]

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SLN="$REPO_ROOT/src/Ferret.sln"
CONFIGURATION="${1:-Debug}"
shift || true

RUN_TEST=false
RUN_CLEAN=false
RUN_FORMAT=false

for arg in "$@"; do
    case "$arg" in
        --test)   RUN_TEST=true ;;
        --clean)  RUN_CLEAN=true ;;
        --format) RUN_FORMAT=true ;;
    esac
done

if $RUN_CLEAN; then
    echo "Cleaning ($CONFIGURATION)..."
    dotnet clean "$SLN" --configuration "$CONFIGURATION"
fi

echo "Building Ferret ($CONFIGURATION)..."
dotnet build "$SLN" --configuration "$CONFIGURATION"

if $RUN_FORMAT; then
    echo "Checking format..."
    dotnet format "$SLN" --verify-no-changes --no-restore
fi

if $RUN_TEST; then
    echo "Running tests..."
    dotnet test "$SLN" --no-build --configuration "$CONFIGURATION" --verbosity normal
fi

echo "Done."
