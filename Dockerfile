# Reproducible build and test environment for the vector viewer.
#
# Scope: this container builds the whole solution — including the WPF project, which
# compiles on Linux thanks to EnableWindowsTargeting — and runs the full test suite.
# It deliberately does NOT run the viewer itself: WPF requires Windows, and no amount of
# X11 forwarding changes that. Use `dotnet run --project src/VectorViewer.Wpf` on Windows
# for the UI. Everything the tests cover is UI-independent by design.

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first, from the project files alone, so editing source does not invalidate the
# NuGet layer. Each csproj is copied to its own path because COPY flattens wildcards.
COPY VectorViewer.sln Directory.Build.props Directory.Packages.props ./
COPY src/VectorViewer.Domain/VectorViewer.Domain.csproj                         src/VectorViewer.Domain/
COPY src/VectorViewer.Application/VectorViewer.Application.csproj               src/VectorViewer.Application/
COPY src/VectorViewer.Infrastructure/VectorViewer.Infrastructure.csproj         src/VectorViewer.Infrastructure/
COPY src/VectorViewer.Wpf/VectorViewer.Wpf.csproj                               src/VectorViewer.Wpf/
COPY tests/VectorViewer.Domain.Tests/VectorViewer.Domain.Tests.csproj                   tests/VectorViewer.Domain.Tests/
COPY tests/VectorViewer.Application.Tests/VectorViewer.Application.Tests.csproj         tests/VectorViewer.Application.Tests/
COPY tests/VectorViewer.Infrastructure.Tests/VectorViewer.Infrastructure.Tests.csproj   tests/VectorViewer.Infrastructure.Tests/
COPY tests/VectorViewer.IntegrationTests/VectorViewer.IntegrationTests.csproj           tests/VectorViewer.IntegrationTests/
RUN dotnet restore

COPY . .
RUN dotnet build -c Release --no-restore

# Default target: run the suite. Results are written to /testresults, which docker-compose
# mounts back to the host so a failure can be inspected outside the container.
FROM build AS test
ENTRYPOINT ["dotnet", "test", "-c", "Release", "--no-build", "--nologo", \
            "--logger", "trx;LogFileName=results.trx", "--results-directory", "/testresults"]
