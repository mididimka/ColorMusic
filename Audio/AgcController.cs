using System;

namespace ColorMusic.Audio
{
    public class AgcController
    {
        public bool Enabled { get; set; } = true;

        public double TargetLevel { get; set; } = 0.35;
        public double Speed { get; set; } = 0.04;

        public double MinGain { get; set; } = 0.2;
        public double MaxGain { get; set; } = 12.0;

        public double CurrentGain { get; private set; } = 1.0;

        public double Update(double measuredLevel)
        {
            if (!Enabled)
                return CurrentGain;

            measuredLevel = Math.Clamp(measuredLevel, 0.00001, 1.0);

            double error = TargetLevel - measuredLevel;

            CurrentGain += error * Speed;

            if (CurrentGain < MinGain) CurrentGain = MinGain;
            if (CurrentGain > MaxGain) CurrentGain = MaxGain;

            return CurrentGain;
        }
    }
}