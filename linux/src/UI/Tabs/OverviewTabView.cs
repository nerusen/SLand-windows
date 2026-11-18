using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class OverviewTabView : UserControl
    {
        private readonly Action<int> _navigateToTab;
        private readonly MprisService _mpris;
        private readonly AgentBridge _agent;
        private readonly PrayerService _prayer;
        private readonly TaskManager _tasks;
        private readonly NotificationStore _notifs;

        public OverviewTabView(
            Action<int> navigateToTab,
            MprisService mpris,
            AgentBridge agent,
            PrayerService prayer,
            TaskManager tasks,
            NotificationStore notifs)
        {
            _navigateToTab = navigateToTab;
            _mpris = mpris;
            _agent = agent;
            _prayer = prayer;
            _tasks = tasks;
            _notifs = notifs;

            BuildUI();
        }

        public void RefreshData()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            var root = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };

            var list = new StackPanel
            {
                Spacing = 8,
                Margin = new Thickness(0, 2, 0, 2)
            };

            // 1. Media Priority Card (Ringkasan Musik)
            var mediaCard = CreateCard(
                icon: "♫",
                iconColor: Color.FromRgb(59, 130, 246),
                title: _mpris.CurrentTrack.Title,
                subtitle: _mpris.CurrentTrack.Artist,
                badge: _mpris.CurrentTrack.IsPlaying ? "Sedang Diputar" : "Jeda",
                badgeColor: _mpris.CurrentTrack.IsPlaying ? TepiTheme.ColorPositive : TepiTheme.ColorSub,
                onClick: () => _navigateToTab(1) // Go to Music Tab
            );
            list.Children.Add(mediaCard);

            // 2. Sholat Priority Card
            var prayerInfo = _prayer.GetNextPrayer();
            var prayerCard = CreateCard(
                icon: "🕌",
                iconColor: Color.FromRgb(236, 184, 92),
                title: $"Waktu {prayerInfo.NextName}",
                subtitle: prayerInfo.Formatted,
                badge: "Offline",
                badgeColor: TepiTheme.ColorSub,
                onClick: () => _navigateToTab(7) // Go to Sholat Tab
            );
            list.Children.Add(prayerCard);

            // 3. Coding Agent Priority Card
            var agentStatus = _agent.CurrentStatus;
            var agentCard = CreateCard(
                icon: "⚡",
                iconColor: Color.FromRgb(96, 165, 250),
                title: $"{agentStatus.AgentName} Coding Agent",
                subtitle: agentStatus.NeedsApproval ? "⚠️ Perintah menunggu izin!" : agentStatus.StatusText,
                badge: agentStatus.NeedsApproval ? "Butuh Aksi" : "Siap",
                badgeColor: agentStatus.NeedsApproval ? TepiTheme.ColorNegative : TepiTheme.ColorPositive,
                onClick: () => _navigateToTab(2) // Go to Agent Tab
            );
            list.Children.Add(agentCard);

            // 4. Kuliah & Tugas Card
            int pendingCount = 0;
            foreach (var t in _tasks.Tasks) if (!t.IsDone) pendingCount++;
            var taskCard = CreateCard(
                icon: "🎓",
                iconColor: Color.FromRgb(167, 139, 250),
                title: "Kuliah & Daftar Tugas",
                subtitle: pendingCount > 0 ? $"{pendingCount} tugas belum diselesaikan" : "Semua tugas selesai!",
                badge: $"{pendingCount} Tertunda",
                badgeColor: pendingCount > 0 ? TepiTheme.ColorWarning : TepiTheme.ColorPositive,
                onClick: () => _navigateToTab(3) // Go to Tasks Tab
            );
            list.Children.Add(taskCard);

            // 5. Pesan & Notifikasi Card
            int notifCount = _notifs.Items.Count;
            var notifCard = CreateCard(
                icon: "💬",
                iconColor: Color.FromRgb(52, 211, 153),
                title: "Pusat Notifikasi",
                subtitle: notifCount > 0 ? _notifs.Items[0].Title + " - " + _notifs.Items[0].Body : "Tidak ada pesan baru",
                badge: $"{notifCount} Pesan",
                badgeColor: TepiTheme.ColorSub,
                onClick: () => _navigateToTab(4) // Go to Messages Tab
            );
            list.Children.Add(notifCard);

            root.Content = list;
            Content = root;
        }

        private Border CreateCard(string icon, Color iconColor, string title, string subtitle, string badge, Color badgeColor, Action onClick)
        {
            var card = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            // Left Icon Badge
            var iconBox = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(36, iconColor.R, iconColor.G, iconColor.B)),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBox.Child = new TextBlock
            {
                Text = icon,
                FontSize = 14,
                Foreground = new SolidColorBrush(iconColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(iconBox);
            Grid.SetColumn(iconBox, 0);

            // Center Titles
            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            textStack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            textStack.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 11,
                Foreground = TepiTheme.BrushSub,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            });
            grid.Children.Add(textStack);
            Grid.SetColumn(textStack, 1);

            // Right Pill Badge
            var badgeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(24, badgeColor.R, badgeColor.G, badgeColor.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            badgeBorder.Child = new TextBlock
            {
                Text = badge,
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(badgeColor)
            };
            grid.Children.Add(badgeBorder);
            Grid.SetColumn(badgeBorder, 2);

            card.Child = grid;
            card.PointerPressed += (s, e) => onClick();

            return card;
        }
    }
}
