using System;
using Avalonia;
using Tepi.Linux.Core;
using Tepi.Linux.UI;

namespace Tepi.Linux
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Logger.Init(args);

            if (Array.Exists(args, a => a == "-h" || a == "--help"))
            {
                Console.WriteLine("Tepi - Dynamic Island for Linux Debian ARM64");
                Console.WriteLine("Usage: tepi [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --debug     Enable verbose logging to console and ~/.local/share/tepi/tepi.log");
                Console.WriteLine("  --opaque    Force opaque window (mitigates black box bug in VNC without a compositor)");
                Console.WriteLine("  --help, -h  Show this help message");
                return;
            }

            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Logger.LogError("Fatal App Crash", ex);
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
