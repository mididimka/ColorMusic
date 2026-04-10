using System.Collections.Generic;

namespace ColorMusic.Model
{
    public class AppSettings
    {
        public double MasterGain { get; set; } = 1.0;
        public double GlobalSensitivity { get; set; } = 5.0;
        public double BaseOpacity { get; set; } = 0.85;

        public bool ShowSettingsPanel { get; set; } = true;
        public bool ShowSpectrumWindow { get; set; } = true;

        public ReactionMode Mode { get; set; } = ReactionMode.Soft;

        public bool SmokeEnabled { get; set; } = true;
        public bool AutoLevelEnabled { get; set; } = true;
        public double AutoLevelTarget { get; set; } = 0.35;

        public bool PulseEnabled { get; set; } = true;
        public double PulseStrength { get; set; } = 1.0;

        public bool StrobeEnabled { get; set; } = false;
        public double StrobeStrength { get; set; } = 1.0;

        public bool NeonGlowEnabled { get; set; } = true;
        public double NeonStrength { get; set; } = 1.0;

        public bool BeatFlashEnabled { get; set; } = true;
        public double BeatFlashStrength { get; set; } = 1.0;

        public List<LightSettings> Lights { get; set; } = new();
    }
}