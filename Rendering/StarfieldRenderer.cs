using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ColorMusic.Rendering
{
    public static class StarfieldRenderer
    {
        private static readonly List<Ellipse> Stars = new();
        private static DispatcherTimer? _timer;
        private static readonly Random Rnd = new();

        public static void Render(Canvas canvas, int stars = 400)
        {
            canvas.Children.Clear();
            Stars.Clear();

            for (int i = 0; i < stars; i++)
            {
                double x = Rnd.NextDouble() * canvas.ActualWidth;
                double y = Rnd.NextDouble() * canvas.ActualHeight;

                double size = Rnd.NextDouble() * 2.5 + 0.5;

                var star = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = Brushes.White,
                    Opacity = 0.15 + Rnd.NextDouble() * 0.85
                };

                Canvas.SetLeft(star, x);
                Canvas.SetTop(star, y);

                Stars.Add(star);
                canvas.Children.Add(star);
            }

            StartTwinkle();
        }

        private static void StartTwinkle()
        {
            _timer?.Stop();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };

            _timer.Tick += (s, e) =>
            {
                if (Stars.Count == 0) return;

                for (int i = 0; i < 18; i++)
                {
                    var star = Stars[Rnd.Next(Stars.Count)];
                    star.Opacity = 0.1 + Rnd.NextDouble() * 0.9;
                }
            };

            _timer.Start();
        }
    }
}