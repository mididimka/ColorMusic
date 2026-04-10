using System.IO;
using System.Text.Json;
using ColorMusic.Model;

namespace ColorMusic.Storage
{
    public static class SettingsStorage
    {
        public static string FilePath => "settings.json";

        public static void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(FilePath, json);
        }

        public static AppSettings LoadOrDefault()
        {
            if (!File.Exists(FilePath))
                return CreateDefault();

            try
            {
                var json = File.ReadAllText(FilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings == null || settings.Lights.Count != 8)
                    return CreateDefault();

                if (settings.GlobalSensitivity <= 0)
                    settings.GlobalSensitivity = 5.0;

                if (settings.AutoLevelTarget <= 0)
                    settings.AutoLevelTarget = 0.35;

                if (settings.BaseOpacity < 0)
                    settings.BaseOpacity = 0.85;

                if (settings.PulseStrength <= 0)
                    settings.PulseStrength = 1.0;

                if (settings.StrobeStrength <= 0)
                    settings.StrobeStrength = 1.0;

                if (settings.NeonStrength <= 0)
                    settings.NeonStrength = 1.0;

                if (settings.BeatFlashStrength <= 0)
                    settings.BeatFlashStrength = 1.0;

                foreach (var l in settings.Lights)
                {
                    if (l.Sensitivity <= 0)
                        l.Sensitivity = 1.0;

                    if (string.IsNullOrWhiteSpace(l.Name) || l.Name.StartsWith("Band "))
                    {
                        int index = settings.Lights.IndexOf(l) + 1;
                        l.Name = $"Канал {index}";
                    }
                }

                return settings;
            }
            catch
            {
                return CreateDefault();
            }
        }

        private static AppSettings CreateDefault()
        {
            var s = new AppSettings
            {
                MasterGain = 1.0,
                GlobalSensitivity = 5.0,
                BaseOpacity = 0.85,
                Mode = ReactionMode.Soft,
                SmokeEnabled = true,
                AutoLevelEnabled = true,
                AutoLevelTarget = 0.35,
                PulseEnabled = true,
                PulseStrength = 1.0,
                StrobeEnabled = false,
                StrobeStrength = 1.0,
                NeonGlowEnabled = true,
                NeonStrength = 1.0,
                BeatFlashEnabled = true,
                BeatFlashStrength = 1.0
            };

            double[][] bands =
            {
                new[] { 20.0, 60.0 },
                new[] { 60.0, 150.0 },
                new[] { 150.0, 400.0 },
                new[] { 400.0, 800.0 },
                new[] { 800.0, 1500.0 },
                new[] { 1500.0, 3000.0 },
                new[] { 3000.0, 6000.0 },
                new[] { 6000.0, 16000.0 }
            };

            string[] colors =
            {
                "#FF2A2A", "#FF7B1A", "#FFE600", "#7CFF00",
                "#00FF66", "#00E5FF", "#2A5BFF", "#FF00E5"
            };

            for (int i = 0; i < 8; i++)
            {
                s.Lights.Add(new LightSettings
                {
                    Name = $"Канал {i + 1}",
                    MinFreq = bands[i][0],
                    MaxFreq = bands[i][1],
                    Gain = 1.0,
                    Sensitivity = 1.0,
                    ColorHex = colors[i],
                    X = 60 + i * 140,
                    Y = 320,
                    Size = 150,
                    Shape = LightShape.Circle,
                    Attack = 0.55,
                    Release = 0.18
                });
            }

            return s;
        }
    }
}