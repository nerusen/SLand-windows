using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Tabs
{
    public class ShelfTabView : UserControl
    {
        private readonly List<string> _clipItems = new();
        private readonly List<string> _shelfItems = new();
        private readonly StackPanel _clipListContainer;
        private readonly StackPanel _shelfListContainer;
        private readonly TextBox _shelfInput;
        private readonly TextBlock _feedbackText;

        public ShelfTabView()
        {
            var root = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Thickness(4, 2, 4, 2)
            };

            // 1. Header Toolbar (Title + Refresh + Clear)
            var headerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                Margin = new Thickness(2, 0, 2, 8)
            };

            var titleText = new TextBlock
            {
                Text = "Rak & Clipboard X11",
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);

            var refreshBtn = new Button
            {
                Content = "🔄 Segarkan",
                FontSize = 10.5,
                Padding = new Thickness(8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushText,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            refreshBtn.Click += async (s, e) => await CheckClipboardAsync();
            Grid.SetColumn(refreshBtn, 1);

            var clearBtn = new Button
            {
                Content = "🗑 Hapus Riwayat",
                FontSize = 10.5,
                Padding = new Thickness(8, 3),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushNegative,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            clearBtn.Click += (s, e) =>
            {
                _clipItems.Clear();
                RenderClipList();
                ShowFeedback("Riwayat clipboard dibersihkan");
            };
            Grid.SetColumn(clearBtn, 2);

            headerGrid.Children.Add(titleText);
            headerGrid.Children.Add(refreshBtn);
            headerGrid.Children.Add(clearBtn);
            Grid.SetRow(headerGrid, 0);
            root.Children.Add(headerGrid);

            // 2. Main Scrollable Container (Split: Riwayat Clipboard + Quick Shelf)
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };

            var bodyStack = new StackPanel { Spacing = 10 };

            // Section A: Riwayat Clipboard X11
            var clipHeader = new TextBlock
            {
                Text = "RIWAYAT CLIPBOARD X11",
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(2, 0, 0, 2)
            };
            bodyStack.Children.Add(clipHeader);

            _clipListContainer = new StackPanel { Spacing = 5 };
            bodyStack.Children.Add(_clipListContainer);

            // Section B: Quick Shelf (Catatan / Berkas Cepat)
            var shelfHeader = new TextBlock
            {
                Text = "QUICK SHELF (CATATAN SEMENTARA)",
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(2, 8, 0, 2)
            };
            bodyStack.Children.Add(shelfHeader);

            var shelfInputGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(0, 0, 0, 4)
            };

            _shelfInput = new TextBox
            {
                Watermark = "Ketik teks atau tautan untuk disimpan di rak...",
                FontSize = 11,
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                Foreground = TepiTheme.BrushText,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 5)
            };
            _shelfInput.KeyDown += (s, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter) AddToShelf();
            };
            Grid.SetColumn(_shelfInput, 0);

            var addShelfBtn = new Button
            {
                Content = "+ Simpan",
                FontSize = 11,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Padding = new Thickness(10, 5),
                Margin = new Thickness(6, 0, 0, 0),
                Background = TepiTheme.BrushAccent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            addShelfBtn.Click += (s, e) => AddToShelf();
            Grid.SetColumn(addShelfBtn, 1);

            shelfInputGrid.Children.Add(_shelfInput);
            shelfInputGrid.Children.Add(addShelfBtn);
            bodyStack.Children.Add(shelfInputGrid);

            _shelfListContainer = new StackPanel { Spacing = 5 };
            bodyStack.Children.Add(_shelfListContainer);

            scroll.Content = bodyStack;
            Grid.SetRow(scroll, 1);
            root.Children.Add(scroll);

            // 3. Bottom Feedback Bar
            _feedbackText = new TextBlock
            {
                Text = "💡 Klik tombol 'Salin' untuk menempatkan kembali ke clipboard X11.",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(2, 6, 2, 0)
            };
            Grid.SetRow(_feedbackText, 2);
            root.Children.Add(_feedbackText);

            Content = root;

            // Seed initial sample and read X11
            _shelfItems.Add("curl -sSL https://get.docker.com | sh");
            _shelfItems.Add("export DISPLAY=:0 && startxfce4 &");
            RenderShelfList();
            RenderClipList();

            AttachedToVisualTree += async (s, e) => await CheckClipboardAsync();
        }

        public async Task CheckClipboardAsync()
        {
            try
            {
                string? text = null;
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    text = await topLevel.Clipboard.GetTextAsync();
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    // Fallback to xclip/xsel command
                    text = ReadClipboardProcess();
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    text = text.Trim();
                    if (!_clipItems.Contains(text))
                    {
                        _clipItems.Insert(0, text);
                        if (_clipItems.Count > 15) _clipItems.RemoveAt(_clipItems.Count - 1);
                        RenderClipList();
                        ShowFeedback("Clipboard X11 diperbarui");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("CheckClipboardAsync", ex);
            }
        }

        private string? ReadClipboardProcess()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "xclip",
                    Arguments = "-o -selection clipboard",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p != null)
                {
                    string outText = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(300);
                    return outText;
                }
            }
            catch { }
            return null;
        }

        private async Task CopyToClipboardAsync(string text)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(text);
                }

                // Fallback shell
                _ = Task.Run(() =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "sh",
                            Arguments = $"-c \"printf '%s' '{text.Replace("'", "'\\''")}' | xclip -selection clipboard\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using var p = Process.Start(psi);
                        p?.WaitForExit(300);
                    }
                    catch { }
                });

                ShowFeedback("✓ Teks berhasil disalin ke clipboard!");
            }
            catch (Exception ex)
            {
                Logger.LogError("CopyToClipboardAsync", ex);
            }
        }

        private void AddToShelf()
        {
            string text = _shelfInput.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
            {
                _shelfItems.Insert(0, text);
                _shelfInput.Text = "";
                RenderShelfList();
                ShowFeedback("Disimpan ke Rak");
            }
        }

        private void RenderClipList()
        {
            _clipListContainer.Children.Clear();

            if (_clipItems.Count == 0)
            {
                _clipListContainer.Children.Add(new Border
                {
                    Background = TepiTheme.CardBackground,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8),
                    Child = new TextBlock
                    {
                        Text = "Belum ada riwayat clipboard. Salin teks lalu klik 🔄 Segarkan.",
                        FontSize = 11,
                        Foreground = TepiTheme.BrushFaint
                    }
                });
                return;
            }

            foreach (var item in _clipItems)
            {
                _clipListContainer.Children.Add(CreateClipCard(item, isShelf: false));
            }
        }

        private void RenderShelfList()
        {
            _shelfListContainer.Children.Clear();

            if (_shelfItems.Count == 0)
            {
                _shelfListContainer.Children.Add(new Border
                {
                    Background = TepiTheme.CardBackground,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8),
                    Child = new TextBlock
                    {
                        Text = "Rak kosong. Tambahkan catatan cepat di atas.",
                        FontSize = 11,
                        Foreground = TepiTheme.BrushFaint
                    }
                });
                return;
            }

            foreach (var item in _shelfItems)
            {
                _shelfListContainer.Children.Add(CreateClipCard(item, isShelf: true));
            }
        }

        private Control CreateClipCard(string text, bool isShelf)
        {
            var card = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto")
            };

            var label = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 0);

            var copyBtn = new Button
            {
                Content = "Salin",
                FontSize = 10,
                Padding = new Thickness(8, 2),
                Margin = new Thickness(6, 0, 4, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushAccent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            copyBtn.Click += async (s, e) => await CopyToClipboardAsync(text);
            Grid.SetColumn(copyBtn, 1);

            var delBtn = new Button
            {
                Content = "✕",
                FontSize = 10,
                Padding = new Thickness(6, 2),
                Background = Brushes.Transparent,
                Foreground = TepiTheme.BrushFaint,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            delBtn.Click += (s, e) =>
            {
                if (isShelf)
                {
                    _shelfItems.Remove(text);
                    RenderShelfList();
                }
                else
                {
                    _clipItems.Remove(text);
                    RenderClipList();
                }
            };
            Grid.SetColumn(delBtn, 2);

            grid.Children.Add(label);
            grid.Children.Add(copyBtn);
            grid.Children.Add(delBtn);

            card.Child = grid;
            return card;
        }

        private void ShowFeedback(string msg)
        {
            _feedbackText.Text = $"✓ {msg}";
            _feedbackText.Foreground = TepiTheme.BrushPositive;
        }
    }
}
