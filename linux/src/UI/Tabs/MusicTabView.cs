using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class MusicTabView : UserControl
    {
        private readonly MprisService _mpris;
        private readonly TextBlock _titleText;
        private readonly TextBlock _artistText;
        private readonly TextBlock _timeText;
        private readonly TextBlock _playPauseIcon;
        private readonly Slider _progressSlider;
        private readonly TextBlock _lyricsText;

        public MusicTabView(MprisService mpris)
        {
            _mpris = mpris;

            var root = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(4, 2, 4, 2)
            };

            // 1. Header Card (Cover Art + Title + Artist)
            var trackCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var trackGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*")
            };

            // Album Art Box (48x48 rounded)
            var coverBox = new Border
            {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 46)),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            coverBox.Child = new TextBlock
            {
                Text = "♫",
                FontSize = 20,
                Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            trackGrid.Children.Add(coverBox);
            Grid.SetColumn(coverBox, 0);

            var metaStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            _titleText = new TextBlock
            {
                Text = _mpris.CurrentTrack.Title,
                FontSize = 13.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _artistText = new TextBlock
            {
                Text = _mpris.CurrentTrack.Artist,
                FontSize = 11.5,
                Foreground = TepiTheme.BrushSub,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            };
            metaStack.Children.Add(_titleText);
            metaStack.Children.Add(_artistText);
            trackGrid.Children.Add(metaStack);
            Grid.SetColumn(metaStack, 1);

            trackCard.Child = trackGrid;
            root.Children.Add(trackCard);

            // 2. Playback Controls Card
            var controlsCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 8)
            };

            var controlsStack = new StackPanel { Spacing = 8 };

            // Progress Bar & Duration
            _progressSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = 35,
                Height = 22,
                Margin = new Thickness(0, -4, 0, -4)
            };
            controlsStack.Children.Add(_progressSlider);

            var timeRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto")
            };
            _timeText = new TextBlock
            {
                Text = "01:14 / 03:28",
                FontSize = 10.5,
                Foreground = TepiTheme.BrushFaint
            };
            timeRow.Children.Add(_timeText);
            controlsStack.Children.Add(timeRow);

            // Button Row (Shuffle, Prev, Play/Pause, Next, Repeat)
            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 14
            };

            // Shuffle
            var shuffleBtn = CreateCircleButton("🔀", 11, () => { });
            buttonRow.Children.Add(shuffleBtn);

            // Previous
            var prevBtn = CreateCircleButton("⏮", 12, () => _mpris.Previous());
            buttonRow.Children.Add(prevBtn);

            // Primary Play / Pause
            var playPauseBtn = new Button
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(19),
                Background = TepiTheme.BrushAccent,
                Padding = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            _playPauseIcon = new TextBlock
            {
                Text = _mpris.CurrentTrack.IsPlaying ? "⏸" : "▶",
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            playPauseBtn.Content = _playPauseIcon;
            playPauseBtn.Click += (s, e) => _mpris.PlayPause();
            buttonRow.Children.Add(playPauseBtn);

            // Next
            var nextBtn = CreateCircleButton("⏭", 12, () => _mpris.Next());
            buttonRow.Children.Add(nextBtn);

            // Repeat
            var repeatBtn = CreateCircleButton("🔁", 11, () => { });
            buttonRow.Children.Add(repeatBtn);

            controlsStack.Children.Add(buttonRow);
            controlsCard.Child = controlsStack;
            root.Children.Add(controlsCard);

            // 3. Synchronized Lyrics Preview Card
            var lyricsCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var lyricsStack = new StackPanel { Spacing = 4 };
            lyricsStack.Children.Add(new TextBlock
            {
                Text = "Lirik Tersinkronisasi",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushSub
            });
            _lyricsText = new TextBlock
            {
                Text = "♫ " + _mpris.CurrentTrack.Title + "\nLirik tersinkronisasi otomatis membaca sesi MPRIS2 pemutar musik.",
                FontSize = 11.5,
                Foreground = TepiTheme.BrushText,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };
            lyricsStack.Children.Add(_lyricsText);
            lyricsCard.Child = lyricsStack;
            root.Children.Add(lyricsCard);

            Content = root;

            // Subscribe to MPRIS updates
            _mpris.TrackChanged += OnTrackChanged;
        }

        private void OnTrackChanged(MediaTrack track)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _titleText.Text = track.Title;
                _artistText.Text = track.Artist;
                _playPauseIcon.Text = track.IsPlaying ? "⏸" : "▶";
                _lyricsText.Text = "♫ " + track.Title + "\nLirik tersinkronisasi otomatis membaca sesi MPRIS2 pemutar musik.";
            });
        }

        private Button CreateCircleButton(string glyph, double fontSize, Action action)
        {
            var btn = new Button
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = TepiTheme.BrushFill,
                Padding = new Thickness(0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            btn.Content = new TextBlock
            {
                Text = glyph,
                FontSize = fontSize,
                Foreground = TepiTheme.BrushText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Click += (s, e) => action();
            return btn;
        }
    }
}
