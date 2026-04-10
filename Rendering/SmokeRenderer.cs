using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ColorMusic.Rendering
{
    public class SmokeRenderer
    {
        private class SmokeBlob
        {
            public Ellipse Shape = null!;
            public double X;
            public double Y;
            public double SpeedX;
            public double SpeedY;
            public double Size;
            public double Opacity;
            public double Phase;
        }

        private readonly List<SmokeBlob> _blobs = new();
        private readonly Random _rnd = new();

        private DispatcherTimer? _timer;
        private Canvas? _canvas;

        public bool IsRunning => _timer != null;

        public void Start(Canvas canvas, int blobs = 14)
        {
            Stop();

            _canvas = canvas;
            _canvas.Children.Clear();
            _blobs.Clear();

            for (int i = 0; i < blobs; i++)
            {
                var b = new SmokeBlob();

                b.Size = 500 + _rnd.NextDouble() * 900;
                b.X = _rnd.NextDouble() * canvas.ActualWidth;
                b.Y = _rnd.NextDouble() * canvas.ActualHeight;

                b.SpeedX = -0.14 + _rnd.NextDouble() * 0.28;
                b.SpeedY = -0.06 + _rnd.NextDouble() * 0.12;

                b.Opacity = 0.10 + _rnd.NextDouble() * 0.14;
                b.Phase = _rnd.NextDouble() * 10;

                var brush = new RadialGradientBrush();
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(90, 120, 170, 255), 0));
                brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));

                b.Shape = new Ellipse
                {
                    Width = b.Size,
                    Height = b.Size,
                    Opacity = b.Opacity,
                    Fill = brush,
                    Effect = new System.Windows.Media.Effects.BlurEffect
                    {
                        Radius = 70
                    }
                };

                Canvas.SetLeft(b.Shape, b.X);
                Canvas.SetTop(b.Shape, b.Y);

                _blobs.Add(b);
                canvas.Children.Add(b.Shape);
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };

            _timer.Tick += (s, e) => Update();
            _timer.Start();
        }

        private void Update()
        {
            if (_canvas == null) return;

            double w = _canvas.ActualWidth;
            double h = _canvas.ActualHeight;

            double t = DateTime.Now.TimeOfDay.TotalSeconds;

            foreach (var b in _blobs)
            {
                b.X += b.SpeedX;
                b.Y += b.SpeedY;

                double pulse = 0.05 * Math.Sin(t * 0.7 + b.Phase);
                b.Shape.Opacity = Math.Clamp(b.Opacity + pulse, 0.05, 0.35);

                if (b.X < -b.Size) b.X = w;
                if (b.X > w) b.X = -b.Size;

                if (b.Y < -b.Size) b.Y = h;
                if (b.Y > h) b.Y = -b.Size;

                Canvas.SetLeft(b.Shape, b.X);
                Canvas.SetTop(b.Shape, b.Y);
            }
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            _blobs.Clear();

            if (_canvas != null)
                _canvas.Children.Clear();
        }
    }
}