# Reproducible build and test environment for the vector viewer.
#
# WHAT THIS IMAGE IS
#   A build/test tool: it compiles every project — including the WPF one, which compiles on
#   Linux thanks to EnableWindowsTargeting — and runs the full test suite. It is what makes
#   "verify the solution" reproducible on a machine with no .NET SDK, and what CI uses to
#   prove the container definition still works.
#
# WHAT THIS IMAGE IS NOT
#   A runtime image. This repository's only application is a WPF desktop app, which requires
#   Windows and cannot run in a Linux container. There is no server, service or web API here,
#   so there is deliberately no runtime stage, no EXPOSE, no published artifact and no
#   registry. Inventing a slim runtime stage would mean inventing a deployable that does not
#   exist. See README §Docker for the boundary.
#
#   If a deployable component is ever added, it gets its own final stage on
#   mcr.microsoft.com/dotnet/runtime-deps (or aspnet), running as a non-root user, containing
#   published output only — not this SDK stage.
#
# Run the viewer itself with `dotnet run --project src/VectorViewer.Wpf` on Windows.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    # Fail loudly instead of silently building against a different SDK than global.json pins.
    DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=0

WORKDIR /src

# Restore from dependency metadata alone, so editing source does not invalidate the NuGet
# layer. global.json is included because it selects the SDK and therefore the restore result.
# Each csproj is copied to its own path because COPY flattens wildcards.
COPY global.json VectorViewer.sln Directory.Build.props Directory.Packages.props ./
COPY src/VectorViewer.Domain/VectorViewer.Domain.csproj                                 src/VectorViewer.Domain/
COPY src/VectorViewer.Application/VectorViewer.Application.csproj                       src/VectorViewer.Application/
COPY src/VectorViewer.Infrastructure/VectorViewer.Infrastructure.csproj                 src/VectorViewer.Infrastructure/
COPY src/VectorViewer.Wpf/VectorViewer.Wpf.csproj                                       src/VectorViewer.Wpf/
COPY tests/VectorViewer.Domain.Tests/VectorViewer.Domain.Tests.csproj                   tests/VectorViewer.Domain.Tests/
COPY tests/VectorViewer.Application.Tests/VectorViewer.Application.Tests.csproj         tests/VectorViewer.Application.Tests/
COPY tests/VectorViewer.Infrastructure.Tests/VectorViewer.Infrastructure.Tests.csproj   tests/VectorViewer.Infrastructure.Tests/
COPY tests/VectorViewer.IntegrationTests/VectorViewer.IntegrationTests.csproj           tests/VectorViewer.IntegrationTests/
RUN dotnet restore

# Source only: .dockerignore keeps documentation, .git, CI definitions and build output out,
# so this layer is invalidated by code changes and nothing else.
COPY . .
RUN dotnet build -c Release --no-restore

# Default target: run the suite. Results go to /testresults, which docker-compose maps to the
# host so a failing run can be inspected after the container exits.
FROM build AS test
ENTRYPOINT ["dotnet", "test", "-c", "Release", "--no-build", "--nologo", \
            "--logger", "trx;LogFileName=results.trx", "--results-directory", "/testresults"]

# Runs as root. This is an ephemeral build tool with no network listener, no secrets and no
# deployment: dropping privileges would buy nothing here and would break the bind-mounted
# results directory. A future *runtime* image must not inherit this choice.
