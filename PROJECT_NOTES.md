# Exoplanet Hunter — Project Notes / Handoff

## What this app does
A WPF (.NET 8) desktop app that loads NASA Kepler light curve data (from the
Kaggle "Kepler Labelled Time Series Data" CSV), extracts hand-crafted
statistical features from each star's brightness-over-time curve, and trains
an ML.NET LightGBM binary classifier to predict whether a star hosts a
confirmed exoplanet (replicating the transit-detection method real
astronomers use).

## Current status: functionally complete, UI polish in progress

### Core pipeline (done, working)
- `LightCurveData.cs` — data model (Id, Flux[], IsExoplanet)
- `LightCurveDataLoader.cs` — CSV loader + `Shuffle()` + `SplitTrainTest()`
  (shuffling matters: the raw CSV has all exoplanet rows grouped at the top)
- `LightCurvePreprocessor.cs` — smooth (moving average) → normalize (0-1) →
  downsample (to 200 or 300 points depending on caller)
- `LightCurveFeatures.cs` — extracts 7 features per curve: MeanFlux, MinFlux,
  StdDevFlux, DipDepth, DipCount, DipSymmetryScore, PeriodicityScore
  (periodicity was added later — it measures how REGULARLY spaced dips are,
  since real transits repeat at a consistent orbital period and noise doesn't)
- `LightCurveClassifier.cs` — ML.NET LightGBM wrapper. Train/Evaluate/Predict.

### Known data constraints (important context, don't re-litigate these)
- Dataset is ~5,087 rows, only 37 are confirmed exoplanets (~0.7% positive) —
  heavily imbalanced. This is inherent to the dataset, not a bug.
- We use `Weight` (136x on positive examples) in training to counteract this.
- Best tuned hyperparameters found so far: `NumberOfLeaves=4`,
  `MinimumExampleCountPerLeaf=3`, `NumberOfIterations=50`.
- Best result from tuning: **AUC 66.1%, Recall 27.3%, Precision 4.0%, F1 7.0%**
  on held-out test set (11 real exoplanets in test set). This is an honest,
  legitimate v1 result given only 26 positive training examples — not a bug
  to "fix" to 95%+, just context for further tuning attempts.
- If pushing accuracy further is ever revisited: options discussed were
  oversampling the minority class, trying the raw 200-point vector instead
  of hand-crafted features, or pulling more positive examples from NASA's
  TESS mission data (different raw format, more setup work).

### UI (in progress — this is the active work)
Design direction: "observatory instrument console" — dark space theme,
NOT a generic light/corporate UI. Deliberately chosen over generic AI-design
defaults.

**Design tokens** (in `Styles.xaml`):
- Background: `#0A0E17` (deep space navy, not pure black)
- Panels: `#131A2A`, lighter panel: `#1A2338`
- Grid lines/borders: `#24304A`
- Text: `#E8ECF4` primary, `#7C8AA8` secondary
- Signal accent (amber): `#FF9F5B` — represents starlight / the dip
- Confirm accent (cyan): `#5EEAD4` — "signal detected" state
- Fonts: Bahnschrift SemiBold (display/headers), Segoe UI (body),
  Cascadia Mono (all data/numbers — reinforces "instrument readout" feel)
- Note: WPF has NO native letter-spacing property — we fake the
  "spaced-out label" look by literally typing spaces between letters in
  XAML text content (e.g. "E X O P L A N E T"), not a CSS-style property.

**Current layout** (`MainWindow.xaml` / `MainWindow.xaml.cs`):
- Header bar: pulse dot + title + subtitle
- Left rail: Load Dataset / Train Model buttons + scrollable star catalog list
- Right: light curve "scope" viewport (hand-drawn on Canvas, NOT a charting
  library — gives full control over the glow effect), feature readout strip,
  telemetry strip (status/AUC/prediction)
- Chart rendering: two overlapping Polylines — a thick blurred semi-transparent
  one (glow) behind a thin sharp one (main line), colored amber normally
- `LightCurveDataLoader.Shuffle()` is called on load specifically so the
  Star Catalog list shows a realistic mix, not all-exoplanets-first

### Next planned UI additions (NOT yet built — this is the active task)
User wants a more premium, "engaging" visual feel, inspired by a Behance
concept (space/solar-system UI with glowing planet imagery, orbital motifs).
Agreed scope — all 5 of these:

1. **Starfield background** — faint twinkling dots behind everything
   (`StarfieldCanvas` element already added to XAML, not yet populated in
   code-behind — needs ~80-150 randomly placed small Ellipses with randomized
   opacity, subset animated with looping opacity Storyboards for twinkle)
2. **Live star visualization** — a glowing circular "star" (radial gradient
   Ellipse) next to/near the chart that visually dims and brightens in sync
   with the actual selected light curve's flux values — this is the highest-
   impact addition, ties the abstract chart to something visually intuitive
3. **Orbiting planet indicator** — on a positive prediction, a small glowing
   dot appears and orbits the star visualization (loop animation) — the
   "reveal" moment for a detected signal
4. **Radial glow / vignette effects** behind panels for depth (soft radial
   gradient backgrounds instead of flat panel colors)
5. **Refined header** — orbit-ring motif (thin circular arc) behind the
   pulse dot instead of a plain circle, echoing the orbital theme

`BracketCanvas` element also already exists in the XAML (corner-bracket
viewfinder decoration around the scope) but isn't populated yet either —
still worth doing, was part of an earlier round of polish suggestions.

Feature readout panel elements already exist in XAML but are NOT yet wired
in code-behind: `FeatDipDepth`, `FeatDipCount`, `FeatSymmetry`,
`FeatPeriodicity`, `FeatStdDev` — should populate when a star is selected
(compute `LightCurveFeatures.Extract()` on selection, not just at predict time).

## Tech environment notes
- .NET 8 (`net8.0-windows`), NOT .NET Framework (deliberately switched early
  on since the newer SDK's `dotnet new wpf` template doesn't support net48
  cleanly)
- ML.NET 5.0.0 + Microsoft.ML.LightGbm package
- No OxyPlot or other charting library — chart is hand-drawn on WPF Canvas
- Git repo already initialized and pushed to GitHub (PookieChipss/ExoplanetHunter)
- `.gitignore` excludes `bin/`, `obj/`, `*.user`, and `data/*.csv` (dataset
  not committed — user downloads it manually from Kaggle into `data/`)
- Dataset: Kaggle "Kepler Labelled Time Series Data" — `exoTrain.csv` /
  `exoTest.csv`, first column LABEL (2=exoplanet, 1=not), remaining ~3197
  columns are FLUX.1...FLUX.3197 (raw brightness samples per star)

## Recommended next step for Claude Code
Read this file, then implement the 5 UI additions listed above in
`MainWindow.xaml` / `MainWindow.xaml.cs`, building on the existing
`StarfieldCanvas` and `BracketCanvas` placeholder elements already in the
XAML. Wire up the feature readout panel to populate on row selection.
Test with `dotnet run` after each addition rather than batching all 5 changes
before testing.