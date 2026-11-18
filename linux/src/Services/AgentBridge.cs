using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tepi.Linux.Core;

namespace Tepi.Linux.Services
{
    public class AgentBridge
    {
        public event Action<AgentStatus>? StatusChanged;
        public AgentStatus CurrentStatus { get; private set; } = new AgentStatus();

        private const string SocketPath = "/tmp/tepi-agent.sock";
        private Socket? _server;
        private CancellationTokenSource? _cts;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _server?.Close(); } catch { }
            if (File.Exists(SocketPath)) File.Delete(SocketPath);
            Logger.Log("AgentBridge stopped and socket cleaned up.");
        }

        public void Approve()
        {
            Logger.Log($"AgentBridge: Approved action for {CurrentStatus.AgentName}");
            CurrentStatus.NeedsApproval = false;
            CurrentStatus.StatusText = "Izin diberikan";
            StatusChanged?.Invoke(CurrentStatus);
        }

        public void Reject()
        {
            Logger.Log($"AgentBridge: Rejected action for {CurrentStatus.AgentName}");
            CurrentStatus.NeedsApproval = false;
            CurrentStatus.StatusText = "Izin ditolak";
            StatusChanged?.Invoke(CurrentStatus);
        }

        private async Task ListenLoop(CancellationToken token)
        {
            try
            {
                if (File.Exists(SocketPath)) File.Delete(SocketPath);

                var endPoint = new UnixDomainSocketEndPoint(SocketPath);
                _server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _server.Bind(endPoint);
                _server.Listen(5);
                Logger.Log($"AgentBridge: Listening on Unix socket {SocketPath}");

                while (!token.IsCancellationRequested)
                {
                    var client = await _server.AcceptAsync();
                    _ = HandleClientAsync(client, token);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AgentBridge.ListenLoop", ex);
            }
        }

        private async Task HandleClientAsync(Socket client, CancellationToken token)
        {
            try
            {
                using (client)
                {
                    var buffer = new byte[2048];
                    int received = await client.ReceiveAsync(buffer, SocketFlags.None);
                    if (received > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, received).Trim();
                        Logger.Log($"AgentBridge received: {message}");
                        // Format: AGENT:NAME:STATUS:PROMPT
                        if (message.StartsWith("AGENT:", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = message.Split(':', 4);
                            if (parts.Length >= 3)
                            {
                                CurrentStatus.AgentName = parts[1];
                                CurrentStatus.StatusText = parts[2];
                                CurrentStatus.IsActive = true;
                                if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                                {
                                    CurrentStatus.NeedsApproval = true;
                                    CurrentStatus.ApprovalPrompt = parts[3];
                                }
                                else
                                {
                                    CurrentStatus.NeedsApproval = false;
                                }
                                StatusChanged?.Invoke(CurrentStatus);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AgentBridge.HandleClient", ex);
            }
        }
    }
}
