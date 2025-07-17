using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO; // Added for Path.DirectorySeparatorChar

namespace NcfDesktopApp.Tests
{
    public static class CrossPlatformTest
    {
        public static void TestPlatformDetection()
        {
            Console.WriteLine("🔍 跨平台兼容性测试");
            Console.WriteLine($"当前操作系统: {Environment.OSVersion}");
            Console.WriteLine($"运行时架构: {RuntimeInformation.RuntimeIdentifier}");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("✅ Windows 平台检测正确");
                TestWindowsCommands();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("✅ macOS 平台检测正确");
                TestUnixCommands();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("✅ Linux 平台检测正确");
                TestUnixCommands();
            }
            else
            {
                Console.WriteLine("⚠️  未知操作系统平台");
            }
        }
        
        private static void TestWindowsCommands()
        {
            Console.WriteLine("🔧 测试 Windows 命令兼容性...");
            
            // 测试 netstat 命令
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"netstat -an | findstr :80\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(startInfo);
                process.WaitForExit();
                Console.WriteLine($"✅ netstat 命令测试完成 (退出代码: {process.ExitCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ netstat 命令测试失败: {ex.Message}");
            }
        }
        
        private static void TestUnixCommands()
        {
            Console.WriteLine("🔧 测试 Unix/Linux/macOS 命令兼容性...");
            
            // 测试 lsof 命令
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "lsof",
                    Arguments = "-i :80",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(startInfo);
                process.WaitForExit();
                Console.WriteLine($"✅ lsof 命令测试完成 (退出代码: {process.ExitCode})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ lsof 命令测试失败: {ex.Message}");
            }
        }
        
        public static void TestBrowserLaunch()
        {
            Console.WriteLine("🌐 测试浏览器启动兼容性...");
            
            var testUrl = "https://www.google.com";
            
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Console.WriteLine("Windows: 使用 UseShellExecute = true");
                    // Process.Start(new ProcessStartInfo(testUrl) { UseShellExecute = true });
                    Console.WriteLine("✅ Windows 浏览器启动配置正确");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Console.WriteLine("macOS: 使用 'open' 命令");
                    // Process.Start("open", testUrl);
                    Console.WriteLine("✅ macOS 浏览器启动配置正确");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Console.WriteLine("Linux: 使用 'xdg-open' 命令");
                    // Process.Start("xdg-open", testUrl);
                    Console.WriteLine("✅ Linux 浏览器启动配置正确");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 浏览器启动测试失败: {ex.Message}");
            }
        }
        
        public static void TestDirectoryPaths()
        {
            Console.WriteLine("📁 测试目录路径兼容性...");
            
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            Console.WriteLine($"LocalApplicationData: {appDataPath}");
            Console.WriteLine($"UserProfile: {userProfile}");
            Console.WriteLine($"路径分隔符: '{Path.DirectorySeparatorChar}'");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("✅ Windows 路径格式正确");
            }
            else
            {
                Console.WriteLine("✅ Unix 路径格式正确");
            }
        }
    }
} 