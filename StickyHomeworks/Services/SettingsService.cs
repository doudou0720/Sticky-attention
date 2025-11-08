using CommunityToolkit.Mvvm.ObservableObject;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using StickyHomeworks;
using static StickyHomeworks.App;
using System.Threading.Tasks;

namespace StickyHomeworks.Services;

public class SettingsService : ObservableRecipient, IHostedService
{
    private Settings _settings = new();
    private System.Timers.Timer? _saveTimer;
    private bool _restartRequired = false;
    private bool _isLoaded = false;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 不保存设置，直接退出
        ExitWithoutSaving();
    }

    public void ScheduleSaveSettings()
    {
        _saveTimer?.Stop();
        _saveTimer = new System.Timers.Timer(500); // 延迟 500 毫秒
        _saveTimer.Elapsed += SaveTimerOnElapsed;
        _saveTimer.Start();
    }

    private void SaveTimerOnElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        SaveSettings();
        _saveTimer?.Stop();
        _saveTimer?.Dispose();
        _saveTimer = null;

        // 如果需要重启，提示用户
        if (_restartRequired)
        {
            _restartRequired = false;
            LogHelper.Info("设置已更改，某些功能需要重启应用程序才能生效。");
        }
    }

    public SettingsService(IHostApplicationLifetime applicationLifetime)
    {
        PropertyChanged += OnPropertyChanged;
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        // 不再在构造函数中加载设置，改为按需加载
        OnSettingsChanged += OnOnSettingsChanged;
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnSettingsChanged?.Invoke(sender, e);
    }

    private void OnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 检查是否是需要重启的设置项
        if (e.PropertyName == nameof(Settings.GrpcEnabled) ||
            e.PropertyName == nameof(Settings.GrpcPort))
        {
            _restartRequired = true;
        }
        
        ScheduleSaveSettings();
    }

    // 异步加载设置，只加载一次
    public async Task EnsureSettingsLoadedAsync()
    {
        if (_isLoaded) return;

        await _loadSemaphore.WaitAsync();
        try
        {
            if (_isLoaded) return;

            await LoadSettingsSafeAsync();
            _isLoaded = true;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public async Task LoadSettingsSafeAsync()
    {
        // 确保.config目录存在
        string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        
        string settingsPath = Path.Combine(configDirectory, "Settings.json");
        if (!File.Exists(settingsPath))
        {
            // 如果文件不存在，创建默认的 Settings.json 文件
            var defaultSettings = new Settings(); // 假设 Settings 是你的设置类
            string json = JsonSerializer.Serialize(defaultSettings);

            try
            {
                await File.WriteAllTextAsync(settingsPath, json);
                LogHelper.Info("创建了默认的 Settings.json 文件");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"创建默认设置文件时出错: {ex.Message}");
               
            }
        }
        else
        {
            try
            {
                string json = await File.ReadAllTextAsync(settingsPath);
                Settings settings = JsonSerializer.Deserialize<Settings>(json);

                if (settings != null)
                {
                    lock (_lockObject)
                    {
                        // 移除旧的事件处理程序
                        _settings.PropertyChanged -= SettingsOnPropertyChanged;
                        
                        Settings = settings;
                        
                        // 为新Settings添加事件处理程序
                        Settings.PropertyChanged += SettingsOnPropertyChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载设置文件时出错: {ex.Message}");
                // 处理异常，比如使用默认设置或通知用户
            }
        }
    }

    private readonly object _lockObject = new object();

    public void SaveSettings()
    {
        // 确保.config目录存在
        string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        
        var filePath = Path.Combine(configDirectory, "Settings.json");
        var settings = Settings;

        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
            }
        }
        catch (IOException)
        {
            Thread.Sleep(1000);
        }

        try
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(settings));
        }
        catch (IOException ex)
        {
            LogHelper.Error("Error writing to file: " + ex.Message);
        }
    }

    public event PropertyChangedEventHandler? OnSettingsChanged;

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings))
        {
            // 移除旧的事件处理程序
            _settings.PropertyChanged -= SettingsOnPropertyChanged;
            
            Settings.PropertyChanged += SettingsOnPropertyChanged;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        SaveSettings();
        return Task.CompletedTask;
    }

    public async Task SaveSettingsAsync()
    {
        // 确保.config目录存在
        string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        
        var json = JsonSerializer.Serialize(Settings);
        var filePath = Path.Combine(configDirectory, "Settings.json");
        await File.WriteAllTextAsync(filePath, json);
    }

    public Settings Settings
    {
        get => _settings;
        set
        {
            if (Equals(value, _settings)) return;
            _settings = value;
            OnPropertyChanged();
        }
    }

    // 新增方法：直接退出而不保存
    public void ExitWithoutSaving()
    {
        // 直接退出程序，不保存任何内容
        Environment.Exit(0);
    }
}