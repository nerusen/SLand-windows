using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Tabs
{
    public class ControlTabView : UserControl
    {
        private readonly Action<bool> _onToggleOpaque;
        private readonly Slider _volumeSlider;
        private readonly TextBlock _volumeValueText;
        private readonly TextBlock _batteryPercentText;
        private readonly TextBlock _batteryStatusText;
        private readonly Border _batteryBar;
        private readonly Button _picomToggleBtn;
        private readonly TextBlock _picomStatusText;
        private readonly Button _themeToggleBtn;
        private bool _isMuted = false;
        private bool _isTorchOn = false;

        public ControlTabView(Action<bool> onToggleOpaque)
        {
            _onToggleOpaque = onToggleOpaque;

            var root = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Margin = new Thickness(4, 2, 4, 2)
            };

            var mainStack = new StackPanel
            {
                Spacing = 10
            };

            // 1. MASTER VOLUME CARD (PulseAudio)
            var volumeCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var volStack = new StackPanel { Spacing = 6 };

            var volHeaderGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var volIcon = new TextBlock
            {
                Text = "🔊",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(volIcon, 0);

            var volTitle = new TextBlock
            {
                Text = "Volume Master (PulseAudio)",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(volTitle, 1);

            _volumeValueText = new TextBlock
            {
                Text = "50%",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushAccent,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_volumeValueText, 2);

            volHeaderGrid.Children.Add(volIcon);
            volHeaderGrid.Children.Add(volTitle);
            volHeaderGrid.Children.Add(_volumeValueText);
            volStack.Children.Add(volHeaderGrid);

            var volControlsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(0, 4, 0, 0)
            };

            _volumeSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _volumeSlider.ValueChanged += (s, e) =>
            {
                int val = (int)e.NewValue;
                _volumeValueText.Text = $"{val}%";
                SetPulseVolume(val);
            };
            Grid.SetColumn(_volumeSlider, 0);

            var muteBtn = new Button
            {
                Content = "Mute",
                FontSize = 10.5,
                Padding = new Thickness(10, 4),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            muteBtn.Click += (s, e) =>
            {
                _isMuted = !_isMuted;
                muteBtn.Foreground = _isMuted ? TepiTheme.BrushNegative : TepiTheme.BrushText;
                muteBtn.Content = _isMuted ? "Unmute" : "Mute";
                TogglePulseMute();
            };
            Grid.SetColumn(muteBtn, 1);

            volControlsGrid.Children.Add(_volumeSlider);
            volControlsGrid.Children.Add(muteBtn);
            volStack.Children.Add(volControlsGrid);

            volumeCard.Child = volStack;
            mainStack.Children.Add(volumeCard);

            // 2. STATUS BATERAI CARD (Android Kernel / Termux fallback)
            var batteryCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var batStack = new StackPanel { Spacing = 6 };

            var batHeaderGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var batIcon = new TextBlock
            {
                Text = "🔋",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(batIcon, 0);

            var batTitle = new TextBlock
            {
                Text = "Daya & Baterai Perangkat",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(batTitle, 1);

            _batteryPercentText = new TextBlock
            {
                Text = "--%",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushPositive,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_batteryPercentText, 2);

            batHeaderGrid.Children.Add(batIcon);
            batHeaderGrid.Children.Add(batTitle);
            batHeaderGrid.Children.Add(_batteryPercentText);
            batStack.Children.Add(batHeaderGrid);

            // Battery bar track
            var barTrack = new Border
            {
                Height = 6,
                Background = TepiTheme.BrushFill,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 4, 0, 2)
            };
            _batteryBar = new Border
            {
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = TepiTheme.BrushPositive,
                CornerRadius = new CornerRadius(3)
            };
            barTrack.Child = _batteryBar;
            batStack.Children.Add(barTrack);

            _batteryStatusText = new TextBlock
            {
                Text = "Mendeteksi status daya...",
                FontSize = 10.5,
                Foreground = TepiTheme.BrushFaint
            };
            batStack.Children.Add(_batteryStatusText);

            batteryCard.Child = batStack;
            mainStack.Children.Add(batteryCard);

            // 3. QUICK TOGGLES PROOT CARD
            var toggleCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var toggleStack = new StackPanel { Spacing = 8 };

            var toggleTitle = new TextBlock
            {
                Text = "PENGALIH CEPAT & LINGKUNGAN PROOT",
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint
            };
            toggleStack.Children.Add(toggleTitle);

            var toggleRow1 = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*")
            };

            // Mode Rendering Toggle
            _themeToggleBtn = new Button
            {
                Content = Logger.IsOpaque ? "Mode: Solid Opaque" : "Mode: Dark Acrylic",
                FontSize = 11,
                Padding = new Thickness(8, 6),
                Margin = new Thickness(0, 0, 4, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            _themeToggleBtn.Click += (s, e) =>
            {
                bool nextOpaque = !Logger.IsOpaque;
                Logger.IsOpaque = nextOpaque;
                _themeToggleBtn.Content = nextOpaque ? "Mode: Solid Opaque" : "Mode: Dark Acrylic";
                _onToggleOpaque(nextOpaque);
            };
            Grid.SetColumn(_themeToggleBtn, 0);

            // Picom Toggle
            _picomToggleBtn = new Button
            {
                Content = "Jalankan Picom",
                FontSize = 11,
                Padding = new Thickness(8, 6),
                Margin = new Thickness(4, 0, 0, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushAccent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            _picomToggleBtn.Click += async (s, e) => await TogglePicomAsync();
            Grid.SetColumn(_picomToggleBtn, 1);

            toggleRow1.Children.Add(_themeToggleBtn);
            toggleRow1.Children.Add(_picomToggleBtn);
            toggleStack.Children.Add(toggleRow1);

            _picomStatusText = new TextBlock
            {
                Text = "Compositor X11 memberikan efek transparansi & bayangan halus.",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint
            };
            toggleStack.Children.Add(_picomStatusText);

            // Termux-API Hardware Shortcuts
            var hwRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*,*"),
                Margin = new Thickness(0, 4, 0, 0)
            };

            var torchBtn = new Button
            {
                Content = "🔦 Senter",
                FontSize = 10.5,
                Padding = new Thickness(6, 5),
                Margin = new Thickness(0, 0, 3, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            torchBtn.Click += (s, e) =>
            {
                _isTorchOn = !_isTorchOn;
                RunCommandDetached($"termux-torch {(_isTorchOn ? "on" : "off")}");
                torchBtn.Foreground = _isTorchOn ? TepiTheme.BrushWarning : TepiTheme.BrushText;
            };
            Grid.SetColumn(torchBtn, 0);

            var vibrateBtn = new Button
            {
                Content = "📳 Getar",
                FontSize = 10.5,
                Padding = new Thickness(6, 5),
                Margin = new Thickness(3, 0, 3, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            vibrateBtn.Click += (s, e) => RunCommandDetached("termux-vibrate -d 200");
            Grid.SetColumn(vibrateBtn, 1);

            var toastBtn = new Button
            {
                Content = "🔔 Toast HP",
                FontSize = 10.5,
                Padding = new Thickness(6, 5),
                Margin = new Thickness(3, 0, 0, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            toastBtn.Click += (s, e) => RunCommandDetached("termux-toast 'Tepi Linux ARM64 aktif di Android'");
            Grid.SetColumn(toastBtn, 2);

            hwRow.Children.Add(torchBtn);
            hwRow.Children.Add(vibrateBtn);
            hwRow.Children.Add(toastBtn);
            toggleStack.Children.Add(hwRow);

            toggleCard.Child = toggleStack;
            mainStack.Children.Add(toggleCard);

            root.Content = mainStack;
            Content = root;

            // Load initial state asynchronously
            Task.Run(InitSystemState);
        }

        private async Task InitSystemState()
        {
            await ReadPulseVolumeAsync();
            await ReadBatteryStatusAsync();
            await CheckPicomRunningAsync();
        }

        private async Task ReadPulseVolumeAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pactl",
                    Arguments = "get-sink-volume @DEFAULT_SINK@",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string output = await p.StandardOutput.ReadToEndAsync();
                    await p.WaitForExitAsync();

                    var match = Regex.Match(output, @"(\d+)%");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int vol))
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _volumeSlider.Value = vol;
                            _volumeValueText.Text = $"{vol}%";
                        });
                    }
                }
            }
            catch { }
        }

        private void SetPulseVolume(int percent)
        {
            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "pactl",
                        Arguments = $"set-sink-volume @DEFAULT_SINK@ {percent}%",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(200);
                }
                catch { }
            });
        }

        private void TogglePulseMute()
        {
            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "pactl",
                        Arguments = "set-sink-mute @DEFAULT_SINK@ toggle",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(200);
                }
                catch { }
            });
        }

        private async Task ReadBatteryStatusAsync()
        {
            int capacity = -1;
            string status = "";

            try
            {
                // Method 1: Kernel sysfs
                string capPath = "/sys/class/power_supply/battery/capacity";
                string statPath = "/sys/class/power_supply/battery/status";
                if (File.Exists(capPath))
                {
                    string rawCap = (await File.ReadAllTextAsync(capPath)).Trim();
                    int.TryParse(rawCap, out capacity);
                }
                if (File.Exists(statPath))
                {
                    status = (await File.ReadAllTextAsync(statPath)).Trim();
                }
            }
            catch { }

            // Method 2: termux-battery-status fallback
            if (capacity < 0)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "termux-battery-status",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        string json = await p.StandardOutput.ReadToEndAsync();
                        await p.WaitForExitAsync();
                        var m = Regex.Match(json, @"""percentage"":\s*(\d+)");
                        if (m.Success) int.TryParse(m.Groups[1].Value, out capacity);

                        var ms = Regex.Match(json, @"""status"":\s*""([^""]+)""");
                        if (ms.Success) status = ms.Groups[1].Value;
                    }
                }
                catch { }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (capacity >= 0)
                {
                    _batteryPercentText.Text = $"{capacity}%";
                    _batteryBar.Width = Math.Clamp(capacity * 3.8, 10, 380);
                    _batteryStatusText.Text = string.IsNullOrEmpty(status) ? "Berjalan dengan baterai" : $"Status: {status}";
                    _batteryPercentText.Foreground = capacity < 20 ? TepiTheme.BrushNegative : TepiTheme.BrushPositive;
                }
                else
                {
                    _batteryPercentText.Text = "PRoot";
                    _batteryPercentText.Foreground = TepiTheme.BrushSub;
                    _batteryBar.Width = 60;
                    _batteryStatusText.Text = "Terisolasi PRoot (Akses sysfs Android dibatasi kernel).";
                }
            });
        }

        private async Task CheckPicomRunningAsync()
        {
            bool running = false;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    Arguments = "-x picom",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string outTxt = await p.StandardOutput.ReadToEndAsync();
                    await p.WaitForExitAsync();
                    running = !string.IsNullOrWhiteSpace(outTxt);
                }
            }
            catch { }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _picomToggleBtn.Content = running ? "Hentikan Picom" : "Jalankan Picom";
                _picomToggleBtn.Foreground = running ? TepiTheme.BrushWarning : TepiTheme.BrushAccent;
                _picomStatusText.Text = running ? "Picom aktif (Compositing X11 berjalan)." : "Picom nonaktif (Mode fallback tanpa transparansi).";
            });
        }

        private async Task TogglePicomAsync()
        {
            try
            {
                bool isRunning = _picomToggleBtn.Content?.ToString()?.Contains("Hentikan") == true;
                if (isRunning)
                {
                    RunCommandDetached("pkill -9 picom");
                }
                else
                {
                    RunCommandDetached("picom -b --backend xrender");
                }

                await Task.Delay(400);
                await CheckPicomRunningAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError("TogglePicomAsync", ex);
            }
        }

        private void RunCommandDetached(string cmd)
        {
            Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "sh",
                        Arguments = $"-c \"{cmd}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    p?.WaitForExit(300);
                }
                catch { }
            });
        }
    }
}
