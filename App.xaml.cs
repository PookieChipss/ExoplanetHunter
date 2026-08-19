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

        var raw = data[0].Flux;
        var processed = LightCurvePreprocessor.Process(raw);

        MessageBox.Show(
            $"Raw flux length: {raw.Length}\n" +
            $"Processed flux length: {processed.Length}\n" +
            $"Raw first value: {raw[0]:F2}\n" +
            $"Processed first value: {processed[0]:F4}\n" +
            $"Processed min: {processed.Min():F4}\n" +
            $"Processed max: {processed.Max():F4}"
        );
    }
}