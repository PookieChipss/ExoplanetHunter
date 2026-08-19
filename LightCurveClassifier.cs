using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace ExoplanetHunter
{
    public class LightCurvePrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsExoplanet { get; set; }

        public float Probability { get; set; }

        public float Score { get; set; }
    }

    public class LightCurveClassifier
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;

        public LightCurveClassifier(int seed = 42)
        {
            _mlContext = new MLContext(seed: seed);
        }

        public void Train(List<LightCurveFeatures> trainingData, int numberOfLeaves = 4, int minExamplesPerLeaf = 3)
        {
            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms
                .Concatenate("Features",
                    nameof(LightCurveFeatures.MeanFlux),
                    nameof(LightCurveFeatures.MinFlux),
                    nameof(LightCurveFeatures.StdDevFlux),
                    nameof(LightCurveFeatures.DipDepth),
                    nameof(LightCurveFeatures.DipCount),
                    nameof(LightCurveFeatures.DipSymmetryScore),
                    nameof(LightCurveFeatures.PeriodicityScore))
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                    new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options
                    {
                        LabelColumnName = nameof(LightCurveFeatures.Label),
                        FeatureColumnName = "Features",
                        ExampleWeightColumnName = nameof(LightCurveFeatures.Weight),
                        MinimumExampleCountPerLeaf = minExamplesPerLeaf,
                        NumberOfLeaves = numberOfLeaves,
                        NumberOfIterations = 50
                    }));

            _model = pipeline.Fit(dataView);
        }

        public CalibratedBinaryClassificationMetrics Evaluate(List<LightCurveFeatures> testData)
        {
            if (_model == null) throw new InvalidOperationException("Call Train() first.");

            IDataView dataView = _mlContext.Data.LoadFromEnumerable(testData);
            IDataView predictions = _model.Transform(dataView);

            return _mlContext.BinaryClassification.Evaluate(
                predictions, labelColumnName: nameof(LightCurveFeatures.Label));
        }

        public LightCurvePrediction Predict(LightCurveFeatures features)
        {
            if (_model == null) throw new InvalidOperationException("Call Train() first.");

            var engine = _mlContext.Model.CreatePredictionEngine<LightCurveFeatures, LightCurvePrediction>(_model);
            return engine.Predict(features);
        }
    }
}