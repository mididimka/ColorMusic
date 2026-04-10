using System;
using System.Windows.Media;
using System.Windows.Shapes;
using ColorMusic.Model;

namespace ColorMusic.Rendering
{
    public class BeamControl
    {
        public Polygon BeamShape { get; private set; }
        public LightSettings Settings { get; }

        private double _currentValue;

        public BeamControl(LightSettings settings)
        {
            Settings = settings;
            BeamShape = CreateBeam();
        }

        private Polygon CreateBeam()
        {
            double width = Settings.Size * 0.8;
            double height = Settings.Size * 2.7;

            var poly = new Polygon
            {
                Points = new PointCollection
                {
                    new System.Windows.Point(width/2, 0),
                    new System.Windows.Point(width, height),
                    new System.Windows.Point(0, height)
                }
            };

            ApplyBrush(poly);

            poly.Opacity = 0.0;
            poly.IsHitTestVisible = false;

            return poly;
        }

        private void ApplyBrush(Polygon p)
        {
            var baseColor = (Color)ColorConverter.ConvertFromString(Settings.ColorHex);

            var brush = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0.5, 0),
                EndPoint = new System.Windows.Point(0.5, 1)
            };

            brush.GradientStops.Add(new GradientStop(Color.FromArgb(220, baseColor.R, baseColor.G, baseColor.B), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(80, baseColor.R, baseColor.G, baseColor.B), 0.45));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B), 1));

            p.Fill = brush;
        }

        public void UpdateBrightness(double targetValue)
        {
            targetValue = Math.Clamp(targetValue, 0, 1);

            double speed = targetValue > _currentValue
                ? Settings.Attack
                : Settings.Release;

            _currentValue += (targetValue - _currentValue) * speed;
            _currentValue = Math.Clamp(_currentValue, 0, 1);

            BeamShape.Opacity = _currentValue * 0.7;
        }

        public void UpdateVisual()
        {
            ApplyBrush(BeamShape);
        }
    }
}