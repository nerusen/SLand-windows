using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Tepi.Linux.Core;

namespace Tepi.Linux.Services
{
    public class TaskManager
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "tepi");
        private static readonly string TasksFilePath = Path.Combine(ConfigDir, "tasks.json");

        private readonly List<TaskItem> _tasks = new();
        private readonly object _lock = new();

        public event Action? TasksChanged;

        public TaskManager()
        {
            Load();
        }

        public IReadOnlyList<TaskItem> Tasks
        {
            get
            {
                lock (_lock) return _tasks.ToArray();
            }
        }

        public void Load()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(TasksFilePath))
                    {
                        string json = File.ReadAllText(TasksFilePath);
                        var loaded = JsonSerializer.Deserialize<List<TaskItem>>(json);
                        if (loaded != null)
                        {
                            _tasks.Clear();
                            _tasks.AddRange(loaded);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("TaskManager.Load", ex);
                }

                // Default initial seed tasks
                if (_tasks.Count == 0)
                {
                    _tasks.Add(new TaskItem { Title = "Putar musik favorit di Linux", DueText = "Hari ini", IsDone = false });
                    _tasks.Add(new TaskItem { Title = "Hubungkan Claude / Antigravity CLI", DueText = "Besok", IsDone = false });
                    _tasks.Add(new TaskItem { Title = "Review laporan kuliah Kulia.live", DueText = "20:00", IsDone = true });
                    Save();
                }
            }
        }

        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    Directory.CreateDirectory(ConfigDir);
                    string json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(TasksFilePath, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError("TaskManager.Save", ex);
                }
            }
            TasksChanged?.Invoke();
        }

        public void Add(string title, string due = "Hari ini")
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            lock (_lock)
            {
                _tasks.Insert(0, new TaskItem { Title = title.Trim(), DueText = due, IsDone = false });
            }
            Save();
        }

        public void Toggle(string id)
        {
            lock (_lock)
            {
                var item = _tasks.Find(t => t.Id == id);
                if (item != null)
                {
                    item.IsDone = !item.IsDone;
                }
            }
            Save();
        }

        public void Delete(string id)
        {
            lock (_lock)
            {
                _tasks.RemoveAll(t => t.Id == id);
            }
            Save();
        }
    }
}
