# Exoplanet Hunter 🔭

A WPF (.NET 8) desktop app that detects likely exoplanets from NASA Kepler
light curve data — replicating the transit-detection method real astronomers
use, with a custom-built "observatory console" UI.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![WPF](https://img.shields.io/badge/UI-WPF-0078D4)
![ML.NET](https://img.shields.io/badge/ML-ML.NET%20%2F%20LightGBM-orange)

---

## What it does

Feed it a star's brightness measurements over time (a "light curve"), and it
predicts whether that star hosts a confirmed exoplanet — by detecting the
subtle, periodic dip in brightness that happens when a planet passes in
front of its star (the same "transit method" used in real astronomy).

- Loads and visualizes raw Kepler light curve data
- Extracts statistical features that describe a transit's signature (dip
  depth, dip count, symmetry, periodicity)
- Trains an ML.NET LightGBM binary classifier on those features
- Runs live predictions on any star in the catalog, with a glowing
  hand-drawn light curve visualization

---

## Screenshots

*(Add a screenshot or short GIF of the app here once you have one — this is
one of the first things people look at.)*

---

## Tech stack

- **.NET 8** (WPF) — desktop UI
- **ML.NET 5.0 + LightGBM** — binary classification
- Hand-drawn Canvas-based charting (no external charting library) for full
  control over the visual design
- Custom XAML design system — dark "observatory console" theme, not the
  default WPF look

---

## How it works

Raw CSV row (3,197 brightness readings)
↓
Preprocess: smooth → normalize → downsample
↓
Extract 7 features: mean flux, min flux, std dev,
dip depth, dip count, symmetry, periodicity
↓
Train / predict with ML.NET LightGBM


### Why features instead of raw deep learning?

The dataset is small and heavily imbalanced — only 37 confirmed exoplanets
out of ~5,000 stars (~0.7%). A deep learning model would very likely overfit
on that few positive examples. Instead, this project hand-engineers features
that describe *what a real transit looks like* (a deep, repeating, symmetric
dip), giving a much easier learning problem for the amount of data available.

### Model performance (honest numbers)

On a held-out test set (11 real exoplanets, 1,017 rows):

| Metric | Score |
|---|---|
| AUC | 66.1% |
| Recall | 27.3% |
| Precision | 4.0% |
| F1 | 7.0% |

**What this means in plain terms:** the model catches roughly 1 in 4 real
exoplanets in the test set, and is right about 1 in 25 times when it flags
one. This is a legitimate result given the data constraints — not a
research-grade detector, but a working replication of the core detection
logic with transparently reported performance. See [Future improvements](#future-improvements)
for how this could be pushed further.

---

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Windows (WPF is Windows-only)
- A free [Kaggle](https://www.kaggle.com) account (to download the dataset)

### Setup

1. **Clone the repo**

git clone https://github.com/PookieChipss/ExoplanetHunter.git
cd ExoplanetHunter


2. **Download the dataset** — [Kepler Labelled Time Series Data](https://www.kaggle.com/datasets/keplersmachines/kepler-labelled-time-series-data)
   on Kaggle. Download and unzip, then place `exoTrain.csv` (and optionally
   `exoTest.csv`) into a `data/` folder in the project root.

   > Note: the dataset isn't included in this repo (see `.gitignore`) since
   > it's large and belongs to its original Kaggle source.

3. **Restore and run**

dotnet restore
dotnet run


4. In the app: click **LOAD DATASET**, select `exoTrain.csv`, then
   **TRAIN MODEL**. Once training finishes, click any star in the catalog
   to view its light curve, and **RUN PREDICTION** to see the model's call.

---

## Project structure

ExoplanetHunter/
├── LightCurveData.cs # Data model for one star's brightness-over-time
├── LightCurveDataLoader.cs # CSV loader, shuffle, train/test split
├── LightCurvePreprocessor.cs # Smoothing, normalizing, downsampling
├── LightCurveFeatures.cs # Feature extraction (dip depth, periodicity, etc.)
├── LightCurveClassifier.cs # ML.NET LightGBM training/prediction
├── Styles.xaml # Design system: colors, fonts, control styles
├── MainWindow.xaml / .cs # Main UI — the observatory console
└── data/ # (not committed) place exoTrain.csv here


---

## Design

The UI is themed as an "observatory instrument console" rather than a
typical form-based app — dark navy background, amber signal accents, and a
hand-drawn glowing light curve chart instead of a generic charting library.

| Token | Value |
|---|---|
| Background | `#0A0E17` |
| Panel | `#131A2A` |
| Grid lines | `#24304A` |
| Text primary | `#E8ECF4` |
| Signal accent (amber) | `#FF9F5B` |
| Confirm accent (cyan) | `#5EEAD4` |

Typography: **Bahnschrift** for headers, **Segoe UI** for body text,
**Cascadia Mono** for all numeric/data readouts.

---

## Future improvements

- **More training data** — pull additional confirmed exoplanets from NASA's
  TESS mission archive to reduce the severe class imbalance
- **Oversampling** the minority class instead of relying solely on class
  weighting
- **Raw-sequence deep learning** (CNN via ONNX) as a comparison against the
  current hand-crafted-feature approach, if enough data is added to support it
- **Batch scan mode** — rank an entire uploaded catalog by predicted
  probability instead of predicting one star at a time
- **Persist trained models** to disk so retraining isn't required every launch

---

## Acknowledgements

- Dataset: [Kepler Labelled Time Series Data](https://www.kaggle.com/datasets/keplersmachines/kepler-labelled-time-series-data)
  on Kaggle, derived from NASA's Kepler mission
- Built with [ML.NET](https://dotnet.microsoft.com/apps/machinelearning-ai/ml-dotnet)

