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

        public PrayerInfo GetNextPrayer()
        {
            var now = DateTime.Now;
            // Standard approximate timetable for default coordinates (can be adjusted via settings)
            var times = new (string Name, TimeSpan Time)[]
            {
                ("Subuh", new TimeSpan(4, 30, 0)),
                ("Dzuhur", new TimeSpan(11, 55, 0)),
                ("Ashar", new TimeSpan(15, 15, 0)),
                ("Maghrib", new TimeSpan(18, 05, 0)),
                ("Isya", new TimeSpan(19, 15, 0))
            };

            var curTime = now.TimeOfDay;
            foreach (var (name, time) in times)
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
            var subuhTomorrow = times[0].Time.Add(TimeSpan.FromHours(24));
            var remTomorrow = subuhTomorrow - curTime;
            return new PrayerInfo
            {
                NextName = "Subuh",
                Remaining = remTomorrow,
                Formatted = $"Subuh dalam {remTomorrow.Hours}j {remTomorrow.Minutes}m"
            };
        }
    }
}
