using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Tabs
{
    public class PhoneTabView : UserControl
    {
        private const int DefaultPort = 19840;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _isListening = false;

        private readonly TextBlock _statusIndicator;
        private readonly TextBlock _statusText;
        private readonly TextBlock _ipPortText;
        private readonly Button _toggleListenerBtn;
        private readonly StackPanel _messagesContainer;
        private readonly TextBlock _feedbackText;

        public struct ReceivedItem
        {
            public string Type; // OTP, Tautan, Teks
            public string Content;
            public DateTime Time;
        }

        private readonly List<ReceivedItem> _items = new();

        public PhoneTabView()
        {
            var root = new ScrollViewer
            {
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                Margin = new Thickness(4, 2, 4, 2)
            };

            var mainStack = new StackPanel { Spacing = 10 };

            // 1. CONNECTION STATUS & SOCKET LISTENER CARD
            var connCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var connStack = new StackPanel { Spacing = 8 };

            var connHeader = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var phoneIcon = new TextBlock
            {
                Text = "📱",
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(phoneIcon, 0);

            var connTitle = new TextBlock
            {
                Text = "Sambungan HP Link (Android Relay)",
                FontSize = 11.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushText,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(connTitle, 1);

            _toggleListenerBtn = new Button
            {
                Content = "Aktifkan Listener",
                FontSize = 10.5,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Padding = new Thickness(10, 4),
                Background = TepiTheme.BrushAccent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            _toggleListenerBtn.Click += (s, e) => ToggleListener();
            Grid.SetColumn(_toggleListenerBtn, 2);

            connHeader.Children.Add(phoneIcon);
            connHeader.Children.Add(connTitle);
            connHeader.Children.Add(_toggleListenerBtn);
            connStack.Children.Add(connHeader);

            // IP & Port Endpoint display
            var endpointBorder = new Border
            {
                Background = TepiTheme.BrushFill,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6)
            };

            var epGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            _statusIndicator = new TextBlock
            {
                Text = "●",
                FontSize = 10,
                Foreground = TepiTheme.BrushNegative,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            Grid.SetColumn(_statusIndicator, 0);

            _statusText = new TextBlock
            {
                Text = "Listener Nonaktif (Port tertutup)",
                FontSize = 11,
                Foreground = TepiTheme.BrushSub,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_statusText, 1);

            _ipPortText = new TextBlock
            {
                Text = $"{GetLocalIpAddress()}:{DefaultPort}",
                FontSize = 11,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_ipPortText, 2);

            epGrid.Children.Add(_statusIndicator);
            epGrid.Children.Add(_statusText);
            epGrid.Children.Add(_ipPortText);
            endpointBorder.Child = epGrid;
            connStack.Children.Add(endpointBorder);

            connCard.Child = connStack;
            mainStack.Children.Add(connCard);

            // 2. RECEIVED MESSAGES & OTP CARD
            var msgCard = new Border
            {
                Background = TepiTheme.CardBackground,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 10)
            };

            var msgStack = new StackPanel { Spacing = 6 };

            var msgHeaderGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto")
            };

            var msgTitle = new TextBlock
            {
                Text = "PESAN & KODE OTP DITERIMA",
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = TepiTheme.BrushFaint,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(msgTitle, 0);

            var testBtn = new Button
            {
                Content = "+ Simulasi OTP",
                FontSize = 10,
                Padding = new Thickness(8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushAccent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            testBtn.Click += (s, e) => SimulateIncomingOtp();
            Grid.SetColumn(testBtn, 1);

            var clearBtn = new Button
            {
                Content = "Bersihkan",
                FontSize = 10,
                Padding = new Thickness(8, 3),
                Background = TepiTheme.BrushFill,
                Foreground = TepiTheme.BrushNegative,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            clearBtn.Click += (s, e) =>
            {
                _items.Clear();
                RenderMessages();
            };
            Grid.SetColumn(clearBtn, 2);

            msgHeaderGrid.Children.Add(msgTitle);
            msgHeaderGrid.Children.Add(testBtn);
            msgHeaderGrid.Children.Add(clearBtn);
            msgStack.Children.Add(msgHeaderGrid);

            _messagesContainer = new StackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 4, 0, 0)
            };
            msgStack.Children.Add(_messagesContainer);

            _feedbackText = new TextBlock
            {
                Text = "💡 Jalankan Tepi Companion atau kirim teks via socket TCP port 19840.",
                FontSize = 10,
                Foreground = TepiTheme.BrushFaint,
                Margin = new Thickness(2, 4, 2, 0)
            };
            msgStack.Children.Add(_feedbackText);

            msgCard.Child = msgStack;
            mainStack.Children.Add(msgCard);

            root.Content = mainStack;
            Content = root;

            // Seed sample item
            _items.Add(new ReceivedItem
            {
                Type = "OTP",
                Content = "481-902",
                Time = DateTime.Now.AddMinutes(-3)
            });
            RenderMessages();
        }

        private void ToggleListener()
        {
            if (_isListening)
            {
                StopListener();
            }
            else
            {
                StartListener();
            }
        }

        private void StartListener()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, DefaultPort);
                _listener.Start();
                _isListening = true;

                _statusIndicator.Foreground = TepiTheme.BrushPositive;
                _statusText.Text = $"Mendengarkan koneksi di port {DefaultPort}";
                _toggleListenerBtn.Content = "Nonaktifkan";
                _toggleListenerBtn.Background = TepiTheme.BrushFill;
                _toggleListenerBtn.Foreground = TepiTheme.BrushNegative;

                Task.Run(() => AcceptClientsAsync(_cts.Token));
                ShowFeedback("Socket listener TCP 19840 aktif.");
            }
            catch (Exception ex)
            {
                Logger.LogError("StartListener", ex);
                ShowFeedback($"Gagal membuka port: {ex.Message}");
            }
        }

        private void StopListener()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _isListening = false;

                _statusIndicator.Foreground = TepiTheme.BrushNegative;
                _statusText.Text = "Listener Nonaktif (Port tertutup)";
                _toggleListenerBtn.Content = "Aktifkan Listener";
                _toggleListenerBtn.Background = TepiTheme.BrushAccent;
                _toggleListenerBtn.Foreground = Brushes.White;

                ShowFeedback("Socket listener dinonaktifkan.");
            }
            catch { }
        }

        private async Task AcceptClientsAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(ct);
                    _ = Task.Run(() => HandleClientAsync(client, ct), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested) break;
                    Logger.LogError("AcceptClientsAsync", ex);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                try
                {
                    string? line = await reader.ReadLineAsync(ct);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        line = line.Trim();
                        string type = "Teks";
                        if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            type = "Tautan";
                        }
                        else if (System.Text.RegularExpressions.Regex.IsMatch(line, @"^\d{3}[-\s]?\d{3}$"))
                        {
                            type = "OTP";
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            _items.Insert(0, new ReceivedItem
                            {
                                Type = type,
                                Content = line,
                                Time = DateTime.Now
                            });
                            if (_items.Count > 15) _items.RemoveAt(_items.Count - 1);
                            RenderMessages();
                            ShowFeedback($"Pesan baru diterima: {type}");
                        });
                    }
                }
                catch { }
            }
        }

        private void SimulateIncomingOtp()
        {
            var rand = new Random();
            int code = rand.Next(100000, 999999);
            string otp = $"{code / 1000}-{code % 1000:000}";

            _items.Insert(0, new ReceivedItem
            {
                Type = "OTP",
                Content = otp,
                Time = DateTime.Now
            });
            RenderMessages();
            ShowFeedback($"Simulasi OTP: {otp}");
        }

        private void RenderMessages()
        {
            _messagesContainer.Children.Clear();

            if (_items.Count == 0)
            {
                _messagesContainer.Children.Add(new Border
                {
                    Background = TepiTheme.BrushFill,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10, 8),
                    Child = new TextBlock
                    {
                        Text = "Belum ada pesan masuk. Hubungkan ponsel atau klik + Simulasi OTP.",
                        FontSize = 11,
                        Foreground = TepiTheme.BrushFaint
                    }
                });
                return;
            }

            foreach (var item in _items)
            {
                _messagesContainer.Children.Add(CreateMessageCard(item));
            }
        }

        private Control CreateMessageCard(ReceivedItem item)
        {
            var card = new Border
            {
                Background = TepiTheme.BrushFill,
                BorderBrush = TepiTheme.CardBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto")
            };

            // Badge
            var isOtp = item.Type == "OTP";
            var badge = new Border
            {
                Background = isOtp ? new SolidColorBrush(Color.FromArgb(40, 236, 184, 92)) : new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = item.Type,
                    FontSize = 9,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Foreground = isOtp ? TepiTheme.BrushWarning : TepiTheme.BrushAccent
                }
            };
            Grid.SetColumn(badge, 0);

            var contentText = new TextBlock
            {
                Text = item.Content,
                FontSize = isOtp ? 13 : 11,
                FontWeight = isOtp ? Avalonia.Media.FontWeight.Bold : Avalonia.Media.FontWeight.Normal,
                Foreground = isOtp ? Brushes.White : TepiTheme.BrushText,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(contentText, 1);

            var timeText = new TextBlock
            {
                Text = item.Time.ToString("HH:mm"),
                FontSize = 9.5,
                Foreground = TepiTheme.BrushFaint,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0)
            };
            Grid.SetColumn(timeText, 2);

            var copyBtn = new Button
            {
                Content = isOtp ? "Salin OTP" : "Salin",
                FontSize = 10,
                Padding = new Thickness(8, 2),
                Background = isOtp ? TepiTheme.BrushAccent : TepiTheme.BrushFill,
                Foreground = isOtp ? Brushes.White : TepiTheme.BrushAccent,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            copyBtn.Click += async (s, e) =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(item.Content);
                    ShowFeedback($"✓ '{item.Content}' tersalin ke clipboard!");
                }
            };
            Grid.SetColumn(copyBtn, 3);

            grid.Children.Add(badge);
            grid.Children.Add(contentText);
            grid.Children.Add(timeText);
            grid.Children.Add(copyBtn);

            card.Child = grid;
            return card;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var props = ni.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        private void ShowFeedback(string msg)
        {
            _feedbackText.Text = msg;
            _feedbackText.Foreground = TepiTheme.BrushPositive;
        }
    }
}
