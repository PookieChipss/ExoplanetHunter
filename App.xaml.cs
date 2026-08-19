using System.Linq;
using System.Windows;

namespace ExoplanetHunter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Quick test: load a small slice of the dataset and confirm it worked
        var data = LightCurveDataLoader.LoadCsv("data/exoTrain.csv", maxRows: 50);

        // Compare features between a known exoplanet and a known non-exoplanet
        var exoplanetRow = data.First(r => r.IsExoplanet);
        var nonExoplanetRow = data.First(r => !r.IsExoplanet);

        var exoFeatures = LightCurveFeatures.Extract(
            LightCurvePreprocessor.Process(exoplanetRow.Flux), true);

        var nonExoFeatures = LightCurveFeatures.Extract(
            LightCurvePreprocessor.Process(nonExoplanetRow.Flux), false);

        MessageBox.Show(
            $"EXOPLANET row features:\n" +
            $"  Dip Depth: {exoFeatures.DipDepth:F4}\n" +
            $"  Dip Count: {exoFeatures.DipCount}\n" +
            $"  Symmetry: {exoFeatures.DipSymmetryScore:F4}\n\n" +
            $"NON-EXOPLANET row features:\n" +
            $"  Dip Depth: {nonExoFeatures.DipDepth:F4}\n" +
            $"  Dip Count: {nonExoFeatures.DipCount}\n" +
            $"  Symmetry: {nonExoFeatures.DipSymmetryScore:F4}"
        );
    }
}