using ElysiaFramework;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static StickyHomeworks.App;
using Image = System.Windows.Controls.Image;           // 为 Image 指定默认命名空间
using RichTextBox = System.Windows.Controls.RichTextBox; // 为 RichTextBox 指定默认命名空间


namespace StickyHomeworks.Views;

/// <summary>
/// HomeworkEditWindow.xaml 的交互逻辑
/// </summary>
public partial class HomeworkEditWindow : Window, INotifyPropertyChanged
{
    private RichTextBox _relatedRichTextBox = new();
    private DateTime _lastClickTime = DateTime.MinValue;
    private Point _lastClickPosition = new Point(0, 0);
    
    // 触摸选择相关字段
    private bool _isTouchSelecting = false;
    private Point _touchStartPoint;
    
    // 鼠标选择相关字段
    private bool _isSelecting = false;
    private Point _startPoint;
    
    public MainWindow MainWindow { get; }
    public SettingsService SettingsService { get; }
    public ICommand AddImageCommand { get; }


    public HomeworkEditViewModel ViewModel { get; } = new();

    public bool IsOpened { get; set; } = false;

    public event EventHandler? EditingFinished;

    public event EventHandler? SubjectChanged;

    public void TryOpen()
    {
        if (IsOpened)
            return;
        Show();
        Activate();
        IsOpened = true;
    }

    public void TryClose()
    {
        if (!IsOpened)
            return;
        IsOpened = false;
        Hide();
    }


    public HomeworkEditWindow(MainWindow mainWindow, SettingsService settingsService)
    {
        MainWindow = mainWindow;
        SettingsService = settingsService;
        AddImageCommand = new RelayCommand(AddImageToRichTextBox);
        
        // 使用FontService设置窗口字体
        FontFamily = FontService.DefaultFontFamily;
        
        InitializeComponent();
    }

    private void AddImageToRichTextBox()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择图片",
            Filter = "图片文件 (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;

            // 加载图片
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(filePath);
            bitmapImage.EndInit();

            // 创建 Image 元素
            Image image = new Image
            {
                Source = bitmapImage,
                Width = 100, // 可以根据需要调整宽度
                Height = 100 // 可以根据需要调整高度
            };

            // 创建 InlineUIContainer 并将 Image 添加到容器
            InlineUIContainer imageContainer = new InlineUIContainer(image);

            // 创建新的段落并添加 InlineUIContainer
            Paragraph paragraph = new Paragraph(imageContainer);

            // 将段落添加到 RichTextBox 的文档中
            RelatedRichTextBox.Document.Blocks.Add(paragraph);
        }
    }

    private void AddImageButton_Click(object sender, RoutedEventArgs e)
    {
        AddImageToRichTextBox();
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }


    private void HomeworkEditWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CenterWindowOnScreen();
    }



    public RichTextBox RelatedRichTextBox
    {
        get => _relatedRichTextBox;
        set
        {
            UnregisterOldTextBox(_relatedRichTextBox);
            RegisterNewTextBox(value);
            _relatedRichTextBox = value;
            OnPropertyChanged();
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        ViewModel.FontFamilies =
            new ObservableCollection<FontFamily>(from i in Fonts.SystemFontFamilies orderby i.ToString() select i)
                { 
                    FontService.DefaultFontFamily
                };
        base.OnInitialized(e);
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
        {
            return;
        }
        var s = RelatedRichTextBox.Selection;
        switch (e.PropertyName)
        {
            case nameof(ViewModel.TextColor):
                s.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(ViewModel.TextColor));
                break;
            case nameof(ViewModel.Font):
                s.ApplyPropertyValue(TextElement.FontFamilyProperty, ViewModel.Font);
                break;
            case nameof(ViewModel.FontSize):
                s.ApplyPropertyValue(TextElement.FontSizeProperty, Math.Max(ViewModel.FontSize, 8));
                break;
        }
    }

    private void RegisterNewTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged += RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged += RichTextBoxOnSelectionChanged;
        richTextBox.PreviewMouseLeftButtonDown += RichTextBoxOnPreviewMouseLeftButtonDown;
        richTextBox.PreviewMouseMove += RichTextBoxOnPreviewMouseMove;
        richTextBox.PreviewMouseLeftButtonUp += RichTextBoxOnPreviewMouseLeftButtonUp;
        
        // 注册触摸事件
        richTextBox.PreviewTouchDown += RichTextBoxOnPreviewTouchDown;
        richTextBox.PreviewTouchMove += RichTextBoxOnPreviewTouchMove;
        richTextBox.PreviewTouchUp += RichTextBoxOnPreviewTouchUp;
    }

    private void UnregisterOldTextBox(RichTextBox richTextBox)
    {
        richTextBox.TextChanged -= RichTextBoxOnTextChanged;
        richTextBox.SelectionChanged -= RichTextBoxOnSelectionChanged;
        richTextBox.PreviewMouseLeftButtonDown -= RichTextBoxOnPreviewMouseLeftButtonDown;
        richTextBox.PreviewMouseMove -= RichTextBoxOnPreviewMouseMove;
        richTextBox.PreviewMouseLeftButtonUp -= RichTextBoxOnPreviewMouseLeftButtonUp;
        
        // 注销触摸事件
        richTextBox.PreviewTouchDown -= RichTextBoxOnPreviewTouchDown;
        richTextBox.PreviewTouchMove -= RichTextBoxOnPreviewTouchMove;
        richTextBox.PreviewTouchUp -= RichTextBoxOnPreviewTouchUp;
    }

    private void RichTextBoxOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 如果正在处理触摸事件，则忽略鼠标事件
        if (_isTouchSelecting)
        {
            e.Handled = true;
            return;
        }

        var richTextBox = sender as RichTextBox;
        if (richTextBox == null) return;

        var currentTime = DateTime.Now;
        var currentPosition = e.GetPosition(richTextBox);

        // 检查是否在短时间内点击了相同位置（双击）
        if ((currentTime - _lastClickTime).TotalMilliseconds < 500 && 
            Math.Abs(currentPosition.X - _lastClickPosition.X) < 2 && 
            Math.Abs(currentPosition.Y - _lastClickPosition.Y) < 2)
        {
            // 获取点击位置的文本指针
            var pointer = richTextBox.GetPositionFromPoint(currentPosition, true);
            if (pointer != null)
            {
                // 查找单词边界
                var start = pointer;
                var end = pointer;

                // 向前查找单词开始位置
                while (start.CompareTo(richTextBox.Document.ContentStart) > 0)
                {
                    start = start.GetPositionAtOffset(-1);
                    if (start == null) break;
                    
                    var charBefore = start.GetPointerContext(LogicalDirection.Forward);
                    if (charBefore == TextPointerContext.Text)
                    {
                        var textRun = start.GetTextInRun(LogicalDirection.Forward);
                        if (textRun.Length > 0 && char.IsWhiteSpace(textRun[0]))
                        {
                            start = start.GetPositionAtOffset(1); // 移动到非空格字符
                            break;
                        }
                    }
                    else if (charBefore != TextPointerContext.Text)
                    {
                        start = start.GetPositionAtOffset(1); // 移动到文本开始
                        break;
                    }
                }

                // 向后查找单词结束位置
                while (end.CompareTo(richTextBox.Document.ContentEnd) < 0)
                {
                    var charAfter = end.GetPointerContext(LogicalDirection.Forward);
                    if (charAfter == TextPointerContext.Text)
                    {
                        var textRun = end.GetTextInRun(LogicalDirection.Forward);
                        if (textRun.Length > 0)
                        {
                            if (char.IsWhiteSpace(textRun[0]))
                            {
                                break;
                            }
                            end = end.GetPositionAtOffset(1);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else if (charAfter == TextPointerContext.ElementEnd || charAfter == TextPointerContext.ElementStart)
                    {
                        break;
                    }
                    else
                    {
                        end = end.GetPositionAtOffset(1);
                    }
                }

                // 选择找到的文本
                if (start != null && end != null && start.CompareTo(end) < 0)
                {
                    richTextBox.Selection.Select(start, end);
                }
            }

            // 重置双击检测状态
            _lastClickTime = DateTime.MinValue;
        }
        else
        {
            // 更新上次点击时间和位置
            _lastClickTime = currentTime;
            _lastClickPosition = currentPosition;
            
            // 开始鼠标选择
            _startPoint = e.GetPosition(richTextBox);
            _isSelecting = true;
            richTextBox.CaptureMouse();
        }
    }

    private void RichTextBoxOnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        // 如果正在处理触摸事件，则忽略鼠标事件
        if (_isTouchSelecting || !_isSelecting) 
        {
            return;
        }

        var richTextBox = sender as RichTextBox;
        if (richTextBox == null || !_isSelecting) return;

        var currentPosition = e.GetPosition(richTextBox);
        
        // 只有当鼠标移动一定距离时才开始选择文本
        if (Math.Abs(currentPosition.X - _startPoint.X) > 2 || Math.Abs(currentPosition.Y - _startPoint.Y) > 2)
        {
            var startPointer = richTextBox.GetPositionFromPoint(_startPoint, true);
            var currentPointer = richTextBox.GetPositionFromPoint(currentPosition, true);
            
            if (startPointer != null && currentPointer != null)
            {
                richTextBox.Selection.Select(startPointer, currentPointer);
            }
        }
    }

    private void RichTextBoxOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // 如果正在处理触摸事件，则忽略鼠标事件
        if (_isTouchSelecting)
        {
            return;
        }

        var richTextBox = sender as RichTextBox;
        if (richTextBox == null || !_isSelecting) return;

        _isSelecting = false;
        richTextBox.ReleaseMouseCapture();
    }

    // 触摸事件处理
    private void RichTextBoxOnPreviewTouchDown(object sender, TouchEventArgs e)
    {
        // 停止任何正在进行的鼠标选择
        _isSelecting = false;
        
        var richTextBox = sender as RichTextBox;
        if (richTextBox == null) return;

        _touchStartPoint = e.GetTouchPoint(richTextBox).Position;
        _isTouchSelecting = true;
        richTextBox.CaptureTouch(e.TouchDevice);
        e.Handled = true;
    }

    private void RichTextBoxOnPreviewTouchMove(object sender, TouchEventArgs e)
    {
        if (!_isTouchSelecting) return;

        var richTextBox = sender as RichTextBox;
        if (richTextBox == null) return;

        var currentPosition = e.GetTouchPoint(richTextBox).Position;
        
        // 只有当触摸移动一定距离时才开始选择文本
        if (Math.Abs(currentPosition.X - _touchStartPoint.X) > 5 || Math.Abs(currentPosition.Y - _touchStartPoint.Y) > 5)
        {
            var startPointer = richTextBox.GetPositionFromPoint(_touchStartPoint, true);
            var currentPointer = richTextBox.GetPositionFromPoint(currentPosition, true);
            
            if (startPointer != null && currentPointer != null)
            {
                richTextBox.Selection.Select(startPointer, currentPointer);
            }
        }
        
        e.Handled = true;
    }

    private void RichTextBoxOnPreviewTouchUp(object sender, TouchEventArgs e)
    {
        _isTouchSelecting = false;
        var richTextBox = sender as RichTextBox;
        if (richTextBox != null)
        {
            richTextBox.ReleaseTouchCapture(e.TouchDevice);
        }
        e.Handled = true;
    }

    private void RichTextBoxOnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;

        var selection = RelatedRichTextBox.Selection;
        if (selection.IsEmpty)
        {
            return;
        }

        UpdateViewModelFromSelection(selection);
    }

    private void UpdateViewModelFromSelection(TextRange selection)
    {
        ViewModel.IsRestoringSelection = true;

        // 更新字体加粗状态
        var fontWeight = selection.GetPropertyValue(TextElement.FontWeightProperty) as FontWeight?;
        if (fontWeight.HasValue)
        {
            ViewModel.IsBold = fontWeight.Value >= FontWeights.Bold;
        }

        // 更新字体样式（斜体）
        ViewModel.IsItalic = Equals(selection.GetPropertyValue(TextElement.FontStyleProperty), FontStyles.Italic);

        // 更新下划线和删除线状态
        var decorations = selection.GetPropertyValue(Paragraph.TextDecorationsProperty) as TextDecorationCollection;
        if (decorations != null)
        {
            ViewModel.IsUnderlined = decorations.Contains(TextDecorations.Underline[0]);
            ViewModel.IsStrikeThrough = decorations.Contains(TextDecorations.Strikethrough[0]);
        }

        // 更新文本颜色
        if (selection.GetPropertyValue(TextElement.ForegroundProperty) is SolidColorBrush fg)
        {
            ViewModel.TextColor = fg.Color;
        }

        // 更新字体和字号
        if (selection.GetPropertyValue(TextElement.FontFamilyProperty) is FontFamily font)
        {
            ViewModel.Font = font;
        }

        if (selection.GetPropertyValue(TextElement.FontSizeProperty) is double fontSize)
        {
            ViewModel.FontSize = fontSize;
        }

        ViewModel.IsRestoringSelection = false;
    }

    private void RichTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void ListBoxTextStyles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsRestoringSelection)
            return;
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.FontWeightProperty, ViewModel.IsBold ? FontWeights.Bold : FontWeights.Regular);
        s.ApplyPropertyValue(TextElement.FontStyleProperty, ViewModel.IsItalic ? FontStyles.Italic : FontStyles.Normal);
        var decorations = new TextDecorationCollection();
        if (ViewModel.IsUnderlined)
            decorations.Add(TextDecorations.Underline);
        if (ViewModel.IsStrikeThrough)
            decorations.Add(TextDecorations.Strikethrough);
        s.ApplyPropertyValue(Paragraph.TextDecorationsProperty, decorations);
        RelatedRichTextBox.Focus();
    }

    private void ButtonClearColor_OnClick(object sender, RoutedEventArgs e)
    {
        var s = RelatedRichTextBox.Selection;
        s.ApplyPropertyValue(TextElement.ForegroundProperty, GetValue(TextElement.ForegroundProperty));
    }



    private void ButtonFontSizeDecrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize -= 2;
    }

    private void ButtonFontSizeIncrease_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.FontSize += 2;
    }

    private void ButtonEditingDone_OnClick(object sender, RoutedEventArgs e)
    {
        // 完成编辑，关闭窗口
        TryClose();
    }


    private bool ValidateBackupFile(string backupFilePath)
    {
        try
        {
            // 检查文件是否存在
            if (!File.Exists(backupFilePath))
            {
                return false;
            }

            // 检查文件大小是否为0
            if (new FileInfo(backupFilePath).Length == 0)
            {
                return false;
            }

            // 检查文件内容是否有效
            using (StreamReader reader = new StreamReader(backupFilePath))
            {
                string content = reader.ReadToEnd();
                // 尝试解析 JSON 内容
                JsonDocument.Parse(content);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"验证备份文件 {backupFilePath} 失败：{ex.Message}");
            return false;
        }
    }

    private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SubjectChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ButtonAddToColor_OnClick(object sender, RoutedEventArgs e)
    {
        if (SettingsService.Settings.SavedColors.Contains(ViewModel.TextColor))
            return;
        SettingsService.Settings.SavedColors.Insert(0, ViewModel.TextColor);
        while (SettingsService.Settings.SavedColors.Count > 6)
        {
            SettingsService.Settings.SavedColors.RemoveAt(6);
        }
        SettingsService.SaveSettings();
    }

    private void ListBoxColors_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel.IsUpdatingColor)
            return;
        ViewModel.IsUpdatingColor = true;
        foreach (var i in e.AddedItems)
        {
            if (i is Color c)
                ViewModel.TextColor = c;
        }

        if (sender is ListBox l)
            l.SelectedIndex = -1;
        ViewModel.IsUpdatingColor = false;
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

    public void ShowAtMousePosition()
    {
        // 获取鼠标位置
        var mousePosition = System.Windows.Forms.Control.MousePosition;

        // 设置窗口位置为鼠标位置的右侧
        Left = mousePosition.X + 10; // 向右偏移10个像素
        Top = mousePosition.Y;

        // 确保窗口在屏幕内
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // 确保窗口在屏幕内
        if (Left < 0) Left = 0;
        if (Top < 0) Top = 0;
        if (Left + ActualWidth > screenWidth) Left = screenWidth - ActualWidth;
        if (Top + ActualHeight > screenHeight) Top = screenHeight - ActualHeight;

        // 显示窗口
        Show();
        IsOpened = true; // 设置窗口状态为已打开
    }



    private void CenterWindowOnScreen()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var screenHeight = SystemParameters.PrimaryScreenHeight;

        // 计算窗口的中心位置
        Left = (screenWidth - ActualWidth) / 2;
        Top = (screenHeight - ActualHeight) / 2;

        // 确保窗口在屏幕内
        if (Left < 0) Left = 0;
        if (Top < 0) Top = 0;
        if (Left + ActualWidth > screenWidth) Left = screenWidth - ActualWidth;
        if (Top + ActualHeight > screenHeight) Top = screenHeight - ActualHeight;
    }

    //private void EmojiButton_Click(object sender, RoutedEventArgs e)
    //{
    //    if (Application.Current.Windows.OfType<EmotionsMgrWindow>().Any())
    //    {
    //        // 如果窗口已存在，直接激活
    //        var existingWindow = Application.Current.Windows.OfType<EmotionsMgrWindow>().First();
    //        existingWindow.Activate();
    //    }
    //    else
    //    {
    //        // 创建新窗口
    //        var emotionsMgrWindow = new EmotionsMgrWindow(new Core.Context.AppDbContext());
    //        emotionsMgrWindow.Owner = this;
    //        emotionsMgrWindow.Show();
    //    }
    //}

    private void EmojiButton_Click(object sender, RoutedEventArgs e)
    {
        var emojiPicker = new EmotionsMgrWindow();
        if (emojiPicker.ShowDialog() == true)
        {
            var selectedEmoji = emojiPicker.SelectedEmoji;
            if (!string.IsNullOrEmpty(selectedEmoji) && RelatedRichTextBox != null)
            {
                RelatedRichTextBox.AppendText(selectedEmoji);
            }
        }
    }

}