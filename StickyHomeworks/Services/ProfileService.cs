using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace StickyHomeworks.Services;

public class ProfileService : IHostedService, INotifyPropertyChanged
{
    private Profile _profile = new();
    private bool _isLoaded = false;
    private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

    public event EventHandler? ProfileSaved;

    public ProfileService(IHostApplicationLifetime applicationLifetime)
    {
        // 不再在构造函数中立即加载配置文件
        //LoadProfile();
        //CleanupOutdated();
        //applicationLifetime.ApplicationStopping.Register(SaveProfile);
        Profile.PropertyChanged += ProfileOnPropertyChanged;
    }

    // 用于处理Profile属性变化事件的方法
    private void ProfileOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveProfile();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
    }

    // 异步加载档案，只加载一次
    public async Task EnsureProfileLoadedAsync()
    {
        if (_isLoaded) return;

        await _loadSemaphore.WaitAsync();
        try
        {
            if (_isLoaded) return;

            LoadProfile();
            _isLoaded = true;
        }
        finally
        {
            _loadSemaphore.Release();
        }
    }

    public void LoadProfile()
    {
        // 确保.config目录存在
        string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        
        string profilePath = Path.Combine(configDirectory, "Profile.json");
        if (!File.Exists(profilePath))
        {
            return;
        }
        var json = File.ReadAllText(profilePath);
        var r = JsonSerializer.Deserialize<Profile>(json);
        if (r != null)
        {
            // 处理作业截止时间，为没有详细到秒的作业设置当天的23:59:59
            foreach (var homework in r.Homeworks)
            {
                // 如果截止时间是当天的00:00:00，说明没有详细到秒，设置为当天的23:59:59
                if (homework.DueTime.TimeOfDay == TimeSpan.Zero)
                {
                    homework.DueTime = homework.DueTime.Date.Add(new TimeSpan(23, 59, 59));
                }
            }
            
            // 先移除旧的事件处理程序（如果有的话）
            _profile.PropertyChanged -= ProfileOnPropertyChanged;
            
            Profile = r;
            // 为新Profile添加事件处理程序
            Profile.PropertyChanged += ProfileOnPropertyChanged;
        }
    }

    public List<Homework> CleanupOutdated()
    {
        // 收集过期的作业
        var expiredHomeworks = new List<Homework>();
        var now = DateTime.Now;
        
        foreach (var homework in Profile.Homeworks.ToList()) // 使用ToList()避免在迭代时修改集合
        {
            // 更新作业的过期状态
            homework.UpdateExpirationStatus();
            
            // 如果作业已过期，添加到过期作业列表
            if (homework.DueTime <= now)
            {
                expiredHomeworks.Add(homework);
            }
        }
        
        return expiredHomeworks;
    }

    public void SaveProfile()
    {
        // 确保.config目录存在
        string configDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".config");
        if (!Directory.Exists(configDirectory))
        {
            Directory.CreateDirectory(configDirectory);
        }
        
        string profilePath = Path.Combine(configDirectory, "Profile.json");
        File.WriteAllText(profilePath, JsonSerializer.Serialize<Profile>(Profile));
        ProfileSaved?.Invoke(this, EventArgs.Empty);
    }

    public Profile Profile
    {
        get => _profile;
        set
        {
            if (Equals(value, _profile)) return;
            _profile = value;
            OnPropertyChanged();
        }
    }


    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}