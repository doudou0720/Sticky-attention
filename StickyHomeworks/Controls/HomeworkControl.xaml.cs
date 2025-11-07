using ElysiaFramework;
using StickyHomeworks.Models;
using StickyHomeworks.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StickyHomeworks.Controls;

/// <summary>
/// HomeworkControl.xaml 的交互逻辑
/// </summary>
public partial class HomeworkControl : UserControl
{
    public static readonly DependencyProperty HomeworkProperty = DependencyProperty.Register(
        nameof(Homework), typeof(Homework), typeof(HomeworkControl), new PropertyMetadata(default(Homework)));

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