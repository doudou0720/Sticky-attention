using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StickyHomeworks.Models;

public class Homework : ObservableRecipient
{
    private string _content = "";
    private string _subject = "";
    private DateTime _dueTime = DateTime.Today;
    private ObservableCollection<string> _tags = new();
    private bool _isNearExpiration = false;
    private bool _isExpired = false;

    public string Content
    {
        get => _content;
        set
        {
            if (value == _content) return;
            _content = value;
            OnPropertyChanged();
        }
    }

    public string Subject
    {
        get => _subject;
        set
        {
            if (value == _subject) return;
            _subject = value;
            OnPropertyChanged();
        }
    }

    public DateTime DueTime
    {
        get => _dueTime;
        set
        {
            if (value.Equals(_dueTime)) return;
            _dueTime = value;
            OnPropertyChanged();
            // 当截止时间改变时，更新状态
            UpdateExpirationStatus();
        }
    }

    public ObservableCollection<string> Tags
    {
        get => _tags;
        set
        {
            if (Equals(value, _tags)) return;
            _tags = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 获取或设置作业是否临近过期（过期前20分钟内）
    /// </summary>
    public bool IsNearExpiration
    {
        get => _isNearExpiration;
        set
        {
            if (value == _isNearExpiration) return;
            _isNearExpiration = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 获取或设置作业是否已过期
    /// </summary>
    public bool IsExpired
    {
        get => _isExpired;
        set
        {
            if (value == _isExpired) return;
            _isExpired = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// 更新作业的过期状态
    /// </summary>
    public void UpdateExpirationStatus()
    {
        var now = DateTime.Now;
        IsExpired = DueTime <= now;
        IsNearExpiration = DueTime > now && DueTime <= now.AddMinutes(20);
    }

    public Homework()
    {
        // 监听属性变化事件，以便在时间变化时更新状态
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(DueTime))
            {
                UpdateExpirationStatus();
            }
        };
    }
}

// 用于导出的包装类，包含版本信息
// TODO: 如果修改了此结构，请同步更新 /standards 文档
public class ExportData
{
    public int Version { get; set; } = 0;
    public string Description { get; set; } = "StickyHomeworks数据导出文件";
    public DateTime ExportDate { get; set; } = DateTime.Now;
    public List<Homework> Homeworks { get; set; } = new();
}