# Vector Graphic Viewer — WSCAD Code Challenge

A small WPF application that reads vector primitives from a JSON file and draws them,
automatically scaled to fit the window.

The interesting part of this repository is not the drawing — it is the seams: input format,
primitive set and rendering back end are all replaceable without touching the core.

---

## 1. Overview

```
samples/example.json ──▶ JSON reader ──▶ Scene ──▶ fit-to-window transform ──▶ draw commands ──▶ WPF
```

* **Reads** an array of primitives (`line`, `circle`, `triangle`) from JSON.
* **Draws** them on a Cartesian plane with **Y pointing up**.
* **Scales down** proportionally when the drawing is larger than the window; never scales up,
  so 100 % zoom means 1 virtual unit = 1 pixel.
* **Colours** are ARGB, alpha included, so overlapping shapes blend as expected.
* **`filled: true`** draws border + fill; **`filled: false`** draws border only.

## 2. Challenge requirements

| Requirement | Where it is implemented |
| --- | --- |
| Read primitives from JSON | `JsonVectorDocumentReader` (Infrastructure) |
| Line / Circle / Triangle | `Line`, `Circle`, `Triangle` (Domain) |
| WPF UI | `VectorViewer.Wpf` |
| Cartesian coordinates, Y up | `ViewportTransform.ToScreen` (single sign inversion) |
| 100 % zoom ⇒ 1 unit = 1 px | `ViewportTransform.Fit`, `MaximumScale = 1.0` |
| Proportional scale-to-fit | `ViewportTransform.Fit`, one uniform scale factor |
| ARGB colours | `ArgbColor` (Domain), `ArgbColorParser` (Infrastructure) |
| `filled` ⇒ border + fill, else border only | `CircleRenderer`, `TriangleRenderer` (Application) |
| Border width ≈ 1 unit | `RenderOptions.BorderWidthInWorldUnits = 1.0` |
| Extensible primitives / formats / selection | §12–§15 |

## 3. Architecture overview

```
VectorViewer.Wpf            presentation adapter — paints draw commands, view model, file dialog
        │
VectorViewer.Infrastructure JSON adapter — DTO mapping, "x; y" and "a; r; g; b" parsing
        │
VectorViewer.Application    viewport maths, render model, primitive renderers, reader port
        │
VectorViewer.Domain         Point2D, BoundingBox, ArgbColor, IPrimitive, Scene — pure, no I/O
```

Dependencies point inwards only. The domain and application layers have **no reference to
WPF**, which is what makes the geometry and scaling logic ordinary unit tests.

Three abstractions carry the extensibility:

| Abstraction | Purpose |
| --- | --- |
| `IPrimitive` (+ `IFillablePrimitive`) | A shape knows its colour and its bounds — nothing about drawing. |
| `IVectorDocumentReader` | A format adapter. JSON today, XML tomorrow, selected by file extension. |
| `IPrimitiveRenderer` + `PrimitiveRendererRegistry` | Type-keyed dispatch from primitive to `DrawCommand`s. No `switch` over primitive types anywhere. |

Details in [docs/architecture.md](docs/architecture.md); the reasoning in
[docs/adr/0001-architecture.md](docs/adr/0001-architecture.md).

## 4. Repository structure

```
├── VectorViewer.sln
├── Directory.Build.props            shared compiler settings (nullable, warnings-as-errors)
├── Directory.Packages.props         central package versions
├── Dockerfile, docker-compose.yml   reproducible build + test environment
├── global.json                      pins the SDK feature band for local/CI parity
├── CHANGELOG.md
├── .github/workflows/ci.yml         dependency-aware CI (§20)
├── scripts/
│   ├── affected.py                  derives the dependency graph, decides what CI runs
│   ├── test_affected.py             tests for that logic
│   └── ci.sh                        full local validation, same commands as CI
├── src/
│   ├── VectorViewer.Domain/         geometry + primitives
│   ├── VectorViewer.Application/    viewport, render model, renderers, loader
│   ├── VectorViewer.Infrastructure/ JSON reading
│   └── VectorViewer.Wpf/            WPF presentation
├── tests/
│   ├── VectorViewer.Domain.Tests/
│   ├── VectorViewer.Application.Tests/
│   ├── VectorViewer.Infrastructure.Tests/
│   └── VectorViewer.IntegrationTests/
├── docs/architecture.md, docs/adr/0001-architecture.md
└── samples/example.json             the challenge payload, used as a test fixture
```

## 5. How to build

```bash
dotnet restore
dotnet build -c Release
```

Requires the .NET 9 SDK. Warnings are errors.

> The WPF project targets `net9.0-windows`. `EnableWindowsTargeting` is set, so the whole
> solution — WPF included — **compiles** on macOS and Linux; it can only be **run** on Windows.

## 6. How to run

```bash
dotnet run --project src/VectorViewer.Wpf            # Windows
dotnet run --project src/VectorViewer.Wpf -- path/to/drawing.json
```

`samples/example.json` is copied next to the executable and loaded on start-up, so the
window shows the challenge drawing immediately. **Open…** (Ctrl+O) loads another file;
the status bar shows the primitive count, the scene bounds and the current zoom.

## 7. How to run tests

```bash
dotnet test
```

Or, with no .NET SDK installed at all:

```bash
docker compose run --rm tests
```

That builds the whole solution — WPF included — and runs the suite in a pinned .NET 9
image, writing `TestResults/results.trx` back to the host. `docker compose run --rm shell`
drops into the same environment. The container cannot run the viewer itself: WPF requires
Windows. That the *tests* run anywhere while the *UI* does not is the architecture working
as intended.

164 tests, all green, in well under a second. They run on any OS — no test project
references WPF, which is the practical pay-off of keeping geometry out of the UI layer.

| Project | Tests | Covers |
| --- | --- | --- |
| `Domain.Tests` | 38 | bounds, ARGB, primitives, scene aggregation |
| `Application.Tests` | 74 | viewport maths, renderers, registry, format selection, extensibility, layer boundaries |
| `Infrastructure.Tests` | 36 | the `"x; y"` / `"a; r; g; b"` formats, JSON mapping, ordering |
| `IntegrationTests` | 16 | the challenge payload end to end |

## 8. TDD approach

The work was done in three phases, and the commit order reflects it:

1. **Phase 1** — repository, architecture docs, then the public API as stubs that throw
   `NotImplementedException`, then the complete test suite. The suite was run and confirmed
   red *for the expected reason*: **149 of 162 failures were `NotImplementedException`**, none
   a compile error. (The 13 that passed immediately assert pure data declarations with no
   behaviour to implement.)
2. **Phase 2** — production code written until the suite went green, layer by layer:
   domain → application → infrastructure → WPF.
3. **Phase 3** — warning-free build, review pass (which removed `Triangle.Vertices`,
   `Appearance.HasStroke/HasFill` and an unused constructor once the implementation showed
   they had no consumer), documentation update.

The design was shaped by the tests more than once. The `Appearance` value type exists because
a first attempt put `Stroke?`/`Fill?` directly on `DrawCommand`, which a derived record could
not narrow — grouping them turned out to be the better model anyway, since it let the
"filled ⇒ border + fill" rule live in exactly one method.

Tests assert observable behaviour — bounds, screen coordinates, emitted draw commands —
not internal structure. There is **no mocking framework**: every collaborator is either a
pure value or trivially constructible, which is itself a signal that the boundaries are in
sensible places. The two test doubles that do exist (a `Rectangle` primitive and its
renderer, defined inside the test project) exist to *prove* the extensibility claim in §13.

## 9. Important design decisions

| Decision | Reasoning |
| --- | --- |
| Renderer **registry** keyed by primitive type, not a visitor | A visitor makes new *operations* cheap and new *types* expensive. The challenge predicts new types. |
| `Filled` on `IFillablePrimitive`, not on `IPrimitive` | A line has no interior; a universal flag would be meaningless for half the primitives. |
| Intermediate `DrawCommand` model in screen space | Makes "filled circle ⇒ border + fill" a plain assertion instead of a pixel test, and allows a second rendering back end. |
| Primitives are immutable `sealed record`s | Value equality reads well in tests; immutability lets `Scene` cache its bounds. |
| `Scene.Bounds` is `BoundingBox?` | An empty scene has no bounds. A sentinel "empty box" invites silent maths errors. |
| `DrawingContext` (`OnRender`) instead of `Shape` objects on a `Canvas` | One visual per primitive does not scale; a single draw pass with cached frozen brushes does. |
| Manual composition root, no DI container | The object graph is about six objects. A container would add configuration, not clarity. |
| No MVVM framework | `INotifyPropertyChanged` + a 30-line `RelayCommand` covers the two commands this app has. |

## 10. Coordinate system

Input is Cartesian with **Y up**; screens are **Y down** with the origin top-left.
`ViewportTransform` is the only place that knows this:

```csharp
screenX = viewportCentreX + (worldX - worldCentreX) * scale
screenY = viewportCentreY - (worldY - worldCentreY) * scale   // ← the inversion
```

The transform is invertible (`ToWorld`), which is what a future hit-test needs. It is a
pure value object with no WPF types, and it is the most heavily tested class in the
solution: origin mapping, Y inversion, negative coordinates, origin-crossing scenes,
degenerate (zero-extent) scenes and round-tripping.

## 11. Scaling / fit-to-window

`ViewportTransform.Fit(bounds, viewport, options)`:

1. `scale = min(viewportWidth / boundsWidth, viewportHeight / boundsHeight)` — **one**
   factor applied to both axes, so the aspect ratio is preserved;
2. `scale = min(scale, options.MaximumScale)` with `MaximumScale = 1.0` — a drawing smaller
   than the window is shown at **100 % (1 unit = 1 pixel)** rather than magnified, exactly
   as the challenge specifies. `AllowUpscale` flips this if magnification is ever wanted;
3. the centre of the scene bounds is mapped to the centre of the viewport, so the drawing
   is centred regardless of where it sits in world space;
4. `Padding` (8 px per side) is reserved so a border stroke on a shape at the very edge of
   the drawing is not clipped by the window.

Circle bounds participate normally (`centre ± radius`), so a circle never overflows the
window. Degenerate scenes — a single point, a horizontal line, an empty file — are handled
explicitly instead of dividing by zero.

Resizing the window recomputes the transform and the draw commands; **the file is parsed
once**, never per redraw.

## 12. Extensibility

The three changes named in the challenge, and what each actually costs.

## 13. Adding a new primitive (e.g. Rectangle)

**Effort: ~30 minutes including tests.** Three additions, zero modifications to existing
behaviour.

1. **Domain** — add the record:

   ```csharp
   public sealed record Rectangle(Point2D Origin, double Width, double Height,
                                  ArgbColor Color, bool Filled)
       : IFillablePrimitive
   {
       public BoundingBox Bounds =>
           BoundingBox.FromCorners(Origin, new Point2D(Origin.X + Width, Origin.Y + Height));
   }
   ```

   Scene bounds need no change — `Scene` unions `IPrimitive.Bounds` and does not know the
   concrete types.

2. **Application** — add a renderer emitting the existing `DrawPolygon` command:

   ```csharp
   public sealed class RectangleRenderer : PrimitiveRenderer<Rectangle>
   {
       protected override void Render(Rectangle r, RenderContext ctx, ICollection<DrawCommand> output)
           => output.Add(new DrawPolygon(
               [ctx.ToScreen(r.BottomLeft), ctx.ToScreen(r.TopLeft),
                ctx.ToScreen(r.TopRight),  ctx.ToScreen(r.BottomRight)],
               ctx.AppearanceFor(r)));   // ← the border/fill rule is applied for you
   }
   ```

3. **Registration** — one line in `PrimitiveRendererRegistry.CreateDefault()`, the single
   place that lists the built-in primitive set:
   `.Register(new RectangleRenderer())`. To read it from JSON, add a `RectangleJsonMapper`
   and one line in `JsonVectorDocumentReader`'s default constructor. Both are list-of-built-ins
   factories, so no logic changes anywhere.

**Unchanged:** `Scene`, `BoundingBox`, `ViewportTransform`, `SceneRenderer`, every existing
renderer, the whole WPF layer — `DrawPolygon` is already painted. This is verified by a
test: `VectorViewer.Application.Tests` defines a rectangle primitive plus renderer entirely
inside the test project and renders it through the *unmodified* production pipeline.

## 14. Adding another input format (e.g. XML)

**Effort: ~1–2 hours including tests**, almost all of it in the new parser.

1. Implement the existing port in Infrastructure:

   ```csharp
   public sealed class XmlVectorDocumentReader : IVectorDocumentReader
   {
       public IReadOnlyCollection<string> SupportedExtensions => [".xml"];
       public Scene Read(Stream stream) { /* XML → domain primitives */ }
   }
   ```

2. Register it next to the JSON reader in the composition root:

   ```csharp
   new VectorDocumentLoader([new JsonVectorDocumentReader(), new XmlVectorDocumentReader()])
   ```

**Unchanged:** the domain model, `VectorDocumentLoader` (it selects by extension), the
render pipeline and the UI — the Open dialog builds its filter from the registered readers'
`FormatName` and `SupportedExtensions`, so the new format appears in it automatically. The existing JSON code is not touched — the two formats are
siblings, not a hierarchy. The application layer never mentions a format at all.

## 15. How primitive selection could be implemented

Not implemented (not required), but the architecture already contains the two pieces that
usually force a redesign: the transform is **invertible**, and primitives are addressable
values.

1. **Identity** — add an `Id` to `IPrimitive` (or key on the record instance, since they
   are immutable and reference-stable within a loaded scene).
2. **Hit-testing in world space** — convert the mouse position once with
   `transform.ToWorld(p)`, then test against primitives. Add `bool Contains(Point2D p,
   double tolerance)` to `IPrimitive`, implemented per shape (distance-to-segment for a
   line, radius comparison for a circle, barycentric test for a triangle). This is pure
   geometry: unit-testable, no WPF, and it belongs naturally next to `Bounds`.
   `Scene` can pre-filter with the cheap `Bounds` check before the exact test, and iterate
   in reverse draw order so the topmost shape wins.
3. **Selection state** — `MainViewModel.SelectedPrimitive`. It lives in the view model, not
   in the domain: selection is a UI concern, and keeping the domain immutable avoids
   change-notification plumbing in geometry types.
4. **Visual feedback** — `SceneRenderer` takes the selected primitive and the renderer
   emits an extra highlight `DrawCommand` (e.g. a `DrawPolygon` around its bounds). No new
   drawing code in WPF.
5. **Inspection panel** — bind a properties panel to `SelectedPrimitive`.

The one thing to watch: hit-test *tolerance* must be converted from pixels to world units
(`ToWorldLength`), or thin lines become unclickable when zoomed out.

**Effort: ~half a day** for hit-testing, selection state, highlight and a simple properties
panel.

## 16. Assumptions

* **`"a": "-1,5; 3,4"` is one point: `x = -1.5`, `y = 3.4`.** `;` separates the coordinates
  and `,` is the decimal separator — confirmed by `"b": "15; -20,3"` in the challenge and by
  a line needing exactly two endpoints. The parser accepts both `,` and `.` as decimal
  separators and is culture-independent, so the app behaves identically on any machine.
  Thousands separators are assumed absent.
* `radius` is a JSON number (`15.0`), i.e. `.` — the JSON grammar, not the string convention.
* `color` is `"A; R; G; B"`, each 0–255, alpha first. Alpha is honoured when drawing.
* Input is valid, per the challenge — no validation layer. The reader still fails loudly
  with a clear message on an unknown `type`, because silently dropping a shape is worse.
* Border width is 1 **world unit**, so it scales with the drawing. It is clamped to a
  minimum of 1 device pixel so borders stay visible on a heavily scaled-down drawing.
* The drawing is only ever scaled **down**, never up (challenge: "1 unit = 1 pixel" at 100 %).

## 17. Trade-offs

* **Four projects for a small viewer.** More structure than the feature set alone needs;
  chosen because each boundary matches a change the challenge explicitly predicts. It would
  be over-engineering for a viewer that was known to be final.
* **The `DrawCommand` indirection** costs one hop between primitive and pixel, and buys
  headless testability plus a second rendering back end for very little code.
* **The draw-command vocabulary is closed** (line/ellipse/polygon) while primitives are
  open. A future Bézier primitive would need a new command *and* a new painter case — a
  deliberate trade that keeps the WPF painter tiny today.
* **No zoom/pan.** Not requested. Fit-to-window is a special case of the transform, so
  adding zoom is a scale override plus a pan offset, not a redesign.
* **Registry lookup by `Type`** does a dictionary hit per primitive. Negligible next to
  drawing, and it avoids reflection entirely.
* **WPF was compiled but not executed during development** (built on macOS, where
  `EnableWindowsTargeting` allows compilation including XAML markup but not launching).
  Mitigated by keeping the UI layer thin and by rendering the same `DrawCommand` list through
  a second, throwaway SVG back end to confirm the output geometry — which is precisely the
  kind of check the UI-independent render model was introduced to make possible.

## 18. Possible improvements

* Zoom and pan (mouse wheel + drag), with a zoom-to-fit reset — the transform already
  supports arbitrary scale.
* Selection and a property inspector, as in §15.
* Anti-aliasing/quality options and export to PNG or SVG — a second `DrawCommand` consumer.
* Virtualisation or geometry batching if drawings grow to tens of thousands of primitives;
  currently one `DrawCommand` list is rebuilt per resize, which is fine at this scale.
* An `IVectorDocumentWriter` counterpart if saving is ever needed — the same port shape.
* Property-based tests (e.g. FsCheck) for the transform round-trip, which is a natural fit
  for `ToWorld(ToScreen(p)) == p`.

## 19. Docker

### What is containerised

The **build and test environment** — and only that. `docker compose run --rm tests` compiles
every project and runs the full suite in a pinned .NET 9 SDK image.

```bash
docker compose run --rm tests     # build + full suite, results in TestResults/
docker compose run --rm shell     # same environment, interactive
docker build --target test .      # image only
```

### What is intentionally *not* containerised, and why

**The viewer itself.** The only application here is a WPF desktop app; WPF is Windows-only,
so it cannot run in a Linux container, and X11 forwarding does not change that. There is no
server, service or web API in this repository — so there is deliberately **no runtime stage,
no `EXPOSE`, no published image and no registry**. Adding a slim runtime image would mean
inventing a deployable that does not exist.

The container still builds the WPF project (`EnableWindowsTargeting` makes it compile on
Linux), so a UI-layer compile break is caught on any machine. Running it remains
`dotnet run --project src/VectorViewer.Wpf` on Windows.

The boundary, stated plainly:

| | Windows only | Any OS |
| --- | --- | --- |
| Run the viewer | ✅ | ❌ |
| Compile the WPF project | ✅ | ✅ (reference assemblies) |
| Run the 164 tests | ✅ | ✅ |

The image runs as root. It is an ephemeral build tool with no listener, no secrets and no
deployment, so dropping privileges would buy nothing and would break the bind-mounted results
directory. A future *runtime* image must not inherit that choice — the Dockerfile says so.

## 20. Continuous Integration

`.github/workflows/ci.yml` runs on pull requests to `master`, pushes to `master`, and
`workflow_dispatch`. Two modes:

* **Pull request — selective.** Only the jobs a change can actually affect.
* **Push to `master` — full.** Everything, always. Selective execution is an optimisation;
  full validation on the default branch is the safety net that makes it safe to have.

### Dependency-aware propagation

The decision logic is in [`scripts/affected.py`](scripts/affected.py), not in YAML, so it can
be unit-tested and run locally (`python3 scripts/affected.py --base origin/master`). It does
not pattern-match folder names — it *derives* the graph:

* project edges from the `ProjectReference` elements in each `.csproj`;
* asset edges from `Content Include`, which is how `samples/example.json` reaches both the
  WPF app **and** the integration tests — an edge no folder heuristic would find;
* container inputs from what the Dockerfile actually copies.

A change is propagated through the reverse-transitive closure, so editing `Domain` does *not*
mean "run domain tests": it runs every test project, the WPF build and the container build.

Three fail-safes escalate to full validation rather than guess: a changed `.csproj` (it may
alter the graph the analysis depends on), a changed file under `scripts/` or
`.github/workflows/` (it defines the pipeline), and any path that cannot be attributed to a
component. [`scripts/test_affected.py`](scripts/test_affected.py) covers all of this —
21 tests, run in CI *before* the analysis is trusted.

### CI matrix

| Change | Validation |
| --- | --- |
| `README.md`, `docs/**`, `CHANGELOG.md` | Analysis only — no build, test or container work |
| `Domain` | All four test projects + WPF build + container |
| `Application` | Application, Infrastructure, Integration tests + WPF build + container |
| `Infrastructure` (incl. the JSON reader) | Infrastructure + Integration tests + WPF build + container |
| `WPF` | WPF build + container (the image builds the whole solution) |
| `samples/example.json` | Integration tests + WPF build + container |
| A test project | That test project only |
| `Dockerfile`, `.dockerignore`, `docker-compose.yml` | Container validation |
| `Directory.*.props`, `global.json`, `*.sln`, any `.csproj` | Full validation |
| `scripts/**`, `.github/workflows/**` | Full validation |
| Push to `master` | Full .NET + container validation |

### Jobs

`detect-changes` → then in parallel: `test` (matrix, one leg per affected test project,
Ubuntu), `build-wpf` (**`windows-latest`**), `docker-build` (Ubuntu) → `ci-success`.

**Runner choice.** Tests and the container run on Ubuntu — cheaper and faster, and nothing
in them is Windows-specific. The WPF build runs on Windows because that is the platform the
application ships to; the Linux compile uses reference assemblies only, so Windows is the
authoritative check of the real toolchain and of XAML markup compilation.

**One leg per test project** rather than a single job: a failure names its layer directly in
the UI, and an unaffected layer costs nothing. Legs run in parallel and share the NuGet cache.

### Caching

NuGet is cached on `**/*.csproj`, `Directory.Packages.props`, `Directory.Build.props` and
`global.json` — every input that can change what gets restored. **No build output is cached**,
so a stale binary can never be resurrected. The container build uses BuildKit with
`type=gha` (`mode=max`) under its own scope. Because the Dockerfile restores from project
files before copying source, and `.dockerignore` keeps documentation out of the context, a
docs-only change cannot invalidate a container layer — verified, not assumed.

### Publishing and permissions

**No image is ever pushed.** No registry is configured and nothing here is deployable, so the
build *is* the validation. Pull requests therefore need no credentials at all, and
`permissions: contents: read` is all any job gets. Concurrency cancels superseded pull-request
runs but never cancels a run on `master`.

No vulnerability scanner is wired in, deliberately: with no published artifact, scanning an
SDK build tool would fail CI on CVEs in tooling that is never deployed. If a deployable
component is added, its runtime image should be scanned and that gate documented here.

### Local parity

`scripts/ci.sh` reproduces the whole thing — analysis self-tests, restore, build, test,
container build. The workflow orchestrates those same commands rather than owning any logic.

### Branch protection

Require the single `CI success` check. It depends on every validation job and uses
`if: always()`, so selectively skipped jobs count as success while failures and cancellations
fail the gate — a required check can never silently vanish because a job was filtered out.
