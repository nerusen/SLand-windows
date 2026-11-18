using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Tabs
{
    public class SettingsTabView : UserControl
    {
        private readonly Config _config;
        private readonly Action<string, int> _onPositionChanged;
        private readonly Action<string> _onIdleDisplayChanged;
        private readonly Action<bool> _onToggleOpaque;
        private readonly Action<int> _onFpsChanged;
        private readonly Action _onExitApp;

        private string _curAlign = "Center";
        private int _curOffset = 0;
        private string _curIdleDisplay = "Mascot";
        private bool _curOpaque = false;
        private int _curFps = 30;

        private readonly Slider _offsetSlider;
        private readonly TextBlock _offsetValueText;
        private readonly Button _btnAlignLeft;
        private readonly Button _btnAlignCenter;
        private readonly Button _btnAlignRight;
        private readonly Button _btnIdleMascot;
        private readonly Button _btnIdleClock;
        private readonly Button _btnIdleBoth;
        private readonly Button _btnRenderAcrylic;
        private readonly Button _btnRenderOpaque;
        private readonly Button _btnFps15;
        private readonly Button _btnFps30;
        private readonly TextBlock _saveFeedback;

        public SettingsTabView(
            Config config,
            Action<string, int> onPositionChanged,
            Action<string> onIdleDisplayChanged,
            Action<bool> onToggleOpaque,
            Action<int> onFpsChanged,
            Action onExitApp)
        {
            _config = config;
            _onPositionChanged = onPositionChanged;
            _onIdleDisplayChanged = onIdleDisplayChanged;
            _onToggleOpaque = onToggleOpaque;
            _onFpsChanged = onFpsChanged;
            _onExitApp = onExitApp;

            // Load saved settings
            _curAlign = _config.Get("PositionAlign", "Center");
            _curOffset = _config.GetInt("PositionOffset", 0);
            _curIdleDisplay = _config.Get("IdleDisplay", "Mascot");
            _curOpaque = _config.GetBool("SolidOpaque", Logger.IsOpaque);
            _curFps = _config.GetInt("FpsLimit", 30);

            var root = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Margin = new Thickness(4, 2, 4, 2)
            };

            var mainStack = new StackPanel { Spacing = 10 };

            // 1. POSISI ISLAND
            var posCard = CreateSectionCard("POSISI HORIZONTAL DYNAMIC ISLAND", out var posStack);

            var alignGrid = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 6) };
            _btnAlignLeft = CreateSelectBtn("Kiri", () => SetAlign("Left"));
            _btnAlignCenter = CreateSelectBtn("Tengah", () => SetAlign("Center"));
            _btnAlignRight = CreateSelectBtn("Kanan", () => SetAlign("Right"));
            alignGrid.Children.Add(_btnAlignLeft);
            alignGrid.Children.Add(_btnAlignCenter);
            alignGrid.Children.Add(_btnAlignRight);
            posStack.Children.Add(alignGrid);

            var offsetGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };

            _offsetValueText = new TextBlock
            {
                Text = $"{_curOffset:+0;-0;0} px",
                FontSize = 11,
                Foreground = TepiTheme.BrushAccent,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_offsetValueText, 1);

            _offsetSlider = new Slider
            {
                Minimum = -150,
                Maximum = 150,
                Value = _curOffset,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _offsetSlider.ValueChanged += (s, e) =>
            {
                _curOffset = (int)e.NewValue;
                if (_offsetValueText != null)
                {
                    _offsetValueText.Text = $"{_curOffset:+0;-0;0} px";
                }
                _config.Set("PositionOffset", _curOffset.ToString());
                _onPositionChanged(_curAlign, _curOffset);
            };
            Grid.SetColumn(_offsetSlider, 0);

            offsetGrid.Children.Add(_offsetSlider);
            offsetGrid.Children.Add(_offsetValueText);
            posStack.Children.Add(offsetGrid);
            mainStack.Children.Add(posCard);

            // 2. TAMPILAN KAPSUL IDLE
            var idleCard = CreateSectionCard("TAMPILAN KAPSUL SAAT IDLE", out var idleStack);

            var idleGrid = new UniformGrid { Columns = 3 };
            _btnIdleMascot = CreateSelectBtn("Mata Vektor", () => SetIdleDisplay("Mascot"));
            _btnIdleClock = CreateSelectBtn("Jam Digital", () => SetIdleDisplay("Clock"));
            _btnIdleBoth = CreateSelectBtn("Keduanya", () => SetIdleDisplay("Both"));
            idleGrid.Children.Add(_btnIdleMascot);
            idleGrid.Children.Add(_btnIdleClock);
            idleGrid.Children.Add(_btnIdleBoth);
            idleStack.Children.Add(idleGrid);
            mainStack.Children.Add(idleCard);

            // 3. MODE RENDERING
            var renderCard = CreateSectionCard("MODE RENDERING GRAFIS", out var renderStack);

            var renderGrid = new UniformGrid { Columns = 2 };
            _btnRenderAcrylic = CreateSelectBtn("Dark Acrylic (Glass)", () => SetRendering(false));
            _btnRenderOpaque = CreateSelectBtn("Solid Opaque (VNC)", () => SetRendering(true));
            renderGrid.Children.Add(_btnRenderAcrylic);
            renderGrid.Children.Add(_btnRenderOpaque);
            renderStack.Children.Add(renderGrid);

            var renderNote = new TextBlock
            {
                Text = "Gunakan 'Solid Opaque' jika VNC menampilkan kotak hitam di sekitar sudut island.",
                FontSize = 9.5,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(0, 4, 0, 0)
            };
            renderStack.Children.Add(renderNote);
            mainStack.Children.Add(renderCard);

            // 4. BATAS FPS ANIMASI
            var fpsCard = CreateSectionCard("BATAS FPS ANIMASI (PEGAS & EQUALIZER)", out var fpsStack);

            var fpsGrid = new UniformGrid { Columns = 2 };
            _btnFps15 = CreateSelectBtn("15 FPS (Hemat Daya)", () => SetFps(15));
            _btnFps30 = CreateSelectBtn("30 FPS (Standar Halus)", () => SetFps(30));
            fpsGrid.Children.Add(_btnFps15);
            fpsGrid.Children.Add(_btnFps30);
            fpsStack.Children.Add(fpsGrid);
            mainStack.Children.Add(fpsCard);

            // 5. BOTTOM ACTIONS (Reload & Exit)
            var actionGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,*"),
                Margin = new Thickness(2, 4, 2, 0)
            };

            var reloadBtn = new Button
            {
                Content = "🔄 Muat Ulang Konfig",
                FontSize = 11,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 0, 4, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            reloadBtn.Click += (s, e) =>
            {
                _config.Load();
                SyncUiWithConfig();
                ShowSaveFeedback("Konfigurasi dimuat ulang");
            };
            Grid.SetColumn(reloadBtn, 0);

            var exitBtn = new Button
            {
                Content = "✕ Keluar Aplikasi",
                FontSize = 11,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(4, 0, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(40, 240, 120, 120)),
                Foreground = TepiTheme.BrushNegative,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            exitBtn.Click += (s, e) => _onExitApp();
            Grid.SetColumn(exitBtn, 1);

            actionGrid.Children.Add(reloadBtn);
            actionGrid.Children.Add(exitBtn);
            mainStack.Children.Add(actionGrid);

            _saveFeedback = new TextBlock
            {
                Text = "✓ Pengaturan otomatis tersimpan di ~/.config/tepi/settings.ini",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 4)
            };
            mainStack.Children.Add(_saveFeedback);

            root.Content = mainStack;
            Content = root;

            UpdateVisualButtons();
        }

        private Border CreateSectionCard(string title, out StackPanel stack)
        {
            var card = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 8)
            };

            stack = new StackPanel { Spacing = 6 };
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint
            });

            card.Child = stack;
            return card;
        }

        private Button CreateSelectBtn(string text, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
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
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetAlign(string align)
        {
            _curAlign = align;
            _config.Set("PositionAlign", align);
            UpdateVisualButtons();
            _onPositionChanged(_curAlign, _curOffset);
            ShowSaveFeedback($"Posisi diubah ke {align}");
        }

        private void SetIdleDisplay(string mode)
        {
            _curIdleDisplay = mode;
            _config.Set("IdleDisplay", mode);
            UpdateVisualButtons();
            _onIdleDisplayChanged(mode);
            ShowSaveFeedback($"Tampilan Idle: {mode}");
        }

        private void SetRendering(bool opaque)
        {
            _curOpaque = opaque;
            Logger.IsOpaque = opaque;
            _config.SetBool("SolidOpaque", opaque);
            UpdateVisualButtons();
            _onToggleOpaque(opaque);
            ShowSaveFeedback(opaque ? "Mode Solid Opaque aktif" : "Mode Dark Acrylic aktif");
        }

        private void SetFps(int fps)
        {
            _curFps = fps;
            _config.Set("FpsLimit", fps.ToString());
            UpdateVisualButtons();
            _onFpsChanged(fps);
            ShowSaveFeedback($"Batas animasi diubah ke {fps} FPS");
        }

        private void SyncUiWithConfig()
        {
            _curAlign = _config.Get("PositionAlign", "Center");
            _curOffset = _config.GetInt("PositionOffset", 0);
            _curIdleDisplay = _config.Get("IdleDisplay", "Mascot");
            _curOpaque = _config.GetBool("SolidOpaque", Logger.IsOpaque);
            _curFps = _config.GetInt("FpsLimit", 30);

            _offsetSlider.Value = _curOffset;
            _offsetValueText.Text = $"{_curOffset:+0;-0;0} px";

            UpdateVisualButtons();
            _onPositionChanged(_curAlign, _curOffset);
            _onIdleDisplayChanged(_curIdleDisplay);
            _onToggleOpaque(_curOpaque);
            _onFpsChanged(_curFps);
        }

        private void UpdateVisualButtons()
        {
            HighlightBtn(_btnAlignLeft, _curAlign == "Left");
            HighlightBtn(_btnAlignCenter, _curAlign == "Center");
            HighlightBtn(_btnAlignRight, _curAlign == "Right");

            HighlightBtn(_btnIdleMascot, _curIdleDisplay == "Mascot");
            HighlightBtn(_btnIdleClock, _curIdleDisplay == "Clock");
            HighlightBtn(_btnIdleBoth, _curIdleDisplay == "Both");

            HighlightBtn(_btnRenderAcrylic, !_curOpaque);
            HighlightBtn(_btnRenderOpaque, _curOpaque);

            HighlightBtn(_btnFps15, _curFps == 15);
            HighlightBtn(_btnFps30, _curFps == 30);
        }

        private void HighlightBtn(Button btn, bool active)
        {
            btn.Background = active ? TepiTheme.BrushAccent : TepiTheme.BrushFill;
            btn.Foreground = active ? Brushes.White : TepiTheme.BrushText;
            btn.FontWeight = active ? Avalonia.Media.FontWeight.SemiBold : Avalonia.Media.FontWeight.Normal;
        }

        private void ShowSaveFeedback(string msg)
        {
            _saveFeedback.Text = $"✓ {msg}";
            _saveFeedback.Foreground = TepiTheme.BrushPositive;
        }
    }
}
