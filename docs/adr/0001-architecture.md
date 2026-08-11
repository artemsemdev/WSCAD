# ADR-0001: Layered core with WPF, JSON and rendering as replaceable adapters

* **Status:** Accepted
* **Date:** 2026-08-11
* **Context:** WSCAD code challenge — vector graphic viewer

## Context

The viewer must read primitives from a JSON file and draw them in a WPF window. The
challenge states three likely future changes: a new primitive type (rectangle), a new
input format (XML), and interactive selection of primitives.

The naive implementation — deserialize in `MainWindow.xaml.cs`, `switch` on a type string,
draw straight onto a `Canvas` — works for the sample file and fails all three extension
requirements at once. Every change would land in the same file, geometry would be
untestable without a UI thread, and the scaling maths would be entangled with layout.

The opposite failure mode is just as real: a full Clean Architecture with empty
`Application`/`Common`/`Abstractions` projects, a DI container, MediatR handlers and a
repository interface per file format would be far more machinery than a three-primitive
viewer justifies.

## Decision

Split along the axes that the challenge says will change, and only those.

1. **Domain** — geometry and primitives, no dependencies. Pure values.
2. **Application** — viewport transformation, the render model, primitive→command
   renderers, and the reader *port*.
3. **Infrastructure** — the JSON *adapter* implementing that port.
4. **WPF** — presentation adapter: paints the render model, hosts the view model.

Concretely, three seams are treated as replaceable adapters:

* **Parsing** — `IVectorDocumentReader` is defined by the application layer and
  implemented by infrastructure. The core never names JSON.
* **Rendering** — renderers emit `DrawCommand` values in screen space rather than calling
  a graphics API. WPF is one consumer; a bitmap exporter or an SVG writer would be another.
* **Primitive dispatch** — a type-keyed renderer registry, so new primitives are
  registered rather than switched on.

## Rationale

* **The core is testable without a UI.** Scale-to-fit, Y inversion, centring and
  colour/fill decisions are the parts most likely to contain bugs, and they are exactly
  the parts that are hardest to test through a WPF window. Keeping them in plain
  libraries makes them deterministic unit tests instead of UI automation.
* **The registry matches the stated direction of change.** A visitor optimises for adding
  operations; the challenge asks for adding *types*. Choosing the pattern that matches the
  expected change is the whole point of picking one.
* **The render model is the testability seam.** Without it, "a filled circle draws a
  border and a fill" can only be verified by rendering pixels. With it, that is a plain
  assertion on a `DrawEllipse` value.
* **WPF stays at the edge.** The domain has no `System.Windows` reference, so the same
  core could drive a WinUI, Avalonia or server-side renderer unchanged. This also lets the
  entire non-UI solution build and test on macOS/Linux CI agents.

## Consequences

**Positive**

* Rectangle = one record + one renderer + one registration line.
* XML = one new `IVectorDocumentReader`; existing JSON code is untouched.
* Selection = hit-test in world space via `ToWorld`; the transform is already invertible.
* ~90 % of the logic is covered by fast, deterministic tests with no mocking.

**Negative**

* Four projects and an intermediate render model are more indirection than a single-file
  solution. Accepted deliberately: each boundary maps to a change the challenge predicts.
* `DrawCommand` is an extra hop between primitive and pixel. Accepted: it buys testability
  and a second rendering back end for very little code.
* The `DrawCommand` vocabulary is closed. A primitive needing a genuinely new geometry
  kind (e.g. a Bézier path) would extend it — a deliberate trade of openness for a small,
  stable painter.

## Alternatives considered

| Alternative | Rejected because |
| --- | --- |
| Everything in the WPF project | Fails all three extension points; geometry untestable. |
| Visitor over primitives | Optimises the wrong axis — new primitives become expensive. |
| Primitives draw themselves (`primitive.Draw(canvas)`) | Couples the domain to WPF; kills headless tests and any second back end. |
| Full Clean Architecture + DI container + MediatR | Ceremony far beyond a 3-primitive viewer; empty layers signal cargo-culting. |
| Retained WPF `Shape` objects on a `Canvas` | One visual per primitive scales poorly and hides the transform in XAML layout. |
