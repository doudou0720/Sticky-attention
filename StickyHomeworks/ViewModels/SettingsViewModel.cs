using CommunityToolkit.Mvvm.ComponentModel;

namespace StickyHomeworks.ViewModels;

public class SettingsViewModel : ObservableRecipient
{
    private int _appIconClickCount = 0;
    private object? _drawerContent;
    private bool _isPopupMenuOpened;
    private string _license = "";
    private string _subjectEditText = "";
    private int _subjectSelectedIndex = -1;
    private string _tagEditText = "";
    private int _tagSelectedIndex = -1;
    private string _debugRichTextBoxContent = "";
    
    // 服务器状态文本
    private string _grpcServiceStatusText = "未知";
    private string _grpcServiceStatusDetailText = "请重启应用程序以应用设置";

    public int AppIconClickCount
    {
        get => _appIconClickCount;
        set
        {
            if (value == _appIconClickCount) return;
            _appIconClickCount = value;
            OnPropertyChanged();
        }
    }

    public object? DrawerContent
    {
        get => _drawerContent;
        set
        {
            if (Equals(value, _drawerContent)) return;
            _drawerContent = value;
            OnPropertyChanged();
        }
    }

    public bool IsPopupMenuOpened
    {
        get => _isPopupMenuOpened;
        set
        {
            if (value == _isPopupMenuOpened) return;
            _isPopupMenuOpened = value;
            OnPropertyChanged();
        }
    }

    public string License
    {
        get => _license;
        set
        {
            if (value == _license) return;
            _license = value;
            OnPropertyChanged();
        }
    }

    public string SubjectEditText
    {
        get => _subjectEditText;
        set
        {
            if (value == _subjectEditText) return;
            _subjectEditText = value;
            OnPropertyChanged();
        }
    }

    public int SubjectSelectedIndex
    {
        get => _subjectSelectedIndex;
        set
        {
            if (value == _subjectSelectedIndex) return;
            _subjectSelectedIndex = value;
            OnPropertyChanged();
        }
    }

    public string TagEditText
    {
        get => _tagEditText;
        set
        {
            if (value == _tagEditText) return;
            _tagEditText = value;
            OnPropertyChanged();
        }
    }

    public int TagSelectedIndex
    {
        get => _tagSelectedIndex;
        set
        {
            if (value == _tagSelectedIndex) return;
            _tagSelectedIndex = value;
            OnPropertyChanged();
        }
    }

    public string DebugRichTextBoxContent
    {
        get => _debugRichTextBoxContent;
        set
        {
            if (value == _debugRichTextBoxContent) return;
            _debugRichTextBoxContent = value;
            OnPropertyChanged();
        }
    }
    
    // gRPC服务状态文本属性
    public string GrpcServiceStatusText
    {
        get => _grpcServiceStatusText;
        set
        {
            if (value == _grpcServiceStatusText) return;
            _grpcServiceStatusText = value;
            OnPropertyChanged();
        }
    }
    
    // gRPC服务状态详细文本属性
    public string GrpcServiceStatusDetailText
    {
        get => _grpcServiceStatusDetailText;
        set
        {
            if (value == _grpcServiceStatusDetailText) return;
            _grpcServiceStatusDetailText = value;
            OnPropertyChanged();
        }
    }

    public bool IsClosing { get; internal set; }
}