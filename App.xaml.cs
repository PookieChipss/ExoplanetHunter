using System.Windows;

namespace ExoplanetHunter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Quick test: load a small slice of the dataset and confirm it worked
        var data = LightCurveDataLoader.LoadCsv("data/exoTrain.csv", maxRows: 50);

        int exoplanetCount = 0;
        foreach (var row in data)
        {
            if (row.IsExoplanet) exoplanetCount++;
        }

        MessageBox.Show(
            $"Loaded {data.Count} rows.\n" +
            $"Exoplanets found: {exoplanetCount}\n" +
            $"First row flux length: {data[0].Flux.Length}"
        );
    }
}