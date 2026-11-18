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
        public double PositionSeconds { get; set; }
        public double DurationSeconds { get; set; }
        public string? ArtUrl { get; set; }
    }

    public class NotificationToast
    {
        public uint Id { get; set; }
        public string AppName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime ReceivedUtc { get; set; } = DateTime.UtcNow;
    }

    public class AgentStatus
    {
        public string AgentName { get; set; } = "Antigravity";
        public string StatusText { get; set; } = "Menunggu perintah";
        public bool IsActive { get; set; }
        public bool NeedsApproval { get; set; }
        public string ApprovalPrompt { get; set; } = "";
    }

    public class TimerState
    {
        public string Label { get; set; } = "Fokus";
        public int RemainingSeconds { get; set; }
        public bool IsRunning { get; set; }
    }
}
