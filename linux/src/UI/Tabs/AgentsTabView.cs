using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class AgentsTabView : UserControl
    {
        private readonly AgentBridge _agent;
        private readonly TextBlock _agentNameText;
        private readonly TextBlock _statusBadgeText;
        private readonly Border _statusBadge;
        private readonly TextBlock _logTerminalText;
        private readonly StackPanel _actionsRow;

        public AgentsTabView(AgentBridge agent)
        {
            _agent = agent;

            var root = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(4, 2, 4, 2)
            };

            // 1. Agent Status Header Card
            var headerCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var iconBorder = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(36, 59, 130, 246)),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = new TextBlock
            {
                Text = "⚡",
                FontSize = 16,
                Foreground = TepiTheme.BrushAccent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerGrid.Children.Add(iconBorder);
            Grid.SetColumn(iconBorder, 0);

            var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _agentNameText = new TextBlock
            {
                Text = _agent.CurrentStatus.AgentName,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = TepiTheme.BrushText
            };
            infoStack.Children.Add(_agentNameText);
            infoStack.Children.Add(new TextBlock
            {
                Text = "Socket: /tmp/tepi-agent.sock",
                FontSize = 10.5,
                Foreground = TepiTheme.BrushSub,
                Margin = new Thickness(0, 2, 0, 0)
            });
            headerGrid.Children.Add(infoStack);
            Grid.SetColumn(infoStack, 1);

            _statusBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 110, 200, 140)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4),
                VerticalAlignment = VerticalAlignment.Center
            };
            _statusBadgeText = new TextBlock
            {
                Text = "Terhubung",
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushPositive
            };
            _statusBadge.Child = _statusBadgeText;
            headerGrid.Children.Add(_statusBadge);
            Grid.SetColumn(_statusBadge, 2);

            headerCard.Child = headerGrid;
            root.Children.Add(headerCard);

            // 2. Terminal Log & Command Review Box
            var terminalCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 20)),
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 8)
            };

            var termStack = new StackPanel { Spacing = 6 };
            termStack.Children.Add(new TextBlock
            {
                Text = "LOG AKTIVITAS & PERINTAH EKSEKUSI",
                FontSize = 9.5,
                FontWeight = FontWeight.Bold,
                Foreground = TepiTheme.BrushFaint
            });

            var scroll = new ScrollViewer
            {
                Height = 85,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };
            _logTerminalText = new TextBlock
            {
                Text = _agent.CurrentStatus.NeedsApproval
                    ? $"[WAITING PERMISSION]\n{_agent.CurrentStatus.ApprovalPrompt}"
                    : $"[READY] Socket aktif di PRoot\n{_agent.CurrentStatus.StatusText}",
                FontFamily = new FontFamily("Consolas,DejaVu Sans Mono,monospace"),
                FontSize = 11,
                Foreground = _agent.CurrentStatus.NeedsApproval ? TepiTheme.BrushWarning : new SolidColorBrush(Color.FromRgb(220, 225, 235)),
                TextWrapping = TextWrapping.Wrap
            };
            scroll.Content = _logTerminalText;
            termStack.Children.Add(scroll);
            terminalCard.Child = termStack;
            root.Children.Add(terminalCard);

            // 3. Approval Action Buttons (Deny, Always, Allow)
            _actionsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8
            };

            var denyBtn = new Button
            {
                Content = "Tolak",
                Width = 84,
                Height = 28,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 44)),
                Foreground = TepiTheme.BrushNegative,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            denyBtn.Click += (s, e) =>
            {
                _agent.Reject();
                UpdateStatusView(_agent.CurrentStatus);
            };
            _actionsRow.Children.Add(denyBtn);

            var alwaysBtn = new Button
            {
                Content = "Selalu",
                Width = 84,
                Height = 28,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 38)),
                Foreground = TepiTheme.BrushSub,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            alwaysBtn.Click += (s, e) =>
            {
                _agent.Approve();
                UpdateStatusView(_agent.CurrentStatus);
            };
            _actionsRow.Children.Add(alwaysBtn);

            var allowBtn = new Button
            {
                Content = "Izinkan",
                Width = 98,
                Height = 28,
                CornerRadius = new CornerRadius(8),
                Background = TepiTheme.BrushAccent,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            allowBtn.Click += (s, e) =>
            {
                _agent.Approve();
                UpdateStatusView(_agent.CurrentStatus);
            };
            _actionsRow.Children.Add(allowBtn);

            root.Children.Add(_actionsRow);
            Content = root;

            // Subscribe to Agent updates
            _agent.StatusChanged += status =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateStatusView(status));
            };
        }

        private void UpdateStatusView(AgentStatus status)
        {
            _agentNameText.Text = status.AgentName;
            if (status.NeedsApproval)
            {
                _statusBadge.Background = new SolidColorBrush(Color.FromArgb(40, 240, 120, 120));
                _statusBadgeText.Text = "Menunggu Izin";
                _statusBadgeText.Foreground = TepiTheme.BrushNegative;
                _logTerminalText.Text = $"[PERLU PERSETUJUAN]\nPerintah: {status.ApprovalPrompt}";
                _logTerminalText.Foreground = TepiTheme.BrushNegative;
            }
            else
            {
                _statusBadge.Background = new SolidColorBrush(Color.FromArgb(30, 110, 200, 140));
                _statusBadgeText.Text = "Siap";
                _statusBadgeText.Foreground = TepiTheme.BrushPositive;
                _logTerminalText.Text = $"[READY] Antigravity/Claude aktif\nStatus: {status.StatusText}";
                _logTerminalText.Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 235));
            }
        }
    }
}
