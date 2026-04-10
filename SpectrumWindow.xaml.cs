using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ColorMusic
{
    public partial class SpectrumWindow : Window
    {
        public SpectrumWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        public void DrawBands(List<(double value, string colorHex)> bands)
        {
            SpectrumCanvas.Children.Clear();

            double w = ActualWidth;
            double h = ActualHeight;

            if (w <= 0 || h <= 0 || bands.Count == 0)
                return;

            int count = bands.Count;
            double barWidth = w / count;

            for (int i = 0; i < count; i++)
            {
                double val = Math.Clamp(bands[i].value, 0, 1);
                double height = val * (h - 28);

                var color = (Color)ColorConverter.ConvertFromString(bands[i].colorHex);

                var rect = new Rectangle
                {
                    Width = Math.Max(8, barWidth - 12),
                    Height = height,
                    Fill = new SolidColorBrush(color),
                    Opacity = 0.9,
                    RadiusX = 8,
                    RadiusY = 8
                };

                SpectrumCanvas.Children.Add(rect);
                Canvas.SetLeft(rect, i * barWidth + 6);
                Canvas.SetTop(rect, h - height - 16);

                var label = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    Opacity = 0.75
                };

                SpectrumCanvas.Children.Add(label);
                Canvas.SetLeft(label, i * barWidth + 10);
                Canvas.SetTop(label, h - 18);
            }
        }
    }
}