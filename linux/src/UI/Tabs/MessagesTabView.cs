using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Tepi.Linux.Core;
using Tepi.Linux.Services;

namespace Tepi.Linux.UI.Tabs
{
    public class MessagesTabView : UserControl
    {
        private readonly NotificationStore _notifStore;
        private readonly StackPanel _listContainer;
        private readonly TextBlock _feedbackText;

        public MessagesTabView(NotificationStore notifStore)
        {
            _notifStore = notifStore;

            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Thickness(4, 2, 4, 2)
            };

            // 1. Header Row (Title & Clear All)
            var headerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                Margin = new Thickness(2, 0, 2, 8)
            };

            var title = new TextBlock
            {
                Text = "Riwayat Pesan & Notifikasi",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            headerGrid.Children.Add(title);
            Grid.SetColumn(title, 0);

            _feedbackText = new TextBlock
            {
                Text = "",
                FontSize = 10.5,
                Foreground = TepiTheme.BrushPositive,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            headerGrid.Children.Add(_feedbackText);
            Grid.SetColumn(_feedbackText, 1);

            var clearBtn = new Button
            {
                Content = "Bersihkan Semua",
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushSub,
                FontSize = 10.5,
                Padding = new Thickness(8, 4),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            clearBtn.Click += (s, e) => _notifStore.ClearAll();
            headerGrid.Children.Add(clearBtn);
            Grid.SetColumn(clearBtn, 2);

            root.Children.Add(headerGrid);
            Grid.SetRow(headerGrid, 0);

            // 2. Scrollable Message List
            var scroll = new ScrollViewer
            {
                Height = 145,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };

            _listContainer = new StackPanel { Spacing = 6 };
            scroll.Content = _listContainer;
            root.Children.Add(scroll);
            Grid.SetRow(scroll, 1);

            // 3. Footer info
            var footerText = new TextBlock
            {
                Text = "Klik kartu pesan untuk menyalin teks ke clipboard.",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(2, 6, 2, 0)
            };
            root.Children.Add(footerText);
            Grid.SetRow(footerText, 2);

            Content = root;

            _notifStore.NotificationsChanged += () =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(RenderList);
            };

            RenderList();
        }

        private void RenderList()
        {
            _listContainer.Children.Clear();
            var items = _notifStore.Items;

            if (items.Count == 0)
            {
                var emptyCard = new Border
                {
                    Background = TepiTheme.CardBackground,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                emptyCard.Child = new TextBlock
                {
                    Text = "💬 Tidak ada pesan atau notifikasi tersimpan.",
                    FontSize = 11.5,
                    Foreground = TepiTheme.BrushFaint
                };
                _listContainer.Children.Add(emptyCard);
                return;
            }

            foreach (var item in items)
            {
                var row = CreateNotifRow(item);
                _listContainer.Children.Add(row);
            }
        }

        private Border CreateNotifRow(NotificationItem item)
        {
            var card = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            // App Icon Badge
            var appBadge = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(Color.FromArgb(28, 59, 130, 246)),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            appBadge.Child = new TextBlock
            {
                Text = item.App.Length > 0 ? item.App.Substring(0, 1).ToUpper() : "N",
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = TepiTheme.BrushAccent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(appBadge);
            Grid.SetColumn(appBadge, 0);

            // Title & Body
            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            textStack.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            textStack.Children.Add(new TextBlock
            {
                Text = item.Body,
                FontSize = 11,
                Foreground = TepiTheme.BrushSub,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0)
            });
            grid.Children.Add(textStack);
            Grid.SetColumn(textStack, 1);

            // Time text
            var timeText = new TextBlock
            {
                Text = item.Time.ToString("HH:mm"),
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint,
                VerticalAlignment = VerticalAlignment.Top
            };
            grid.Children.Add(timeText);
            Grid.SetColumn(timeText, 2);

            card.Child = grid;

            // Click to copy
            card.PointerPressed += async (s, e) =>
            {
                try
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.Clipboard != null)
                    {
                        await topLevel.Clipboard.SetTextAsync($"{item.Title}: {item.Body}");
                        _feedbackText.Text = "Tersalin!";
                        Avalonia.Threading.DispatcherTimer.RunOnce(() => _feedbackText.Text = "", TimeSpan.FromSeconds(2));
                    }
                }
                catch { }
            };

            return card;
        }
    }
}
