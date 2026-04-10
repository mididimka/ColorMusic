namespace ColorMusic.Model
{
    public class LightSettings
    {
        public string Name { get; set; } = "Band";

        public double MinFreq { get; set; }
        public double MaxFreq { get; set; }

        // отдельная чувствительность канала
        public double Sensitivity { get; set; } = 1.0;

        public double Gain { get; set; } = 1.0;
        public string ColorHex { get; set; } = "#00FF00";

        public double X { get; set; } = 100;
        public double Y { get; set; } = 200;

        public double Size { get; set; } = 150;

        public LightShape Shape { get; set; } = LightShape.Circle;

        public double Attack { get; set; } = 0.55;
        public double Release { get; set; } = 0.18;
    }
}