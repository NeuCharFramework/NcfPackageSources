using Avalonia;
using System;
using System.Runtime.InteropServices;
using Avalonia.WebView.Desktop;

namespace NcfDesktopApp.GUI;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // 设置控制台编码为UTF-8，以正确显示emoji和中文
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Console.WriteLine("========================================");
            Console.WriteLine("🚀 NCF Desktop App Starting...");
            Console.WriteLine("========================================");
            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"Architecture: {RuntimeInformation.ProcessArchitecture}");
            Console.WriteLine($".NET Version: {RuntimeInformation.FrameworkDescription}");
            Console.WriteLine("\n🏗️ Building Avalonia App...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n❌ FATAL ERROR:");
            Console.WriteLine($"   Type: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"\n📋 Stack Trace:");
            Console.WriteLine(ex.StackTrace);
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"\n🔗 Inner Exception:");
                Console.WriteLine($"   Type: {ex.InnerException.GetType().Name}");
                Console.WriteLine($"   Message: {ex.InnerException.Message}");
            }
            
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseDesktopWebView();
}
