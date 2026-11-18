using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI
{
    public partial class IslandWindow : Window
    {
        private readonly Config _config = new();
        private readonly MprisService _mpris = new();
        private readonly AgentBridge _agent = new();
        private readonly PrayerService _prayer = new();

        private readonly DispatcherTimer _clockTimer = new();
        private readonly DispatcherTimer _mascotTimer = new();
        private bool _isExpanded;
        private int _mascotState;

        private const double CompactWidth = 146;
        private const double CompactHeight = 36;
        private const double ExpandedWidth = 380;
        private const double ExpandedHeight = 210;

        public IslandWindow()
        {
            InitializeComponent();

            // Handle --opaque mode or VNC mitigation
            if (Logger.IsOpaque)
            {
                TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
                Background = Avalonia.Media.Brush.Parse("#101014");
                IslandBorder.Background = Avalonia.Media.Brush.Parse("#101014");
                IslandBorder.BorderBrush = Avalonia.Media.Brush.Parse("#40FFFFFF");
                IslandBorder.BoxShadow = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow());
                Logger.Log("IslandWindow configured in OPAQUE mode (no alpha blending).");
            }

            SetCompactMode();

            // Center at top of screen
            Opened += (s, e) => PositionTopCenter();

            // Setup clock timer (1s)
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) => UpdateClockAndStatus();
            _clockTimer.Start();

            // Setup mascot blink timer (every 4s)
            _mascotTimer.Interval = TimeSpan.FromSeconds(4);
            _mascotTimer.Tick += (s, e) => TriggerMascotBlink();
            _mascotTimer.Start();

            // Media service callbacks
            _mpris.TrackChanged += track => Dispatcher.UIThread.Post(() => OnTrackUpdated(track));

            // Agent service callbacks
            _agent.StatusChanged += status => Dispatcher.UIThread.Post(() => OnAgentUpdated(status));
            _agent.Start();

            UpdateClockAndStatus();
            Logger.Log("IslandWindow initialized successfully.");
        }

        private void PositionTopCenter()
        {
            try
            {
                var screen = Screens.Primary ?? Screens.All[0];
                int screenWidth = screen.WorkingArea.Width;
                int posX = (screenWidth - (int)Width) / 2;
                Position = new PixelPoint(posX, 8);
                Logger.Log($"PositionTopCenter: ScreenW={screenWidth}, WindowW={Width}, PosX={posX}, PosY=8");
            }
            catch (Exception ex)
            {
                Logger.LogError("PositionTopCenter", ex);
            }
        }

        private void SetCompactMode()
        {
            _isExpanded = false;
            IslandBorder.Width = CompactWidth;
            IslandBorder.Height = CompactHeight;
            IslandBorder.CornerRadius = new CornerRadius(18);
            IdleView.IsVisible = true;
            ExpandedView.IsVisible = false;

            Width = CompactWidth + 6;
            Height = CompactHeight + 8;
            PositionTopCenter();
            Logger.Log("Island switched to Compact Mode.");
        }

        private void SetExpandedMode()
        {
            _isExpanded = true;
            IslandBorder.Width = ExpandedWidth;
            IslandBorder.Height = ExpandedHeight;
            IslandBorder.CornerRadius = new CornerRadius(20);
            IdleView.IsVisible = false;
            ExpandedView.IsVisible = true;

            Width = ExpandedWidth + 6;
            Height = ExpandedHeight + 8;
            PositionTopCenter();

            // Refresh prayer time
            var p = _prayer.GetNextPrayer();
            PrayerText.Text = p.Formatted;
            Logger.Log($"Island switched to Expanded Mode. Next prayer: {p.Formatted}");
        }

        private void UpdateClockAndStatus()
        {
            var now = DateTime.Now;
            ClockText.Text = now.ToString("HH:mm");

            // Check media
            _mpris.PollMetadata();
            StatusGlyph.Text = _mpris.CurrentTrack.IsPlaying ? "♫" : "●";
            StatusGlyph.Foreground = _mpris.CurrentTrack.IsPlaying 
                ? Avalonia.Media.Brushes.LightGreen 
                : Avalonia.Media.Brushes.Gray;
        }

        private void TriggerMascotBlink()
        {
            _mascotState = (_mascotState + 1) % 3;
            MascotText.Text = _mascotState switch
            {
                1 => "( - . - )",
                2 => "( ^ . ^ )",
                _ => "●"
            };
        }

        private void OnTrackUpdated(MediaTrack track)
        {
            TrackTitle.Text = track.Title;
            TrackArtist.Text = track.Artist;
            PlayPauseIcon.Text = track.IsPlaying ? "⏸" : "▶";
            StatusGlyph.Foreground = track.IsPlaying 
                ? Avalonia.Media.Brushes.LightGreen 
                : Avalonia.Media.Brushes.Gray;
        }

        private void OnAgentUpdated(AgentStatus status)
        {
            AgentSection.IsVisible = status.IsActive;
            AgentNameText.Text = status.AgentName;
            AgentPromptText.Text = string.IsNullOrEmpty(status.ApprovalPrompt) 
                ? status.StatusText 
                : status.ApprovalPrompt;
            AgentActions.IsVisible = status.NeedsApproval;

            if (status.NeedsApproval && !_isExpanded)
            {
                SetExpandedMode();
            }
        }

        // Pointer gestures
        private void OnPointerEntered(object? sender, PointerEventArgs e)
        {
            if (!_isExpanded)
            {
                IslandBorder.Width = CompactWidth + 8;
                Width = CompactWidth + 14;
                PositionTopCenter();
            }
        }

        private void OnPointerExited(object? sender, PointerEventArgs e)
        {
            if (!_isExpanded)
            {
                IslandBorder.Width = CompactWidth;
                Width = CompactWidth + 6;
                PositionTopCenter();
            }
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!_isExpanded)
            {
                SetExpandedMode();
            }
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            SetCompactMode();
        }

        private void OnPlayPauseClick(object? sender, RoutedEventArgs e)
        {
            _mpris.PlayPause();
        }

        private void OnPrevTrackClick(object? sender, RoutedEventArgs e)
        {
            _mpris.Previous();
        }

        private void OnNextTrackClick(object? sender, RoutedEventArgs e)
        {
            _mpris.Next();
        }

        private void OnAgentApproveClick(object? sender, RoutedEventArgs e)
        {
            _agent.Approve();
        }

        private void OnAgentRejectClick(object? sender, RoutedEventArgs e)
        {
            _agent.Reject();
        }

        private void OnTimerToggleClick(object? sender, RoutedEventArgs e)
        {
            TimerBtnText.Text = TimerBtnText.Text == "Timer" ? "25:00" : "Timer";
            Logger.Log($"Timer clicked: {TimerBtnText.Text}");
        }

        private void OnExitClick(object? sender, RoutedEventArgs e)
        {
            Logger.Log("Exit requested from Island UI.");
            _agent.Stop();
            Close();
        }
    }
}
