using System;

namespace Tepi.Linux.Core
{
    public enum ViewMode
    {
        Idle,
        Media,
        Notif,
        Agent,
        Timer,
        Panel
    }

    public class MediaTrack
    {
        public string Title { get; set; } = "Tidak ada lagu";
        public string Artist { get; set; } = "Tepi Linux";
        public bool IsPlaying { get; set; }
        public double PositionSeconds { get; set; } = 42;
        public double DurationSeconds { get; set; } = 210;
        public string? ArtUrl { get; set; }
        public string LyricsPreview { get; set; } = "Menunggu pemutar musik...";
    }

    public class NotificationToast
    {
        public uint Id { get; set; }
        public string AppName { get; set; } = "Sistem";
        public string Title { get; set; } = "Pemberitahuan";
        public string Body { get; set; } = "Tepi aktif di Linux PRoot";
        public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;
    }

    public class AgentStatus
    {
        public string AgentName { get; set; } = "Antigravity";
        public string StatusText { get; set; } = "Menunggu perintah tugas";
        public bool IsActive { get; set; } = true;
        public bool NeedsApproval { get; set; } = false;
        public string ApprovalPrompt { get; set; } = "";
        public string RecentLogs { get; set; } = "[READY] Listening on /tmp/tepi-agent.sock\n[SESSION] Antigravity idle";
    }

    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = "";
        public string DueText { get; set; } = "Hari ini";
        public bool IsDone { get; set; }
    }

    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string App { get; set; } = "Pesan";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime Time { get; set; } = DateTime.Now;
    }
}
