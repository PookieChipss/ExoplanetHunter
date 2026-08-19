using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace ExoplanetHunter;

public partial class MainWindow : Window
{
    private List<LightCurveData>? _dataset;
    private LightCurveClassifier? _classifier;
    private LightCurveData? _selectedRow;
    private bool _starfieldInitialized;
    private float[]? _currentFlux;
    private int _starPulseIndex;
    private readonly DispatcherTimer _starPulseTimer;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => RedrawChartIfNeeded();
        Loaded += (_, _) => InitializeStarfield();

        _starPulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _starPulseTimer.Tick += StarPulseTimer_Tick;
    }

    private void StarPulseTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentFlux == null || _currentFlux.Length == 0) return;

        float flux = _currentFlux[_starPulseIndex % _currentFlux.Length];
        _starPulseIndex++;

        double brightness = 0.45 + flux * 0.55;
        StarViz.Opacity = brightness;
        StarVizGlow.Opacity = 0.4 + flux * 0.6;

        double scale = 0.85 + flux * 0.15;
        StarViz.RenderTransform = new ScaleTransform(scale, scale, StarViz.Width / 2, StarViz.Height / 2);
    }

    // ============ STARFIELD BACKDROP ============

    private void InitializeStarfield()
    {
        if (_starfieldInitialized) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        _starfieldInitialized = true;

        var rnd = new Random();
        var starBrush = (Brush)FindResource("TextPrimaryBrush");
        int starCount = rnd.Next(80, 151);

        for (int i = 0; i < starCount; i++)
        {
            double size = rnd.NextDouble() * 1.6 + 0.8;
            double x = rnd.NextDouble() * ActualWidth;
            double y = rnd.NextDouble() * ActualHeight;
            double baseOpacity = rnd.NextDouble() * 0.5 + 0.15;

            var star = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = starBrush,
                Opacity = baseOpacity
            };
            Canvas.SetLeft(star, x);
            Canvas.SetTop(star, y);
            StarfieldCanvas.Children.Add(star);

            // ~1 in 3 stars twinkle — desynced via random begin delay and duration
            if (rnd.Next(3) == 0)
            {
                var twinkle = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = baseOpacity,
                    To = Math.Max(0.05, baseOpacity - 0.35),
                    Duration = TimeSpan.FromSeconds(rnd.NextDouble() * 2.5 + 1.5),
                    AutoReverse = true,
                    RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromSeconds(rnd.NextDouble() * 4.0)
                };
                star.BeginAnimation(OpacityProperty, twinkle);
            }
        }
    }

    private void LoadDatasetButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            StatusText.Text = "Loading...";
            _dataset = LightCurveDataLoader.Shuffle(LightCurveDataLoader.LoadCsv(dialog.FileName));

            RowListBox.ItemsSource = _dataset.Select(r =>
                $"#{r.Id,-5} {(r.IsExoplanet ? "EXOPLANET" : "no signal")}");

            StatusText.Text = $"{_dataset.Count} rows loaded";
            TrainButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load dataset:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "Load failed";
        }
    }

    private void RowListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_dataset == null || RowListBox.SelectedIndex < 0) return;

        _selectedRow = _dataset[RowListBox.SelectedIndex];
        DrawLightCurve(_selectedRow);

        PredictButton.IsEnabled = _classifier != null;
        PredictionText.Text = "—";

        OrbitPlanetGroup.Visibility = Visibility.Collapsed;
        OrbitRotate.BeginAnimation(RotateTransform.AngleProperty, null);
    }

    private void TrainButton_Click(object sender, RoutedEventArgs e)
    {
        if (_dataset == null) return;

        StatusText.Text = "Training...";
        TrainButton.IsEnabled = false;

        var (trainRaw, testRaw) = LightCurveDataLoader.SplitTrainTest(_dataset);

        var trainFeatures = trainRaw
            .Select(r =>
            {
                var f = LightCurveFeatures.Extract(LightCurvePreprocessor.Process(r.Flux), r.IsExoplanet);
                f.Weight = r.IsExoplanet ? 136f : 1f;
                return f;
            })
            .ToList();

        var testFeatures = testRaw
            .Select(r => LightCurveFeatures.Extract(LightCurvePreprocessor.Process(r.Flux), r.IsExoplanet))
            .ToList();

        _classifier = new LightCurveClassifier();
        _classifier.Train(trainFeatures, numberOfLeaves: 4, minExamplesPerLeaf: 3);

        var metrics = _classifier.Evaluate(testFeatures);

        AucText.Text = metrics.AreaUnderRocCurve.ToString("P1");
        StatusText.Text = $"Trained on {trainFeatures.Count} rows";

        PredictButton.IsEnabled = _selectedRow != null;
    }

    private void PredictButton_Click(object sender, RoutedEventArgs e)
    {
        if (_classifier == null || _selectedRow == null) return;

        var processed = LightCurvePreprocessor.Process(_selectedRow.Flux);
        var features = LightCurveFeatures.Extract(processed, _selectedRow.IsExoplanet);

        var prediction = _classifier.Predict(features);

        PredictionText.Text = prediction.IsExoplanet
            ? $"SIGNAL DETECTED ({prediction.Probability:P0})"
            : $"NO SIGNAL ({(1 - prediction.Probability):P0})";

        PredictionText.Foreground = prediction.IsExoplanet
            ? (Brush)FindResource("ConfirmCyanBrush")
            : (Brush)FindResource("TextSecondaryBrush");

        if (prediction.IsExoplanet)
        {
            OrbitPlanetGroup.Visibility = Visibility.Visible;
            var orbit = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = TimeSpan.FromSeconds(6),
                RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
            };
            OrbitRotate.BeginAnimation(RotateTransform.AngleProperty, orbit);
        }
        else
        {
            OrbitPlanetGroup.Visibility = Visibility.Collapsed;
            OrbitRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }

    // ============ CHART DRAWING ============

    private void RedrawChartIfNeeded()
    {
        if (_selectedRow != null) DrawLightCurve(_selectedRow);
    }

    private void DrawLightCurve(LightCurveData row)
    {
        EmptyStateText.Visibility = Visibility.Collapsed;

        double width = ChartCanvas.ActualWidth;
        double height = ChartCanvas.ActualHeight;
        if (width <= 0 || height <= 0) return;

        ChartCanvas.Children.Clear();
        GridCanvas.Children.Clear();

        var processed = LightCurvePreprocessor.Process(row.Flux, targetLength: 300);

        DrawGrid(width, height);
        DrawCurve(processed, width, height, row.IsExoplanet);

        _currentFlux = processed;
        _starPulseIndex = 0;
        StarVizContainer.Visibility = Visibility.Visible;
        _starPulseTimer.Start();
    }

    private void DrawGrid(double width, double height)
    {
        var gridBrush = (Brush)FindResource("GridLineBrush");

        // Horizontal grid lines
        for (int i = 0; i <= 4; i++)
        {
            double y = height * i / 4.0;
            var line = new Line
            {
                X1 = 0, X2 = width, Y1 = y, Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1,
                Opacity = 0.5
            };
            GridCanvas.Children.Add(line);
        }

        // Vertical grid lines
        for (int i = 0; i <= 8; i++)
        {
            double x = width * i / 8.0;
            var line = new Line
            {
                X1 = x, X2 = x, Y1 = 0, Y2 = height,
                Stroke = gridBrush,
                StrokeThickness = 1,
                Opacity = 0.5
            };
            GridCanvas.Children.Add(line);
        }
    }

    private void DrawCurve(float[] flux, double width, double height, bool isExoplanet)
    {
        double padding = 20;
        double plotHeight = height - (padding * 2);
        double plotWidth = width;

        var points = new PointCollection();
        for (int i = 0; i < flux.Length; i++)
        {
            double x = (i / (double)(flux.Length - 1)) * plotWidth;
            // Flip Y since screen coordinates go top-to-bottom but we want low flux = low on screen
            double y = padding + (1 - flux[i]) * plotHeight;
            points.Add(new Point(x, y));
        }

        var curveColor = isExoplanet
            ? (Color)FindResource("SignalAmberColor")
            : (Color)FindResource("ConfirmCyanColor");

        // Glow layer — a thicker, blurred, semi-transparent line behind the main line
        var glowLine = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(curveColor) { Opacity = 0.35 },
            StrokeThickness = 6,
            StrokeLineJoin = PenLineJoin.Round,
            Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 8 }
        };

        // Sharp main line on top
        var mainLine = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(curveColor),
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round
        };

        ChartCanvas.Children.Add(glowLine);
        ChartCanvas.Children.Add(mainLine);
    }
}