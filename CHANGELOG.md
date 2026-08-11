# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Vector graphic viewer: reads `line`, `circle` and `triangle` primitives from a JSON file
  and draws them in a WPF window, using Cartesian coordinates with the Y axis pointing up.
- Automatic fit-to-window: a drawing larger than the window is scaled down proportionally,
  preserving its aspect ratio and staying centred. A drawing smaller than the window is
  shown at 100 %, where one virtual unit is one pixel.
- ARGB colour support including alpha, so overlapping shapes blend. `filled: true` draws a
  border and a fill; `filled: false` draws the border only.
- Extension points for the three changes named in the challenge: new primitives register a
  renderer, new input formats implement `IVectorDocumentReader`, and the viewport transform
  is invertible so hit-testing for selection can be added without redesign.
- Reproducible build and test environment via Docker, so the solution can be verified with
  no local .NET SDK. The container builds every project, WPF included, and runs the full
  suite; running the viewer still requires Windows.
- Architecture tests asserting that the domain and application layers reference no UI
  framework, so the boundary the design depends on cannot erode unnoticed.

### Notes

- Coordinate strings use `;` between components and `,` as the decimal separator, so
  `"-1,5; 3,4"` is the single point `(-1.5, 3.4)`. Parsing is culture-independent and also
  accepts `.`, so a file renders identically on any machine.
