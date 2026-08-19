namespace ExoplanetHunter
{
    //Represent one star's brightness measurement over time (a "Light Curve"),
    //Plus it's label (from the Kaggle dataset: Label 2 = confirmed exoplanet, Label 1 = not).

    public class LightCurveData
    {
        public int Id { get; set; }
        public float [] Flux { get; set; } = System.Array.Empty<float>();
        public bool IsExoplanet {get; set; }
    }
}