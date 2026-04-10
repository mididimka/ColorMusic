using System;
using System.Windows.Media;
using System.Windows.Shapes;
using ColorMusic.Model;

namespace ColorMusic.Rendering
{
    public class SpotlightControl
    {
        public Shape ShapeElement { get; private set; }
        public LightSettings Settings { get; }

        private double _currentValue;
        private double _pulsePhase;
        private double _strobeValue;
        private double _beatFlashValue;

        public SpotlightControl(LightSettings settings)
        {
            Settings = settings;
            ShapeElement = CreateShape();
        }

        private Shape CreateShape()
        {
            Shape s = Settings.Shape switch
            {
                LightShape.Square => new Rectangle { RadiusX = 18, RadiusY = 18 },
                LightShape.Triangle => new Polygon
                {
                    Points = new PointCollection
                    {
                        new System.Windows.Point(Settings.Size / 2, 0),
                        new System.Windows.Point(Settings.Size, Settings.Size),
                        new System.Windows.Point(0, Settings.Size)
                    }
                },
                _ => new Ellipse()
            };

            s.Width = Settings.Size;
            s.Height = Settings.Size;

            ApplyBrush(s);

            s.Opacity = 0.05;

            s.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 80,
                ShadowDepth = 0,
                Opacity = 0.65,
                Color = (Color)ColorConverter.ConvertFromString(Settings.ColorHex)
            };

            return s;
        }

        private void ApplyBrush(Shape s)
        {
            var baseColor = (Color)ColorConverter.ConvertFromString(Settings.ColorHex);

            var brush = new RadialGradientBrush
            {
                GradientOrigin = new System.Windows.Point(0.42, 0.42),
                Center = new System.Windows.Point(0.5, 0.5),
                RadiusX = 0.92,
                RadiusY = 0.92
            };

            brush.GradientStops.Add(new GradientStop(baseColor, 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(170, baseColor.R, baseColor.G, baseColor.B), 0.28));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B), 1));

            s.Fill = brush;
        }

        public void UpdateBrightness(
            double targetValue,
            bool pulseEnabled,
            bool strobeEnabled,
            bool neonEnabled,
            bool beatFlashEnabled,
            bool strobeTrigger,
            bool beatTrigger,
            double baseOpacity,
            double pulseStrength,
            double strobeStrength,
            double neonStrength,
            double beatStrength)
        {
            targetValue = Math.Clamp(targetValue, 0, 1);
            baseOpacity = Math.Clamp(baseOpacity, 0, 1.0);
            pulseStrength = Math.Clamp(pulseStrength, 0, 3);
            strobeStrength = Math.Clamp(strobeStrength, 0, 3);
            neonStrength = Math.Clamp(neonStrength, 0, 3);
            beatStrength = Math.Clamp(beatStrength, 0, 3);

            double attack = Math.Clamp(Math.Max(Settings.Attack, 0.72), 0.30, 0.99);
            double release = Math.Clamp(Math.Max(Settings.Release, 0.42), 0.16, 0.92);

            double speed = targetValue > _currentValue ? attack : release;
            _currentValue += (targetValue - _currentValue) * speed;
            _currentValue = Math.Clamp(_currentValue, 0, 1);

            double pulse = 0;
            if (pulseEnabled && pulseStrength > 0)
            {
                _pulsePhase += 0.32 + _currentValue * (0.18 + 0.05 * pulseStrength);
                pulse = Math.Sin(_pulsePhase) * (0.14 * pulseStrength) * Math.Max(0.2, _currentValue);
            }

            if (strobeEnabled && strobeStrength > 0 && strobeTrigger)
                _strobeValue = 1.0 * strobeStrength;
            else
                _strobeValue *= 0.76;

            if (beatFlashEnabled && beatStrength > 0 && beatTrigger)
                _beatFlashValue = 1.0 * beatStrength;
            else
                _beatFlashValue *= 0.84;

            double final = _currentValue + pulse;
            final += _strobeValue * 0.85;
            final += _beatFlashValue * 0.60;
            final = Math.Clamp(final, 0, 1);

            ShapeElement.Opacity = final * baseOpacity;

            if (ShapeElement.Effect is System.Windows.Media.Effects.DropShadowEffect ds)
            {
                double neonMul = neonEnabled ? (0.68 + 0.32 * neonStrength) : 0.55;
                ds.Opacity = (0.24 + final * (0.70 + 0.25 * neonStrength)) * neonMul * Math.Max(0.15, baseOpacity);
                ds.BlurRadius = neonEnabled
                    ? 28 + final * (80 + 45 * neonStrength) + _beatFlashValue * (10 + 8 * beatStrength) + _strobeValue * (15 + 10 * strobeStrength)
                    : 20 + final * 60;
            }
        }

        public void UpdateVisual()
        {
            ShapeElement.Width = Settings.Size;
            ShapeElement.Height = Settings.Size;

            ApplyBrush(ShapeElement);

            if (ShapeElement.Effect is System.Windows.Media.Effects.DropShadowEffect ds)
                ds.Color = (Color)ColorConverter.ConvertFromString(Settings.ColorHex);
        }
    }
}