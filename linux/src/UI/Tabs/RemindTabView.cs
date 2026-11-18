using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class RemindTabView : UserControl
    {
        private readonly PrayerService _prayerService;
        private readonly Action<string?>? _onFocusTick;

        // Pomodoro State
        private readonly DispatcherTimer _pomodoroTimer;
        private int _totalSeconds = 25 * 60;
        private int _remainingSeconds = 25 * 60;
        private bool _isRunning = false;

        private readonly TextBlock _timerDisplay;
        private readonly Button _startPauseBtn;
        private readonly TextBlock _timerStatusLabel;
        private readonly StackPanel _prayerListContainer;

        public RemindTabView(PrayerService prayerService, Action<string?>? onFocusTick = null)
        {
            _prayerService = prayerService;
            _onFocusTick = onFocusTick;

            _pomodoroTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pomodoroTimer.Tick += OnPomodoroTick;

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

            // 1. FOCUS / POMODORO TIMER CARD
            var timerCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var timerStack = new StackPanel { Spacing = 8 };

            var timerHeader = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            var timerTitle = new TextBlock
            {
                Text = "TIMER FOKUS & BELAJAR (POMODORO)",
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timerTitle, 0);

            _timerStatusLabel = new TextBlock
            {
                Text = "Siap Mulai",
                FontSize = 10,
                Foreground = TepiTheme.BrushAccent,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_timerStatusLabel, 1);

            timerHeader.Children.Add(timerTitle);
            timerHeader.Children.Add(_timerStatusLabel);
            timerStack.Children.Add(timerHeader);

            // Large Digital Countdown Display
            _timerDisplay = new TextBlock
            {
                Text = "25:00",
                FontSize = 32,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = TepiTheme.BrushText,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
            timerStack.Children.Add(_timerDisplay);

            // Preset Buttons: 15m, 25m, 45m, 60m
            var presetRow = new UniformGrid
            {
                Columns = 4,
                Margin = new Thickness(0, 0, 0, 6)
            };

            presetRow.Children.Add(CreatePresetBtn("15 m", 15));
            presetRow.Children.Add(CreatePresetBtn("25 m", 25));
            presetRow.Children.Add(CreatePresetBtn("45 m", 45));
            presetRow.Children.Add(CreatePresetBtn("60 m", 60));
            timerStack.Children.Add(presetRow);

            // Controls Row (Start/Pause & Reset)
            var controlRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*")
            };

            _startPauseBtn = new Button
            {
                Content = "▶ Mulai Fokus",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Padding = new Thickness(12, 6),
                Margin = new Thickness(0, 0, 4, 0),
                Background = TepiTheme.BrushAccent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            _startPauseBtn.Click += (s, e) => ToggleTimer();
            Grid.SetColumn(_startPauseBtn, 0);

            var resetBtn = new Button
            {
                Content = "↺ Reset",
                FontSize = 11.5,
                Padding = new Thickness(12, 6),
                Margin = new Thickness(4, 0, 0, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            resetBtn.Click += (s, e) => ResetTimer();
            Grid.SetColumn(resetBtn, 1);

            controlRow.Children.Add(_startPauseBtn);
            controlRow.Children.Add(resetBtn);
            timerStack.Children.Add(controlRow);

            timerCard.Child = timerStack;
            mainStack.Children.Add(timerCard);

            // 2. PRAYER SCHEDULE CARD (5 Waktu Sholat)
            var prayerCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var prayerStack = new StackPanel { Spacing = 6 };

            var prayerHeader = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var pIcon = new TextBlock
            {
                Text = "🕌",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(pIcon, 0);

            var pTitle = new TextBlock
            {
                Text = "Jadwal 5 Waktu Sholat (Offline)",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pTitle, 1);

            var nextInfo = _prayerService.GetNextPrayer();
            var pBadge = new TextBlock
            {
                Text = nextInfo.Formatted,
                FontSize = 10,
                Foreground = TepiTheme.BrushWarning,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pBadge, 2);

            prayerHeader.Children.Add(pIcon);
            prayerHeader.Children.Add(pTitle);
            prayerHeader.Children.Add(pBadge);
            prayerStack.Children.Add(prayerHeader);

            _prayerListContainer = new StackPanel
            {
                Spacing = 4,
                Margin = new Thickness(0, 4, 0, 0)
            };
            prayerStack.Children.Add(_prayerListContainer);

            prayerCard.Child = prayerStack;
            mainStack.Children.Add(prayerCard);

            root.Content = mainStack;
            Content = root;

            RenderPrayerSchedule();
        }

        private Button CreatePresetBtn(string label, int minutes)
        {
            var btn = new Button
            {
                Content = label,
                FontSize = 10.5,
                Padding = new Thickness(6, 4),
                Margin = new Thickness(2),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            btn.Click += (s, e) =>
            {
                if (!_isRunning)
                {
                    _totalSeconds = minutes * 60;
                    _remainingSeconds = _totalSeconds;
                    UpdateTimerDisplay();
                }
            };
            return btn;
        }

        private void ToggleTimer()
        {
            _isRunning = !_isRunning;
            if (_isRunning)
            {
                _pomodoroTimer.Start();
                _startPauseBtn.Content = "⏸ Jeda";
                _startPauseBtn.Background = TepiTheme.BrushFill;
                _startPauseBtn.Foreground = TepiTheme.BrushWarning;
                _timerStatusLabel.Text = "Sesi Berjalan...";
                _timerStatusLabel.Foreground = TepiTheme.BrushPositive;
                _onFocusTick?.Invoke(FormatTime(_remainingSeconds));
            }
            else
            {
                _pomodoroTimer.Stop(); // Ensure 0 FPS at pause
                _startPauseBtn.Content = "▶ Lanjutkan";
                _startPauseBtn.Background = TepiTheme.BrushAccent;
                _startPauseBtn.Foreground = Brushes.White;
                _timerStatusLabel.Text = "Dijeda";
                _timerStatusLabel.Foreground = TepiTheme.BrushWarning;
            }
        }

        private void ResetTimer()
        {
            _pomodoroTimer.Stop(); // Ensure 0 FPS at rest
            _isRunning = false;
            _remainingSeconds = _totalSeconds;
            _startPauseBtn.Content = "▶ Mulai Fokus";
            _startPauseBtn.Background = TepiTheme.BrushAccent;
            _startPauseBtn.Foreground = Brushes.White;
            _timerStatusLabel.Text = "Siap Mulai";
            _timerStatusLabel.Foreground = TepiTheme.BrushAccent;
            UpdateTimerDisplay();
            _onFocusTick?.Invoke(null);
        }

        private void OnPomodoroTick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerDisplay();
                _onFocusTick?.Invoke(FormatTime(_remainingSeconds));
            }
            else
            {
                _pomodoroTimer.Stop(); // Guarantee 0 FPS when done
                _isRunning = false;
                _startPauseBtn.Content = "✓ Selesai!";
                _startPauseBtn.Background = TepiTheme.BrushPositive;
                _timerStatusLabel.Text = "Waktu Fokus Habis!";
                _timerStatusLabel.Foreground = TepiTheme.BrushPositive;
                _onFocusTick?.Invoke("Selesai");
            }
        }

        private void UpdateTimerDisplay()
        {
            _timerDisplay.Text = FormatTime(_remainingSeconds);
        }

        private static string FormatTime(int sec)
        {
            int m = sec / 60;
            int s = sec % 60;
            return $"{m:00}:{s:00}";
        }

        private void RenderPrayerSchedule()
        {
            _prayerListContainer.Children.Clear();
            var items = _prayerService.GetDailySchedule();

            foreach (var item in items)
            {
                var row = new Border
                {
                    Background = item.IsNext ? new SolidColorBrush(Color.FromArgb(30, 59, 130, 246)) : Brushes.Transparent,
                    BorderBrush = item.IsNext ? TepiTheme.BrushAccent : Brushes.Transparent,
                    BorderThickness = new Thickness(item.IsNext ? 1 : 0),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 5)
                };

                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
                };

                var icon = new TextBlock
                {
                    Text = item.Icon,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(icon, 0);

                var name = new TextBlock
                {
                    Text = item.Name,
                    FontSize = 11,
                    FontWeight = item.IsNext ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal,
                    Foreground = item.IsNext ? Brushes.White : TepiTheme.BrushText,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(name, 1);

                if (item.IsNext)
                {
                    var nextTag = new Border
                    {
                        Background = TepiTheme.BrushAccent,
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 1),
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "BERIKUTNYA",
                            FontSize = 8.5,
                            FontWeight = Avalonia.Media.FontWeight.Bold,
                            Foreground = Brushes.White
                        }
                    };
                    Grid.SetColumn(nextTag, 2);
                    grid.Children.Add(nextTag);
                }

                var timeStr = DateTime.Today.Add(item.Time).ToString("HH:mm");
                var timeBlock = new TextBlock
                {
                    Text = timeStr,
                    FontSize = 11,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    Foreground = item.IsNext ? TepiTheme.BrushAccent : TepiTheme.BrushSub,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(timeBlock, 3);

                grid.Children.Add(icon);
                grid.Children.Add(name);
                grid.Children.Add(timeBlock);

                row.Child = grid;
                _prayerListContainer.Children.Add(row);
            }
        }
    }
}
