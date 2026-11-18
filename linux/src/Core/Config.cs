using System;
using System.Collections.Generic;
using System.IO;

namespace Tepi.Linux.Core
{
    public class Config
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "tepi");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.ini");

        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public Config()
        {
            Load();
        }

        public void Load()
        {
            if (!File.Exists(ConfigPath)) return;

            try
            {
                foreach (var line in File.ReadAllLines(ConfigPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                        continue;

                    int eq = trimmed.IndexOf('=');
                    if (eq > 0)
                    {
                        string k = trimmed.Substring(0, eq).Trim();
                        string v = trimmed.Substring(eq + 1).Trim();
                        _values[k] = v;
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var lines = new List<string>();
                foreach (var kvp in _values)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                File.WriteAllLines(ConfigPath, lines);
            }
            catch { }
        }

        public string Get(string key, string fallback = "") =>
            _values.TryGetValue(key, out var v) ? v : fallback;

        public bool GetBool(string key, bool fallback = false) =>
            _values.TryGetValue(key, out var v) ? (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase)) : fallback;

        public int GetInt(string key, int fallback = 0) =>
            _values.TryGetValue(key, out var v) && int.TryParse(v, out int i) ? i : fallback;

        public void Set(string key, string value)
        {
            _values[key] = value;
            Save();
        }

        public void SetBool(string key, bool value) => Set(key, value ? "1" : "0");
    }
}
