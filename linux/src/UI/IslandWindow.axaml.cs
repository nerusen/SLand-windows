using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Tepi.Linux.Core;
using Tepi.Linux.Services;
using Tepi.Linux.UI.Controls;
using Tepi.Linux.UI.Tabs;

namespace Tepi.Linux.UI
{
    public enum IslandVisualState
    {
        CompactIdle,
        CompactMedia,
        Toast,
        AgentApprove,
        Expanded
    }

    public partial class IslandWindow : Window
    {
        private readonly Config _config = new();
        private readonly MprisService _mpris = new();
        private readonly AgentBridge _agent = new();
        private readonly PrayerService _prayer = new();
        private readonly TaskManager _taskManager = new();
        private readonly NotificationStore _notifStore = new();

        // Physics Springs for exact morphing (from Base.cs audit)
        private readonly Spring _springW = Spring.ForWidth(96f);
        private readonly Spring _springH = Spring.ForHeight(34f);
        private readonly Spring _springR = Spring.ForRadius(17f);
        private readonly DispatcherTimer _morphTimer;

        // Background services & state timers
        private readonly DispatcherTimer _clockTimer = new();
        private IslandVisualState _currentState = IslandVisualState.CompactIdle;
        private int _activeTabIndex = 0;

        // User Configuration State
        private string _positionAlign = "Center";
        private int _positionOffset = 0;
        private string _idleDisplayMode = "Mascot";
        private string? _activeFocusCountdown = null;

        // Modular Tab Views (Phase 2B)
        private OverviewTabView? _overviewTab;
        private MusicTabView? _musicTab;
        private AgentsTabView? _agentsTab;
        private TasksTabView? _tasksTab;
        private MessagesTabView? _messagesTab;
        private ShelfTabView? _shelfTab;
        private ControlTabView? _controlTab;
        private RemindTabView? _remindTab;
        private PhoneTabView? _phoneTab;
        private SettingsTabView? _settingsTab;

        public IslandWindow()
        {
            InitializeComponent();

            // 1. Load User Preferences from settings.ini
            _positionAlign = _config.Get("PositionAlign", "Center");
            _positionOffset = _config.GetInt("PositionOffset", 0);
            _idleDisplayMode = _config.Get("IdleDisplay", "Mascot");
            int savedFps = _config.GetInt("FpsLimit", 30);
            bool savedOpaque = _config.GetBool("SolidOpaque", false);

            // Honor CLI flag --opaque if passed, otherwise use saved config
            bool initialOpaque = Logger.IsOpaque || savedOpaque;
            ApplySurfaceStyling(initialOpaque);

            // 2. Initialize Morphing Timer (configured with saved FPS limit)
            _morphTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(savedFps == 15 ? 66 : 33) };
            _morphTimer.Tick += OnMorphTick;

            // 3. Setup Clock Timer (1s)
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClock();
            _clockTimer.Start();

            // 4. Center / align window at top of screen on Open
            Opened += (s, e) => UpdateWindowPosition();

            // 5. Connect Linux Services
            _mpris.TrackChanged += track => Dispatcher.UIThread.Post(() => OnTrackUpdated(track));
            _agent.StatusChanged += status => Dispatcher.UIThread.Post(() => OnAgentUpdated(status));
            _agent.Start();

            // 6. Initialize Modular Tab Views (Phase 2B-1 & 2B-2)
            _overviewTab = new OverviewTabView(SelectTab, _mpris, _agent, _prayer, _taskManager, _notifStore);
            _musicTab = new MusicTabView(_mpris);
            _agentsTab = new AgentsTabView(_agent);
            _tasksTab = new TasksTabView(_taskManager);
            _messagesTab = new MessagesTabView(_notifStore);
            _shelfTab = new ShelfTabView();
            _controlTab = new ControlTabView(opaque => ApplySurfaceStyling(opaque));
            _remindTab = new RemindTabView(_prayer, time => OnFocusCountdown(time));
            _phoneTab = new PhoneTabView();
            _settingsTab = new SettingsTabView(
                _config,
                (align, offset) =>
                {
                    _positionAlign = align;
                    _positionOffset = offset;
                    UpdateWindowPosition();
                },
                mode => SetIdleDisplayMode(mode),
                opaque => ApplySurfaceStyling(opaque),
                fps => SetFpsLimit(fps),
                () => Close()
            );

            // 7. Apply Idle Display Mode
            SetIdleDisplayMode(_idleDisplayMode);

            // Set initial state
            SetState(IslandVisualState.CompactIdle, immediate: true);
            SelectTab(0);
            UpdateClock();

            Logger.Log($"IslandWindow initialized: Align={_positionAlign}, Offset={_positionOffset}, IdleDisplay={_idleDisplayMode}, Opaque={Logger.IsOpaque}, FPS={savedFps}");
        }

        public void ApplySurfaceStyling(bool? forceOpaque = null)
        {
            bool opaque = forceOpaque ?? Logger.IsOpaque;
            Logger.IsOpaque = opaque;

            if (opaque)
            {
                TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
                Background = TepiTheme.OpaqueBackground;
                IslandBorder.Background = TepiTheme.OpaqueBackground;
                IslandBorder.BorderBrush = TepiTheme.OpaqueBorder;
                IslandBorder.BorderThickness = new Thickness(1);
                IslandBorder.BoxShadow = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow());
            }
            else
            {
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
                Background = Avalonia.Media.Brushes.Transparent;
                IslandBorder.Background = TepiTheme.HullBackground;
                IslandBorder.BorderBrush = TepiTheme.HullBorderStroke;
                IslandBorder.BorderThickness = new Thickness(1);
                IslandBorder.BoxShadow = new Avalonia.Media.BoxShadows(
                    new Avalonia.Media.BoxShadow
                    {
                        Blur = 18,
                        Spread = 0,
                        OffsetY = 4,
                        Color = Avalonia.Media.Color.FromArgb(140, 0, 0, 0)
                    });
            }
        }

        public void SetState(IslandVisualState newState, bool immediate = false)
        {
            _currentState = newState;

            float targetW, targetH, targetR;

            CompactIdleView.IsVisible = newState == IslandVisualState.CompactIdle;
            CompactMediaView.IsVisible = newState == IslandVisualState.CompactMedia;
            MediaHairlineProgress.IsVisible = newState == IslandVisualState.CompactMedia;
            ToastView.IsVisible = newState == IslandVisualState.Toast;
            AgentApproveView.IsVisible = newState == IslandVisualState.AgentApprove;
            ExpandedPanelView.IsVisible = newState == IslandVisualState.Expanded;

            switch (newState)
            {
                case IslandVisualState.CompactIdle:
                    targetW = 96f;
                    targetH = 34f;
                    targetR = 17f;
                    break;

                case IslandVisualState.CompactMedia:
                    int titleLen = MediaTitleCompact?.Text?.Length ?? 10;
                    targetW = Math.Clamp(112f + titleLen * 7.5f, 240f, 380f);
                    targetH = 38f;
                    targetR = 19f;
                    break;

                case IslandVisualState.Toast:
                    targetW = 404f;
                    targetH = 66f;
                    targetR = 26f;
                    break;

                case IslandVisualState.AgentApprove:
                    targetW = 452f;
                    targetH = 220f;
                    targetR = 30f;
                    break;

                case IslandVisualState.Expanded:
                default:
                    targetW = 460f;
                    targetH = 320f;
                    targetR = 30f;
                    break;
            }

            _springW.Target = targetW;
            _springH.Target = targetH;
            _springR.Target = targetR;

            if (immediate)
            {
                _springW.Snap(targetW);
                _springH.Snap(targetH);
                _springR.Snap(targetR);
                ApplyGeometry();
            }
            else
            {
                if (!_morphTimer.IsEnabled)
                {
                    _morphTimer.Start();
                }
            }

            Logger.Log($"Island morphed to {newState} (Target: {targetW}x{targetH}, R={targetR})");
        }

        private void OnMorphTick(object? sender, EventArgs e)
        {
            float dt = 0.033f;
            bool wMoving = _springW.Step(dt);
            bool hMoving = _springH.Step(dt);
            bool rMoving = _springR.Step(dt);

            ApplyGeometry();

            if (!wMoving && !hMoving && !rMoving)
            {
                _morphTimer.Stop(); // Guarantee 0% CPU at rest!
            }
        }

        private void ApplyGeometry()
        {
            IslandBorder.Width = _springW.Value;
            IslandBorder.Height = _springH.Value;
            IslandBorder.CornerRadius = new CornerRadius(_springR.Value);

            Width = _springW.Value + 8;
            Height = _springH.Value + 12;

            UpdateWindowPosition();
        }

        private void UpdateWindowPosition()
        {
            try
            {
                var screen = Screens.Primary ?? Screens.All[0];
                int screenWidth = screen.WorkingArea.Width;
                int basePosX = _positionAlign switch
                {
                    "Left" => 16,
                    "Right" => screenWidth - (int)Width - 16,
                    _ => (screenWidth - (int)Width) / 2
                };
                int finalX = Math.Clamp(basePosX + _positionOffset, 0, Math.Max(0, screenWidth - (int)Width));
                Position = new PixelPoint(finalX, 8);
            }
            catch (Exception ex)
            {
                Logger.LogError("UpdateWindowPosition", ex);
            }
        }

        public void SetIdleDisplayMode(string mode)
        {
            _idleDisplayMode = mode;
            if (mode == "Clock")
            {
                EyeMascot.IsVisible = false;
                ClockText.IsVisible = true;
            }
            else if (mode == "Both")
            {
                EyeMascot.IsVisible = true;
                ClockText.IsVisible = true;
            }
            else
            {
                EyeMascot.IsVisible = true;
                ClockText.IsVisible = false;
            }
        }

        public void SetFpsLimit(int fps)
        {
            int ms = fps == 15 ? 66 : 33;
            _morphTimer.Interval = TimeSpan.FromMilliseconds(ms);
        }

        private void OnFocusCountdown(string? time)
        {
            _activeFocusCountdown = time;
            Dispatcher.UIThread.Post(() =>
            {
                if (!string.IsNullOrEmpty(time))
                {
                    BottomStatusText.Text = $"Tepi ARM64 • Fokus: {time}";
                }
                else
                {
                    string[] tabTitles = {
                        "Ringkasan", "Musik & Lirik", "Agen Coding", "Kuliah & Tugas", "Pesan Masuk",
                        "Rak & Clipboard", "Kontrol Sistem", "Pengingat & Sholat", "HP Link", "Pengaturan"
                    };
                    string title = _activeTabIndex < tabTitles.Length ? tabTitles[_activeTabIndex] : "Aktif";
                    BottomStatusText.Text = $"Tepi ARM64 • {title}";
                }
            });
        }

        private void UpdateClock()
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm");

            // Check if evening -> sleepy eyes
            int hour = DateTime.Now.Hour;
            if (hour >= 23 || hour < 5)
            {
                if (EyeMascot.Mood == EyeMood.None) EyeMascot.Mood = EyeMood.Sleepy;
            }
        }

        private void OnTrackUpdated(MediaTrack track)
        {
            MediaTitleCompact.Text = track.Title;
            MediaArtistCompact.Text = track.Artist;
            MediaPlayPauseQuickIcon.Text = track.IsPlaying ? "⏸" : "▶";
            MediaEqualizer.IsPlaying = track.IsPlaying;

            if (_activeTabIndex == 0)
            {
                _overviewTab?.RefreshData();
            }

            if (track.IsPlaying)
            {
                EyeMascot.Mood = EyeMood.Happy;
                IdleMediaIndicator.IsVisible = true;
                if (_currentState == IslandVisualState.CompactIdle)
                {
                    SetState(IslandVisualState.CompactMedia);
                }
            }
            else
            {
                IdleMediaIndicator.IsVisible = false;
                if (EyeMascot.Mood == EyeMood.Happy) EyeMascot.Mood = EyeMood.None;
                if (_currentState == IslandVisualState.CompactMedia)
                {
                    SetState(IslandVisualState.CompactIdle);
                }
            }
        }

        private void OnAgentUpdated(AgentStatus status)
        {
            if (status.NeedsApproval)
            {
                _notifStore.Add(status.AgentName, "Persetujuan Perintah", status.ApprovalPrompt);
                if (_activeTabIndex == 0) _overviewTab?.RefreshData();

                AgentProjectName.Text = status.AgentName;
                AgentKindBadge.Text = status.AgentName;
                AgentCommandText.Text = string.IsNullOrEmpty(status.ApprovalPrompt)
                    ? status.StatusText
                    : status.ApprovalPrompt;

                EyeMascot.Mood = EyeMood.Surprised;
                SetState(IslandVisualState.AgentApprove);
            }
        }

        // ========================================================
        // POINTER & GAZE TRACKING (0% CPU at Idle)
        // ========================================================
        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            if (_currentState == IslandVisualState.CompactIdle)
            {
                _springW.Target = 104f; // Subtle hover pop
                if (!_morphTimer.IsEnabled) _morphTimer.Start();
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            EyeMascot.ResetGaze();

            if (_currentState == IslandVisualState.CompactIdle)
            {
                _springW.Target = 96f;
                if (!_morphTimer.IsEnabled) _morphTimer.Start();
            }
        }

        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            if (_currentState == IslandVisualState.CompactIdle)
            {
                var pos = e.GetPosition(IslandBorder);
                var center = new Point(pos.X - IslandBorder.Bounds.Width / 2f, pos.Y - IslandBorder.Bounds.Height / 2f);
                EyeMascot.TrackPointer(center);
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_currentState == IslandVisualState.CompactIdle || _currentState == IslandVisualState.CompactMedia)
            {
                SetState(IslandVisualState.Expanded);
            }
        }

        // ========================================================
        // MEDIA CONTROL HOVER MUTATION (Equalizer <-> Play/Pause)
        // ========================================================
        private void OnMediaControlHover(object? sender, PointerEventArgs e)
        {
            MediaEqualizer.IsVisible = false;
            MediaPlayPauseQuickBtn.IsVisible = true;
        }

        private void OnMediaControlUnhover(object? sender, PointerEventArgs e)
        {
            MediaEqualizer.IsVisible = true;
            MediaPlayPauseQuickBtn.IsVisible = false;
        }

        private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
        {
            _mpris.PlayPause();
        }

        // ========================================================
        // AGENT ACTIONS
        // ========================================================
        private void OnAgentApproveClick(object? sender, RoutedEventArgs e)
        {
            _agent.Approve();
            EyeMascot.Mood = EyeMood.Happy;
            SetState(IslandVisualState.CompactIdle);
        }

        private void OnAgentRejectClick(object? sender, RoutedEventArgs e)
        {
            _agent.Reject();
            EyeMascot.Mood = EyeMood.None;
            SetState(IslandVisualState.CompactIdle);
        }

        // ========================================================
        // 10 TAB NAVIGATION (COMPACT 460 PX)
        // ========================================================
        private void OnTabClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int tabIndex))
            {
                SelectTab(tabIndex);
            }
        }

        private void SelectTab(int index)
        {
            _activeTabIndex = index;

            string[] tabTitles = {
                "Ringkasan", "Musik & Lirik", "Agen Coding", "Kuliah & Tugas", "Pesan Masuk",
                "Rak & Clipboard", "Kontrol Sistem", "Pengingat & Sholat", "HP Link", "Pengaturan"
            };

            string title = index < tabTitles.Length ? tabTitles[index] : $"Tab {index}";
            if (BottomStatusText != null)
            {
                BottomStatusText.Text = $"Tepi ARM64 • {title}";
            }

            // Update tab button visual state (only active tab shows text label)
            for (int i = 0; i < TabBar.Children.Count; i++)
            {
                if (TabBar.Children[i] is Button tabBtn)
                {
                    bool isActive = i == index;
                    tabBtn.Classes.Set("active", isActive);

                    if (tabBtn.Content is StackPanel sp && sp.Children.Count > 1)
                    {
                        if (sp.Children[1] is TextBlock label)
                        {
                            label.IsVisible = isActive;
                        }
                    }
                }
            }

            // Switch modular content host (All 10 Tabs)
            switch (index)
            {
                case 0:
                    _overviewTab?.RefreshData();
                    TabContentHost.Content = _overviewTab;
                    break;
                case 1:
                    TabContentHost.Content = _musicTab;
                    break;
                case 2:
                    TabContentHost.Content = _agentsTab;
                    break;
                case 3:
                    TabContentHost.Content = _tasksTab;
                    break;
                case 4:
                    TabContentHost.Content = _messagesTab;
                    break;
                case 5:
                    _ = _shelfTab?.CheckClipboardAsync();
                    TabContentHost.Content = _shelfTab;
                    break;
                case 6:
                    TabContentHost.Content = _controlTab;
                    break;
                case 7:
                    TabContentHost.Content = _remindTab;
                    break;
                case 8:
                    TabContentHost.Content = _phoneTab;
                    break;
                case 9:
                    TabContentHost.Content = _settingsTab;
                    break;
                default:
                    TabContentHost.Content = CreatePlaceholderCard(index);
                    break;
            }

            Logger.Log($"Switched to Tab {index}: {title}");
        }

        private Control CreatePlaceholderCard(int index)
        {
            string[] names = {
                "Ringkasan", "Musik", "Agen", "Kuliah", "Pesan",
                "Rak & Clipboard", "Kontrol Sistem", "Pengingat & Sholat", "HP Link (Android)", "Pengaturan Tepi"
            };
            string name = index < names.Length ? names[index] : $"Tab {index}";

            return new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Child = new StackPanel
                {
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"🔧 Modul {name}",
                            FontSize = 13,
                            FontWeight = Avalonia.Media.FontWeight.SemiBold,
                            Foreground = TepiTheme.BrushText,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = "Sedang disiapkan pada sub-tahap Fase 2B-2.",
                            FontSize = 11,
                            Foreground = TepiTheme.BrushSub,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            SetState(IslandVisualState.CompactIdle);
        }
    }
}
