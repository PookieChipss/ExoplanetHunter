using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ExoplanetHunter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var allData = LightCurveDataLoader.LoadCsv("data/exoTrain.csv");
            var (trainRaw, testRaw) = LightCurveDataLoader.SplitTrainTest(allData);

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

            // Try several parameter combinations and record how each performs
            var leafOptions = new[] { 4, 8, 16 };
            var minExampleOptions = new[] { 1, 3, 5 };

            var results = new StringBuilder();
            results.AppendLine($"Train exoplanets: {trainFeatures.Count(f => f.Label)} | Test exoplanets: {testFeatures.Count(f => f.Label)}\n");
            results.AppendLine("Leaves | MinEx | AUC     | Precision | Recall  | F1");

            foreach (var leaves in leafOptions)
            {
                foreach (var minEx in minExampleOptions)
                {
                    var classifier = new LightCurveClassifier();
                    classifier.Train(trainFeatures, numberOfLeaves: leaves, minExamplesPerLeaf: minEx);
                    var metrics = classifier.Evaluate(testFeatures);

                    results.AppendLine(
                        $"{leaves,6} | {minEx,5} | {metrics.AreaUnderRocCurve:P1} | " +
                        $"{metrics.PositivePrecision:P1}    | {metrics.PositiveRecall:P1}  | {metrics.F1Score:P1}"
                    );
                }
            }

            MessageBox.Show(results.ToString());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"ERROR:\n{ex.GetType().Name}\n{ex.Message}\n\n{ex.StackTrace}");
        }
    }
}