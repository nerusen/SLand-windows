using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Tepi.Linux.Core;

namespace Tepi.Linux.Services
{
    public class MprisService
    {
        public event Action<MediaTrack>? TrackChanged;
        public MediaTrack CurrentTrack { get; private set; } = new MediaTrack();

        public MprisService()
        {
            Logger.Log("MprisService initialized.");
            PollMetadata();
        }

        public void PlayPause()
        {
            Logger.Log("MprisService: PlayPause triggered");
            RunPlayerctl("play-pause");
            CurrentTrack.IsPlaying = !CurrentTrack.IsPlaying;
            TrackChanged?.Invoke(CurrentTrack);
        }

        public void Next()
        {
            Logger.Log("MprisService: Next triggered");
            RunPlayerctl("next");
            Task.Delay(300).ContinueWith(_ => PollMetadata());
        }

        public void Previous()
        {
            Logger.Log("MprisService: Previous triggered");
            RunPlayerctl("previous");
            Task.Delay(300).ContinueWith(_ => PollMetadata());
        }

        public void PollMetadata()
        {
            try
            {
                string status = RunPlayerctl("status")?.Trim() ?? "";
                if (string.IsNullOrEmpty(status) || status.Contains("No players found", StringComparison.OrdinalIgnoreCase))
                {
                    if (CurrentTrack.IsPlaying)
                    {
                        CurrentTrack.IsPlaying = false;
                        TrackChanged?.Invoke(CurrentTrack);
                    }
                    return;
                }

                bool isPlaying = status.Equals("Playing", StringComparison.OrdinalIgnoreCase);
                string title = RunPlayerctl("metadata title")?.Trim() ?? "";
                string artist = RunPlayerctl("metadata artist")?.Trim() ?? "";

                if (CurrentTrack.IsPlaying != isPlaying || CurrentTrack.Title != title)
                {
                    CurrentTrack.IsPlaying = isPlaying;
                    CurrentTrack.Title = string.IsNullOrEmpty(title) ? "Tidak ada lagu" : title;
                    CurrentTrack.Artist = string.IsNullOrEmpty(artist) ? "Unknown Artist" : artist;
                    Logger.Log($"MprisService: Track updated -> {CurrentTrack.Title} by {CurrentTrack.Artist} (Playing={isPlaying})");
                    TrackChanged?.Invoke(CurrentTrack);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("MprisService.PollMetadata", ex);
            }
        }

        private static string? RunPlayerctl(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "playerctl",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(1000);
                return output;
            }
            catch (Exception ex)
            {
                Logger.LogError($"RunPlayerctl({args})", ex);
                return null;
            }
        }
    }
}
