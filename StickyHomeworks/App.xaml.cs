using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Interfaces;
using ElysiaFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Core.Context;
using StickyHomeworks.Models;
using StickyHomeworks.Services;
using StickyHomeworks.Views;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Sentry;
using System.Drawing;
using System.Windows.Forms;
using static StickyHomeworks.Controls.HomeworkControl;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace StickyHomeworks;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : AppEx
{
    private static Mutex? Mutex;
    
    private Process? _webServiceProcess;

    private NotifyIcon _notifyIcon;

    private MainWindow MainWindow;
    private ToolStripMenuItem showMainWindowItem; // 定义菜单项

    private System.Timers.Timer _memoryUsageTimer;

    public static string AppVersion => Assembly.GetExecutingAssembly().GetName().Version!.ToString();

    // 使用完整版本号，包括预发布标签
    public static string FullAppVersion { get; set; } = "v" + Assembly.GetExecutingAssembly().GetName().Version!.ToString(); // 默认使用程序集版本
    
    // 静态构造函数，在应用程序启动时设置完整版本号
    static App()
    {
        // 从程序集信息中获取完整版本号
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        
        // 如果有自定义属性包含完整版本号，则使用它
        var customAttributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
        if (customAttributes.Length > 0)
        {
            FullAppVersion = ((AssemblyInformationalVersionAttribute)customAttributes[0]).InformationalVersion;
        }
        else
        {
            FullAppVersion = "v" + version.ToString();
        }
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        SentrySdk.Init(o =>
        {
            // Tells which project in Sentry to send events to:
            o.Dsn = "https://48c6921f7cf22181e09e16345b65fd77@o4508114075713536.ingest.us.sentry.io/4508114077417472";
            // When configuring for the first time, to see what the SDK is doing:
#if DEBUG
            o.Debug = true;
#else
            o.Debug = false;
#endif
            // Set TracesSampleRate to 1.0 to capture 100% of transactions for tracing.
            // We recommend adjusting this value in production.
            o.TracesSampleRate = 1.0;
            // Set the release version of the application
            o.Release = App.AppVersion;
            // Enable auto session tracking
            o.AutoSessionTracking = true; // default: false
        });

        // 根据调试或发布模式设置控制台窗口的显示状态
#if DEBUG
        AllocConsole();
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            ShowWindow(consoleWindow, SW_SHOW);
        }
#else
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow != IntPtr.Zero)
        {
            ShowWindow(consoleWindow, SW_HIDE);
        }
#endif
    }

    public static class LogHelper
    {
        // 颜色定义
        private static readonly ConsoleColor[] Colors = new[]
        {
            ConsoleColor.Red,
            ConsoleColor.Green,
            ConsoleColor.Yellow,
            ConsoleColor.Cyan,
            ConsoleColor.Magenta,
            ConsoleColor.Blue,
            ConsoleColor.White
        };

        // 调试日志
        public static void Debug(string message, params object[] args)
        {
#if DEBUG
            WriteColoredLog("Debug", ConsoleColor.Cyan, message, args);
#endif
        }

        // 信息日志
        public static void Info(string message, params object[] args)
        {
            WriteColoredLog("Info", ConsoleColor.Green, message, args);
        }

        // 警告日志
        public static void Warning(string message, params object[] args)
        {
            WriteColoredLog("Warning", ConsoleColor.Yellow, message, args);
        }

        // 错误日志
        public static void Error(string message, params object[] args)
        {
            WriteColoredLog("Error", ConsoleColor.Red, message, args);
        }

        public static void Error(Exception exception)
        {
            WriteColoredLog("Error", ConsoleColor.Red, exception.ToString());
        }

        private static void WriteColoredLog(string level, ConsoleColor color, string message, params object[] args)
        {
            try
            {
                // 获取时间
                DateTime now = DateTime.Now;

                // 格式化
                string formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                string logEntry = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {formattedMessage}";

                // 设置颜色并输出
                ConsoleColor originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(logEntry);
                Console.ForegroundColor = originalColor;
            }
            catch (IOException ex)
            {
                // 可以选择将错误记录到文件或其他存储
                using (StreamWriter writer = new StreamWriter("error_log.txt", true))
                {
                    writer.WriteLine($"Error writing to console: {ex.Message}");
                }
            }
        }

        public static void PrintWelcomeMessage()
        {
            string welcomeMessage = " \r\n \r\n ____  _ _      _ _   _ _   _ _             \r\n / ___ || | _(_) ___ | | ___   _ __ _ | | _ | | _ ___ _ __ | | _(_) ___ _ __  \r\n \\___ \\| __ | |/ __ | |/ / | | | | _____ / _` | __ | __ / _ \\ '_ \\| __| |/ _ \\| '_ \\ \r\n ___) | | _ | | (__ |   <| | _ | | _____ | (_ | | | _ | || __ / | | | | _ | | (_) | | | |\r\n | ____ / \\__ | _ |\\___ | _ |\\_\\\\__, |      \\__,_ |\\__ |\\__\\___ | _ | | _ |\\__ | _ |\\___ /| _ | | _ |\r\n | ___ /                                                 \r\n";
            for (int i = 0; i < welcomeMessage.Length; i++)
            {
                Console.ForegroundColor = Colors[i % Colors.Length];
                Console.Write(welcomeMessage[i]);
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        // 打印内存使用情况
        public static void PrintMemoryUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            long memoryUsage = currentProcess.PrivateMemorySize64;
            WriteColoredLog("Memory", ConsoleColor.Blue, $"Memory usage: {memoryUsage / 1024 / 1024} MB");
        }
    }

    void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        SentrySdk.CaptureException(e.Exception);
        LogHelper.Error(e.Exception);

        // 防止应用程序崩溃
        e.Handled = true;

        // 显示错误窗口
        var cw = GetService<CrashWindow>();
        cw.CrashInfo = e.Exception.ToString();
        cw.Exception = e.Exception;
        cw.OpenWindow();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        LogHelper.PrintWelcomeMessage();

        StartMemoryMonitoring();

        LogHelper.Info("Application started");

        Mutex = new Mutex(true, "StickyHomeworks.Lock", out var createNew);
        base.OnStartup(e);
        AppCenter.Start("51d345d3-d94e-4398-8e9a-927d22c5849a", typeof(Analytics), typeof(Crashes));
        if (!createNew)
        {
            MessageBox.Show("应用已经在运行中，请勿重复启动第二个实例。");
            Environment.Exit(0);
        }

        // 检查并清理过期作业
        CheckAndCleanupExpiredHomeworks();

        base.OnStartup(e);

        // 异步启动Web服务进程，避免阻塞主线程
        _ = Task.Run(() => StartWebService());

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddDbContext<AppDbContext>();
                services.AddSingleton<IThemeService, ThemeService>();
                services.AddSingleton<ProfileService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<SettingsWindow>();
                services.AddSingleton<WallpaperPickingService>();
                services.AddHostedService<ThemeBackgroundService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<HomeworkEditWindow>();
                services.AddSingleton<CrashWindow>();
                services.AddSingleton<WindowFocusObserverService>();
            }).
            Build();
        _ = Host.StartAsync();
        GetService<AppDbContext>();
        MainWindow = GetService<MainWindow>();
        
        // 等待关键服务初始化完成后再显示主窗口
        await InitializeCriticalServices();
        
        GetService<MainWindow>().Show();
        base.OnStartup(e);

        LogHelper.Info("Application started");

        // 创建托盘图标
        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "AppLogo.ico")),
            Text = "StickyHomeworks",
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };
    }

    /// <summary>
    /// 检查并清理过期作业
    /// </summary>
    private void CheckAndCleanupExpiredHomeworks()
    {
        try
        {
            // 获取配置文件路径
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
            string profilePath = Path.Combine(configPath, "Profile.json");
            
            // 检查配置文件是否存在
            if (!File.Exists(profilePath))
                return;
                
            // 读取配置文件
            string jsonContent = File.ReadAllText(profilePath);
            
            // 解析JSON内容
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var profile = JsonSerializer.Deserialize<Profile>(jsonContent, options);
            
            if (profile != null && profile.Homeworks != null)
            {
                // 获取当前时间
                DateTime now = DateTime.Now;
                
                // 过滤掉过期的作业
                int removedCount = profile.Homeworks.RemoveAll(h => h.DueTime < now);
                
                if (removedCount > 0)
                {
                    LogHelper.Info($"已清理 {removedCount} 个过期作业");
                    
                    // 保存更新后的配置文件
                    var saveOptions = new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    };
                    string updatedJson = JsonSerializer.Serialize(profile, saveOptions);
                    File.WriteAllText(profilePath, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"清理过期作业时出错: {ex.Message}");
        }
    }

    // 用于反序列化的数据结构
    public class Profile
    {
        public List<Homework> Homeworks { get; set; } = new();
    }
    
    public class Homework
    {
        public DateTime DueTime { get; set; }
    }

    // 初始化关键服务
    private async Task InitializeCriticalServices()
    {
        var settingsService = GetService<SettingsService>();
        var profileService = GetService<ProfileService>();
        
        // 并行加载设置和档案
        await Task.WhenAll(
            settingsService.EnsureSettingsLoadedAsync(),
            profileService.EnsureProfileLoadedAsync()
        );
    }

    private void StartWebService()
    {
        try
        {
            var settingsService = GetService<SettingsService>();
            var settings = settingsService.Settings;

            if (settings.GrpcEnabled)
            {
                // 修改为正确的Web服务可执行文件名
                var webServicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StickyHomeworks.Web.exe");
                if (File.Exists(webServicePath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = webServicePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    };

                    // 添加配置参数
                    if (!settings.GrpcEnabled)
                        startInfo.ArgumentList.Add("--EnableGrpcService=false");
                    
                    if (settings.GrpcPort != 5001)
                        startInfo.ArgumentList.Add($"--GrpcPort={settings.GrpcPort}");

                    _webServiceProcess = Process.Start(startInfo);
                    LogHelper.Info("Web service started successfully");
                }
                else
                {
                    // 检查是否有带版本号的Web服务可执行文件
                    var webServiceFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "StickyHomeworks.Web*.exe");
                    if (webServiceFiles.Length > 0)
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = webServiceFiles[0], // 使用找到的第一个匹配文件
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                        };

                        // 添加配置参数
                        if (!settings.GrpcEnabled)
                            startInfo.ArgumentList.Add("--EnableGrpcService=false");
                        
                        if (settings.GrpcPort != 5001)
                            startInfo.ArgumentList.Add($"--GrpcPort={settings.GrpcPort}");

                        _webServiceProcess = Process.Start(startInfo);
                        LogHelper.Info("Web service started successfully");
                    }
                    else
                    {
                        LogHelper.Error("Web service executable not found: " + webServicePath);
                    }
                }
            }
            else
            {
                LogHelper.Info("gRPC service is disabled, skipping web service startup");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("Failed to start web service: " + ex.Message);
        }
    }

    // 创建托盘右键菜单
    private ContextMenuStrip CreateContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        // 添加"显示"菜单项，并为其单独绑定事件处理函数
        var showItem = new ToolStripMenuItem("隐藏或显示界面");
        showItem.Click += ShowItem_Click;
        contextMenu.Items.Add(showItem);

        contextMenu.Items.Add("设置", null, Appsettings);
        contextMenu.Items.Add("退出", null, ExitApplication);

        return contextMenu;
    }

    private void ShowItem_Click(object sender, EventArgs e)
    {
        var menuItem = sender as ToolStripMenuItem;
        MainWindow.ToggleWindowExpansion();
    }

    private void Appsettings(object sender, EventArgs e)
    {
        var win = AppEx.GetService<SettingsWindow>();
        if (!win.IsOpened)
        {
            win.IsOpened = true;
            win.Show();
        }
        else
        {
            if (win.WindowState == WindowState.Minimized)
            {
                win.WindowState = WindowState.Normal;
            }
            win.Activate();
        }
    }

    // 退出逻辑
    private void ExitApplication(object sender, EventArgs e)
    {
        Current.Shutdown();
    }

    private void StartMemoryMonitoring()
    {
        _memoryUsageTimer = new System.Timers.Timer(5000);
        _memoryUsageTimer.Elapsed += (sender, e) => LogHelper.PrintMemoryUsage();
        _memoryUsageTimer.AutoReset = true;
        _memoryUsageTimer.Enabled = true;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 关闭Web服务进程
        if (_webServiceProcess != null && !_webServiceProcess.HasExited)
        {
            try
            {
                _webServiceProcess.Kill();
                _webServiceProcess.WaitForExit(3000); // 等待最多3秒
                _webServiceProcess.Dispose();
                LogHelper.Info("Web service stopped");
            }
            catch (Exception ex)
            {
                LogHelper.Error("Error stopping web service: " + ex.Message);
            }
        }

        base.OnExit(e);
        _memoryUsageTimer?.Stop();
        _memoryUsageTimer?.Dispose();
        LogHelper.Info("Application exited");
#if DEBUG
        FreeConsole();
#endif
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        LogHelper.Error(e.Exception);
        var cw = GetService<CrashWindow>();
        cw.CrashInfo = e.Exception.ToString();
        cw.Exception = e.Exception;
        cw.OpenWindow();
    }

    public static void ReleaseLock()
    {
        Mutex?.ReleaseMutex();
    }
}