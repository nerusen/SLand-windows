using System;
using System.Collections.Generic;
using Tepi.Linux.Core;

namespace Tepi.Linux.Services
{
    public class NotificationStore
    {
        private readonly List<NotificationItem> _items = new();
        private readonly object _lock = new();

        public event Action? NotificationsChanged;

        public NotificationStore()
        {
            // Seed initial notifications
            _items.Add(new NotificationItem
            {
                App = "Tepi Linux",
                Title = "Selamat Datang!",
                Body = "Tepi Dynamic Island aktif di Debian PRoot ARM64.",
                Time = DateTime.Now.AddMinutes(-5)
            });
            _items.Add(new NotificationItem
            {
                App = "WhatsApp",
                Title = "Budi Santoso",
                Body = "Proyek porting sudah sampai tahap apa mas?",
                Time = DateTime.Now.AddMinutes(-12)
            });
        }

        public IReadOnlyList<NotificationItem> Items
        {
            get
            {
                lock (_lock) return _items.ToArray();
            }
        }

        public void Add(string app, string title, string body)
        {
            lock (_lock)
            {
                _items.Insert(0, new NotificationItem
                {
                    App = app,
                    Title = title,
                    Body = body,
                    Time = DateTime.Now
                });
            }
            NotificationsChanged?.Invoke();
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                _items.Clear();
            }
            NotificationsChanged?.Invoke();
        }
    }
}
