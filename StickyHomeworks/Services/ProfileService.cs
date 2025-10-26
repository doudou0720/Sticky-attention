using Microsoft.Extensions.Hosting;
using StickyHomeworks.Models;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace StickyHomeworks.Services;

public class ProfileService : IHostedService, INotifyPropertyChanged
{
    private Profile _profile = new();

    public event EventHandler? ProfileSaved;

    public ProfileService(IHostApplicationLifetime applicationLifetime)
    {
        LoadProfile();
        //CleanupOutdated();
        //applicationLifetime.ApplicationStopping.Register(SaveProfile);
        Profile.PropertyChanged += (sender, args) => SaveProfile();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
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
            Profile = r;
            Profile.PropertyChanged += (sender, args) => SaveProfile();
        }
    }

    public List<Homework> CleanupOutdated()
    {
        var rm = Profile.Homeworks.Where(i => i.DueTime.Date < DateTime.Today.Date).ToList();
        foreach (var i in rm)
        {
            Profile.Homeworks.Remove(i);
        }
        return rm;
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