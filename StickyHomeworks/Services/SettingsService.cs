using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using StickyHomeworks;
using static StickyHomeworks.App;

namespace StickyHomeworks.Services;

public class SettingsService : ObservableRecipient, IHostedService
{
    private Settings _settings = new();
    private System.Timers.Timer? _saveTimer;
    private bool _restartRequired = false;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // 不保存设置，直接退出
        ExitWithoutSaving();
    }

    private void ScheduleSaveSettings()
    {
        _saveTimer?.Stop();
        _saveTimer = new System.Timers.Timer(500); // 延迟 500 毫秒
        _saveTimer.Elapsed += (sender, args) =>
        {
            SaveSettings();
            _saveTimer?.Dispose();
            _saveTimer = null;
            
            // 如果需要重启，提示用户
            if (_restartRequired)
            {
                _restartRequired = false;
                LogHelper.Info("设置已更改，某些功能需要重启应用程序才能生效。");
            }
        };
        _saveTimer.Start();
    }

    public SettingsService(IHostApplicationLifetime applicationLifetime)
    {
        PropertyChanged += OnPropertyChanged;
        Settings.PropertyChanged += (o, args) => OnSettingsChanged?.Invoke(o, args);
        LoadSettingsSafeAsync();
        OnSettingsChanged += OnOnSettingsChanged;
    }

    private void OnOnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 检查是否是需要重启的设置项
        if (e.PropertyName == nameof(Settings.HttpServerEnabled) || 
            e.PropertyName == nameof(Settings.GrpcEnabled) ||
            e.PropertyName == nameof(Settings.HttpServerPort) ||
            e.PropertyName == nameof(Settings.GrpcPort))
        {
            _restartRequired = true;
        }
        
        ScheduleSaveSettings();
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
                        Settings = settings;
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
            Settings.PropertyChanged += (o, args) => OnSettingsChanged?.Invoke(o, args);
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