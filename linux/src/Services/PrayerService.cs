using System;

namespace Tepi.Linux.Services
{
    public class PrayerService
    {
        // Simple offline prayer calculation (Subuh, Dzuhur, Ashar, Maghrib, Isya)
        public struct PrayerInfo
        {
            public string NextName;
            public TimeSpan Remaining;
            public string Formatted;
        }

        public struct ScheduleItem
        {
            public string Name;
            public TimeSpan Time;
            public string Icon;
            public bool IsNext;
        }

        private static readonly (string Name, TimeSpan Time, string Icon)[] DefaultSchedule =
        {
            ("Subuh", new TimeSpan(4, 30, 0), "🌅"),
            ("Dzuhur", new TimeSpan(11, 55, 0), "☀️"),
            ("Ashar", new TimeSpan(15, 15, 0), "⛅"),
            ("Maghrib", new TimeSpan(18, 5, 0), "🌇"),
            ("Isya", new TimeSpan(19, 15, 0), "🌙")
        };

        public PrayerInfo GetNextPrayer()
        {
            var now = DateTime.Now;
            var curTime = now.TimeOfDay;

            foreach (var (name, time, _) in DefaultSchedule)
            {
                if (time > curTime)
                {
                    var rem = time - curTime;
                    return new PrayerInfo
                    {
                        NextName = name,
                        Remaining = rem,
                        Formatted = $"{name} dalam {rem.Hours}j {rem.Minutes}m"
                    };
                }
            }

            // After Isya: Next is Subuh tomorrow
            var subuhTomorrow = DefaultSchedule[0].Time.Add(TimeSpan.FromHours(24));
            var remTomorrow = subuhTomorrow - curTime;
            return new PrayerInfo
            {
                NextName = "Subuh",
                Remaining = remTomorrow,
                Formatted = $"Subuh dalam {remTomorrow.Hours}j {remTomorrow.Minutes}m"
            };
        }

        public ScheduleItem[] GetDailySchedule()
        {
            var next = GetNextPrayer();
            var list = new ScheduleItem[DefaultSchedule.Length];

            for (int i = 0; i < DefaultSchedule.Length; i++)
            {
                list[i] = new ScheduleItem
                {
                    Name = DefaultSchedule[i].Name,
                    Time = DefaultSchedule[i].Time,
                    Icon = DefaultSchedule[i].Icon,
                    IsNext = DefaultSchedule[i].Name.Equals(next.NextName, StringComparison.OrdinalIgnoreCase)
                };
            }
            return list;
        }
    }
}
