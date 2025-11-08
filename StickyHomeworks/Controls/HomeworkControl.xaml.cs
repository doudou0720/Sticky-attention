using ElysiaFramework;
using StickyHomeworks.Models;
using StickyHomeworks.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace StickyHomeworks.Controls;

/// <summary>
/// HomeworkControl.xaml 的交互逻辑
/// </summary>
public partial class HomeworkControl : UserControl
{
    public static readonly DependencyProperty HomeworkProperty = DependencyProperty.Register(
        nameof(Homework), typeof(Homework), typeof(HomeworkControl), new PropertyMetadata(default(Homework), OnHomeworkChanged));

    private static void OnHomeworkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (HomeworkControl)d;
        if (e.OldValue is Homework oldHomework)
        {
            oldHomework.PropertyChanged -= control.HomeworkOnPropertyChanged;
        }
        
        if (e.NewValue is Homework newHomework)
        {
            newHomework.PropertyChanged += control.HomeworkOnPropertyChanged;
        }
    }

    private void HomeworkOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 当作业属性变化时，更新控件显示
        if (e.PropertyName == nameof(Homework.IsNearExpiration) || 
            e.PropertyName == nameof(Homework.IsExpired))
        {
            // 触发UI更新
            Dispatcher?.Invoke(() => {
                // 强制更新绑定
                RichTextBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
                
                // 如果是过期状态变化，触发动画
                if (e.PropertyName == nameof(Homework.IsExpired) && Homework?.IsExpired == true)
                {
                    AnimateStrikethrough();
                }
            });
        }
    }

    /// <summary>
    /// 删除线加载事件处理程序
    /// </summary>
    private void StrikethroughLine_OnLoaded(object sender, RoutedEventArgs e)
    {
        // 如果作业已过期，立即显示删除线动画
        if (Homework?.IsExpired == true)
        {
            AnimateStrikethrough();
        }
    }

    /// <summary>
    /// 为删除线添加动画效果
    /// </summary>
    private void AnimateStrikethrough()
    {
        if (StrikethroughLine == null || Homework == null) return;

        // 等待布局更新后再计算宽度
        Dispatcher?.BeginInvoke(new Action(() => {
            // 计算文本的宽度以确定删除线的终点
            var textWidth = CalculateTextWidth();
            
            // 设置删除线的起始点
            StrikethroughLine.X1 = 0;
            
            // 创建动画
            var animation = new DoubleAnimation
            {
                From = 0,
                To = textWidth,
                Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };
            
            // 应用动画
            StrikethroughLine.BeginAnimation(Line.X2Property, animation);
        }), System.Windows.Threading.DispatcherPriority.Render);
    }

    /// <summary>
    /// 计算文本的宽度
    /// </summary>
    /// <returns>文本宽度</returns>
    private double CalculateTextWidth()
    {
        if (RichTextBox?.Document == null) return 100; // 默认值
        
        // 创建一个格式化器来测量文本
        var formattedText = new FormattedText(
            new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd).Text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(RichTextBox.FontFamily, RichTextBox.FontStyle, RichTextBox.FontWeight, RichTextBox.FontStretch),
            RichTextBox.FontSize,
            Brushes.Black,
            new NumberSubstitution(),
            1);
            
        return formattedText.Width;
    }

    public Homework Homework
    {
        get { return (Homework)GetValue(HomeworkProperty); }
        set { SetValue(HomeworkProperty, value); }
    }

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(HomeworkControl), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                var c = o as HomeworkControl;
                c?.IsSelectedChanged((bool)args.NewValue);
            }));

    public bool IsSelected
    {
        get { return (bool)GetValue(IsSelectedProperty); }
        set { SetValue(IsSelectedProperty, value); }
    }

    public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register(
        nameof(IsEditing), typeof(bool), typeof(HomeworkControl), new PropertyMetadata(default(bool),
            (o, args) =>
            {
                var c = o as HomeworkControl;
                c?.IsEditingChanged((bool)args.NewValue);
            }));

    public bool IsEditing
    {
        get { return (bool)GetValue(IsEditingProperty); }
        set { SetValue(IsEditingProperty, value); }
    }

    // 触摸选择相关字段
    private bool _isTouchSelecting = false;
    private Point _touchStartPoint;

    public HomeworkControl()
    {
        InitializeComponent();
        
        // 注册触摸事件
        RichTextBox.PreviewTouchDown += RichTextBox_PreviewTouchDown;
        RichTextBox.PreviewTouchMove += RichTextBox_PreviewTouchMove;
        RichTextBox.PreviewTouchUp += RichTextBox_PreviewTouchUp;
        
        // 监听大小变化以更新删除线
        SizeChanged += HomeworkControl_SizeChanged;
    }

    private void HomeworkControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 如果作业已过期，更新删除线的宽度
        if (Homework?.IsExpired == true)
        {
            AnimateStrikethrough();
        }
    }

    private void IsEditingChanged(bool value)
    {
        Debug.WriteLine($"IsEditing changed! {value} {IsSelected}");
        if (IsSelected && value)
        {
            Debug.WriteLine("RelatedRichTextBox updated because IsEditing changed");
            EnterEdit();
        }
    }

    private async void EnterEdit()
    {
        if (RichTextBox == null) return;
        AppEx.GetService<HomeworkEditWindow>().RelatedRichTextBox = RichTextBox;
        await System.Windows.Threading.Dispatcher.Yield();
        await Task.Delay(100); // 延迟一小段时间以确保输入框获得焦点
        RichTextBox.Focus();
        RichTextBox.CaretPosition = RichTextBox.Document.ContentEnd;
    }


    private void IsSelectedChanged(bool value)
    {
        Debug.WriteLine($"IsSelected changed! {value} {IsEditing}");
        if (value && IsEditing)
        {
            Debug.WriteLine("RelatedRichTextBox updated because IsSelected changed");
            EnterEdit();
        }
    }

    private void RichTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            e.Handled = true; // 防止触发其他事件
            App.GetService<MainWindow>().OnTextBoxEnter();
        }
    }


    private void RichTextBox_PreviewTouchDown(object sender, TouchEventArgs e)
    {
        _isTouchSelecting = true;
        _touchStartPoint = e.GetTouchPoint(RichTextBox).Position;
        RichTextBox.CaptureTouch(e.TouchDevice);
        e.Handled = true; // 阻止焦点丢失
        ((RichTextBox)sender).Focus();
    }

    private void RichTextBox_PreviewTouchMove(object sender, TouchEventArgs e)
    {
        if (!_isTouchSelecting) return;

        var richTextBox = sender as RichTextBox;
        if (richTextBox == null) return;

        var currentPosition = e.GetTouchPoint(RichTextBox).Position;
        
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

    private void RichTextBox_PreviewTouchUp(object sender, TouchEventArgs e)
    {
        _isTouchSelecting = false;
        RichTextBox.ReleaseTouchCapture(e.TouchDevice);
        e.Handled = true;
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // 防止触摸事件被鼠标事件干扰
        if (_isTouchSelecting)
        {
            e.Handled = true;
            return;
        }
        
        base.OnPreviewMouseDown(e);
    }
}