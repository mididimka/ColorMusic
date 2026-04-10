using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ColorMusic.Audio;
using ColorMusic.Model;
using ColorMusic.Rendering;
using ColorMusic.Storage;

using Xceed.Wpf.Toolkit;
using WpfWindowState = System.Windows.WindowState;

namespace ColorMusic
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;

        private AudioCapture? _audio;
        private FftAnalyzer? _fft;

        private readonly List<SpotlightControl> _spotlights = new();
        private readonly List<DragController> _dragControllers = new();

        private readonly SmokeRenderer _smoke = new SmokeRenderer();
        private SpectrumWindow? _spectrumWindow;

        private bool _uiReady;

        private bool _fullscreen;
        private WpfWindowState _oldState;
        private WindowStyle _oldStyle;
        private ResizeMode _oldResize;

        private double _autoLevelGain = 1.0;

        private double _bassEnvelope;
        private double _bassAverage = 0.02;
        private double _fullAverage = 0.02;

        private bool _strobeTrigger;
        private bool _beatTrigger;

        private DateTime _lastStrobeTime = DateTime.MinValue;
        private DateTime _lastBeatTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();

            _settings = SettingsStorage.LoadOrDefault();

            MasterGainSlider.Value = Math.Clamp(_settings.MasterGain, 0, 3);
            GlobalSensitivitySlider.Value = Math.Clamp(_settings.GlobalSensitivity, 0.1, 20);
            AutoLevelTargetSlider.Value = Math.Clamp(_settings.AutoLevelTarget, 0.05, 1.0);
            BaseOpacitySlider.Value = Math.Clamp(_settings.BaseOpacity, 0, 1.0);

            PulseStrengthSlider.Value = Math.Clamp(_settings.PulseStrength, 0, 3);
            StrobeStrengthSlider.Value = Math.Clamp(_settings.StrobeStrength, 0, 3);
            BeatStrengthSlider.Value = Math.Clamp(_settings.BeatFlashStrength, 0, 3);
            NeonStrengthSlider.Value = Math.Clamp(_settings.NeonStrength, 0, 3);

            SmokeCheckBox.IsChecked = _settings.SmokeEnabled;
            AutoLevelCheckBox.IsChecked = _settings.AutoLevelEnabled;
            PulseCheckBox.IsChecked = _settings.PulseEnabled;
            StrobeCheckBox.IsChecked = _settings.StrobeEnabled;
            NeonCheckBox.IsChecked = _settings.NeonGlowEnabled;
            BeatFlashCheckBox.IsChecked = _settings.BeatFlashEnabled;

            ModeComboBox.ItemsSource = new[]
            {
                "Мягкий",
                "Клубный"
            };

            ModeComboBox.SelectedIndex =
                _settings.Mode == ReactionMode.HardClub ? 1 : 0;

            _uiReady = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_settings.SmokeEnabled)
                _smoke.Start(SmokeCanvas, 14);
            else
                SmokeCanvas.Children.Clear();

            StarfieldRenderer.Render(BackgroundCanvas, 450);

            BackgroundCanvas.SizeChanged += (s, ev) =>
            {
                StarfieldRenderer.Render(BackgroundCanvas, 450);
            };

            BuildLights();
            BuildChannelTabs();

            _audio = new AudioCapture();
            _audio.Start();

            _fft = new FftAnalyzer(_audio.SampleRate);
            _audio.OnSamples += samples => _fft.AddSamples(samples);

            _fft.OnSpectrumReady += spectrum =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateLights(spectrum);
                });
            };

            if (_settings.ShowSpectrumWindow)
            {
                _spectrumWindow = new SpectrumWindow
                {
                    Owner = this,
                    Topmost = true
                };
                _spectrumWindow.Show();
            }

            SettingsPanel.Visibility = _settings.ShowSettingsPanel
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BuildLights()
        {
            LightsCanvas.Children.Clear();
            _spotlights.Clear();
            _dragControllers.Clear();

            foreach (var ls in _settings.Lights)
            {
                var sp = new SpotlightControl(ls);
                _spotlights.Add(sp);

                LightsCanvas.Children.Add(sp.ShapeElement);
                Canvas.SetLeft(sp.ShapeElement, ls.X);
                Canvas.SetTop(sp.ShapeElement, ls.Y);

                _dragControllers.Add(new DragController(sp.ShapeElement, ls));
            }
        }

        private void BuildChannelTabs()
        {
            ChannelTabs.Items.Clear();

            for (int i = 0; i < _settings.Lights.Count; i++)
            {
                var light = _settings.Lights[i];
                var channelColor = (Color)ColorConverter.ConvertFromString(light.ColorHex);

                var tabHeader = new TextBlock
                {
                    Text = $"Канал {i + 1}",
                    Foreground = new SolidColorBrush(channelColor),
                    FontWeight = FontWeights.Bold
                };

                var tab = new TabItem
                {
                    Header = tabHeader,
                    Background = new SolidColorBrush(Color.FromRgb(22, 32, 51))
                };

                var panel = new StackPanel
                {
                    Margin = new Thickness(10),
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42))
                };

                panel.Children.Add(new TextBlock
                {
                    Text = light.Name,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                var freqRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };

                freqRow.Children.Add(new TextBlock
                {
                    Text = "Гц",
                    Foreground = Brushes.White,
                    Width = 30,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = "Нижняя и верхняя границы частотного диапазона канала."
                });

                var minBox = new DoubleUpDown
                {
                    Width = 120,
                    Minimum = 10,
                    Maximum = 20000,
                    Value = light.MinFreq,
                    Increment = 10,
                    FormatString = "0",
                    ToolTip = "Нижняя граница диапазона."
                };

                var maxBox = new DoubleUpDown
                {
                    Width = 120,
                    Minimum = 10,
                    Maximum = 22000,
                    Value = light.MaxFreq,
                    Increment = 50,
                    FormatString = "0",
                    Margin = new Thickness(10, 0, 0, 0),
                    ToolTip = "Верхняя граница диапазона."
                };

                minBox.ValueChanged += (s, e) =>
                {
                    if (minBox.Value.HasValue)
                    {
                        light.MinFreq = minBox.Value.Value;
                        if (light.MinFreq > light.MaxFreq - 10)
                            light.MinFreq = light.MaxFreq - 10;
                        minBox.Value = light.MinFreq;
                    }
                };

                maxBox.ValueChanged += (s, e) =>
                {
                    if (maxBox.Value.HasValue)
                    {
                        light.MaxFreq = maxBox.Value.Value;
                        if (light.MaxFreq < light.MinFreq + 10)
                            light.MaxFreq = light.MinFreq + 10;
                        maxBox.Value = light.MaxFreq;
                    }
                };

                freqRow.Children.Add(minBox);
                freqRow.Children.Add(new TextBlock
                {
                    Text = " - ",
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 6, 0)
                });
                freqRow.Children.Add(maxBox);

                panel.Children.Add(freqRow);

                panel.Children.Add(new TextBlock
                {
                    Text = "Чув. кнл",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 4),
                    ToolTip = "Чувствительность канала."
                });

                var sensSlider = new Slider
                {
                    Minimum = 0.1,
                    Maximum = 10,
                    Value = light.Sensitivity,
                    ToolTip = "Чем выше, тем сильнее этот канал реагирует на свой диапазон."
                };

                sensSlider.ValueChanged += (s, e) =>
                {
                    light.Sensitivity = sensSlider.Value;
                };

                panel.Children.Add(sensSlider);

                panel.Children.Add(new TextBlock
                {
                    Text = "Усил.",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 8, 0, 4),
                    ToolTip = "Дополнительное усиление канала."
                });

                var gainSlider = new Slider
                {
                    Minimum = 0,
                    Maximum = 15,
                    Value = light.Gain,
                    ToolTip = "Финальное усиление канала."
                };

                gainSlider.ValueChanged += (s, e) =>
                {
                    light.Gain = gainSlider.Value;
                };

                panel.Children.Add(gainSlider);

                panel.Children.Add(new TextBlock
                {
                    Text = "Разм.",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 8, 0, 4),
                    ToolTip = "Размер прожектора."
                });

                var sizeSlider = new Slider
                {
                    Minimum = 60,
                    Maximum = 320,
                    Value = light.Size,
                    ToolTip = "Размер фигуры прожектора."
                };

                sizeSlider.ValueChanged += (s, e) =>
                {
                    light.Size = sizeSlider.Value;
                    FindSpotlight(light)?.UpdateVisual();
                };

                panel.Children.Add(sizeSlider);

                panel.Children.Add(new TextBlock
                {
                    Text = "Цвет",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 8, 0, 4),
                    ToolTip = "Цвет канала."
                });

                var cp = new ColorPicker
                {
                    Height = 26,
                    SelectedColor = channelColor,
                    ToolTip = "Выбор цвета канала."
                };

                cp.SelectedColorChanged += (s, e) =>
                {
                    if (cp.SelectedColor.HasValue)
                    {
                        var c = cp.SelectedColor.Value;
                        light.ColorHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                        FindSpotlight(light)?.UpdateVisual();

                        if (tab.Header is TextBlock tb)
                            tb.Foreground = new SolidColorBrush(c);
                    }
                };

                panel.Children.Add(cp);

                panel.Children.Add(new TextBlock
                {
                    Text = "Форм.",
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 8, 0, 4),
                    ToolTip = "Форма прожектора."
                });

                var shapeCombo = new ComboBox
                {
                    Height = 26,
                    ToolTip = "Выбор формы прожектора."
                };

                shapeCombo.Items.Add("Круг");
                shapeCombo.Items.Add("Квадрат");
                shapeCombo.Items.Add("Треугольник");

                shapeCombo.SelectedIndex = light.Shape switch
                {
                    LightShape.Circle => 0,
                    LightShape.Square => 1,
                    LightShape.Triangle => 2,
                    _ => 0
                };

                shapeCombo.SelectionChanged += (s, e) =>
                {
                    switch (shapeCombo.SelectedIndex)
                    {
                        case 0:
                            light.Shape = LightShape.Circle;
                            break;
                        case 1:
                            light.Shape = LightShape.Square;
                            break;
                        case 2:
                            light.Shape = LightShape.Triangle;
                            break;
                    }

                    RebuildLight(light);
                };

                panel.Children.Add(shapeCombo);

                tab.Content = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(51, 76, 106)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8),
                    Child = panel
                };

                ChannelTabs.Items.Add(tab);
            }
        }

        private SpotlightControl? FindSpotlight(LightSettings settings)
        {
            foreach (var sp in _spotlights)
                if (sp.Settings == settings)
                    return sp;

            return null;
        }

        private void RebuildLight(LightSettings settings)
        {
            int index = _settings.Lights.IndexOf(settings);
            if (index < 0) return;

            LightsCanvas.Children.Remove(_spotlights[index].ShapeElement);

            var sp = new SpotlightControl(settings);
            _spotlights[index] = sp;

            LightsCanvas.Children.Add(sp.ShapeElement);

            Canvas.SetLeft(sp.ShapeElement, settings.X);
            Canvas.SetTop(sp.ShapeElement, settings.Y);

            _dragControllers[index] = new DragController(sp.ShapeElement, settings);
        }

        private void UpdateBeatAndStrobe(double[] spectrum)
        {
            if (_fft == null)
                return;

            double bass = _fft.GetBandAmplitude(spectrum, 25, 180);
            double mid = _fft.GetBandAmplitude(spectrum, 180, 1400);
            double high = _fft.GetBandAmplitude(spectrum, 1400, 9000);

            double full = (bass * 2.2 + mid * 1.1 + high * 0.6) / 3.9;

            _bassEnvelope += (bass - _bassEnvelope) * 0.48;
            _bassAverage += (_bassEnvelope - _bassAverage) * 0.045;
            _fullAverage += (full - _fullAverage) * 0.06;

            _strobeTrigger = false;
            _beatTrigger = false;

            var now = DateTime.UtcNow;

            bool beatNow =
                _bassEnvelope > _bassAverage * 1.38 &&
                _bassEnvelope > 0.004 &&
                (now - _lastBeatTime).TotalMilliseconds > 130;

            if (beatNow)
            {
                _beatTrigger = true;
                _lastBeatTime = now;
            }

            bool strobeNow =
                full > _fullAverage * 1.62 &&
                full > 0.007 &&
                (now - _lastStrobeTime).TotalMilliseconds > 85;

            if (strobeNow)
            {
                _strobeTrigger = true;
                _lastStrobeTime = now;
            }
        }

        private void UpdateLights(double[] spectrum)
        {
            if (_fft == null) return;

            UpdateBeatAndStrobe(spectrum);

            List<(double value, string colorHex)> bandValues = new();

            double[] preAuto = new double[_spotlights.Count];
            double preAutoAverage = 0;

            for (int i = 0; i < _spotlights.Count; i++)
            {
                var s = _spotlights[i].Settings;

                double amp = _fft.GetBandAmplitude(spectrum, s.MinFreq, s.MaxFreq);

                double val = Math.Log10(1 + amp * 34.0);
                val = Math.Pow(val, 1.28);

                val *= _settings.GlobalSensitivity;
                val *= s.Sensitivity;
                val *= s.Gain;

                if (_settings.Mode == ReactionMode.HardClub)
                    val *= 2.4;

                preAuto[i] = val;
                preAutoAverage += val;
            }

            preAutoAverage /= Math.Max(1, _spotlights.Count);

            if (_settings.AutoLevelEnabled)
            {
                double target = Math.Max(0.05, _settings.AutoLevelTarget);
                double desired = target / Math.Max(0.01, preAutoAverage);
                desired = Math.Clamp(desired, 0.05, 14.0);

                _autoLevelGain += (desired - _autoLevelGain) * 0.18;
            }
            else
            {
                _autoLevelGain += (1.0 - _autoLevelGain) * 0.18;
            }

            _autoLevelGain = Math.Clamp(_autoLevelGain, 0.05, 14.0);

            for (int i = 0; i < _spotlights.Count; i++)
            {
                double val = preAuto[i];
                val *= _autoLevelGain;
                val *= _settings.MasterGain;

                val = val / 0.72;
                val = Math.Clamp(val, 0, 1);

                _spotlights[i].UpdateBrightness(
                    val,
                    _settings.PulseEnabled,
                    _settings.StrobeEnabled,
                    _settings.NeonGlowEnabled,
                    _settings.BeatFlashEnabled,
                    _strobeTrigger,
                    _beatTrigger,
                    _settings.BaseOpacity,
                    _settings.PulseStrength,
                    _settings.StrobeStrength,
                    _settings.NeonStrength,
                    _settings.BeatFlashStrength);

                bandValues.Add((val, _spotlights[i].Settings.ColorHex));
            }

            if (_spectrumWindow != null && _spectrumWindow.IsVisible)
            {
                _spectrumWindow.Topmost = true;
                _spectrumWindow.DrawBands(bandValues);
            }
        }

        private void MasterGainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.MasterGain = MasterGainSlider.Value;
        }

        private void GlobalSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.GlobalSensitivity = GlobalSensitivitySlider.Value;
        }

        private void AutoLevelTargetSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.AutoLevelTarget = AutoLevelTargetSlider.Value;
        }

        private void BaseOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.BaseOpacity = BaseOpacitySlider.Value;
        }

        private void PulseStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.PulseStrength = PulseStrengthSlider.Value;
        }

        private void StrobeStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.StrobeStrength = StrobeStrengthSlider.Value;
        }

        private void BeatStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.BeatFlashStrength = BeatStrengthSlider.Value;
        }

        private void NeonStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_uiReady) return;
            _settings.NeonStrength = NeonStrengthSlider.Value;
        }

        private void SmokeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;

            _settings.SmokeEnabled = SmokeCheckBox.IsChecked == true;

            if (_settings.SmokeEnabled)
                _smoke.Start(SmokeCanvas, 14);
            else
                _smoke.Stop();
        }

        private void AutoLevelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            _settings.AutoLevelEnabled = AutoLevelCheckBox.IsChecked == true;
        }

        private void PulseCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            _settings.PulseEnabled = PulseCheckBox.IsChecked == true;
        }

        private void StrobeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            _settings.StrobeEnabled = StrobeCheckBox.IsChecked == true;
        }

        private void NeonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            _settings.NeonGlowEnabled = NeonCheckBox.IsChecked == true;
        }

        private void BeatFlashCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_uiReady) return;
            _settings.BeatFlashEnabled = BeatFlashCheckBox.IsChecked == true;
        }

        private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_uiReady) return;

            _settings.Mode = ModeComboBox.SelectedIndex == 1
                ? ReactionMode.HardClub
                : ReactionMode.Soft;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                _settings.ShowSettingsPanel = !_settings.ShowSettingsPanel;
                SettingsPanel.Visibility = _settings.ShowSettingsPanel
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            if (e.Key == Key.F2)
            {
                _settings.ShowSpectrumWindow = !_settings.ShowSpectrumWindow;

                if (_settings.ShowSpectrumWindow)
                {
                    if (_spectrumWindow == null || !_spectrumWindow.IsLoaded)
                    {
                        _spectrumWindow = new SpectrumWindow
                        {
                            Owner = this,
                            Topmost = true
                        };
                    }

                    _spectrumWindow.Topmost = true;
                    _spectrumWindow.Show();
                    _spectrumWindow.Activate();
                }
                else
                {
                    _spectrumWindow?.Hide();
                }
            }

            if (e.Key == Key.F11)
                ToggleFullscreen();
        }

        private void ToggleFullscreen()
        {
            if (!_fullscreen)
            {
                _oldState = WindowState;
                _oldStyle = WindowStyle;
                _oldResize = ResizeMode;

                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WpfWindowState.Maximized;

                _fullscreen = true;
            }
            else
            {
                WindowStyle = _oldStyle;
                ResizeMode = _oldResize;
                WindowState = _oldState;

                _fullscreen = false;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (_spectrumWindow != null && _spectrumWindow.IsVisible)
                _spectrumWindow.Topmost = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                SettingsStorage.Save(_settings);
            }
            catch { }

            try
            {
                _audio?.Dispose();
            }
            catch { }

            try
            {
                _smoke.Stop();
            }
            catch { }

            try
            {
                _spectrumWindow?.Close();
            }
            catch { }

            Application.Current.Shutdown();
        }
    }
}