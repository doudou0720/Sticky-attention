using ClassIsland.Services;
using ElysiaFramework;
using ElysiaFramework.Controls;
using MaterialDesignThemes.Wpf;
using StickyHomeworks.Models;
using StickyHomeworks.Views;
using StickyHomeworks.Services;
using StickyHomeworks.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using StickyHomeworks;
using Microsoft.AppCenter.Crashes;
using MdXaml;
using System.Windows.Documents;
using System.Windows.Media;
using System.Net.Http;
using static StickyHomeworks.App;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;

namespace StickyHomeworks.Views;
/// <summary>
/// SettingsWindow.xaml 的交互逻辑
/// </summary>
public partial class SettingsWindow : MyWindow
{
    private GitHubUpdateService _updateService;

    private const string IconPath01 = "/Assets/icon/上传 (1).png"; // 有最新版本时的图标
    private const string IconPath02 = "/Assets/icon/成功 (2).png"; // 没有最新版本时的图标
    private const string IconPath03 = "/Assets/icon/叹号 (1).png"; // 有最新版本且下载完毕时的图标
    private const string DownloadFilePath = "update.zip";
    private const string DecompressionFolder = "Decompression update";

    private Markdown engine;
    private FlowDocument document;

    // 添加IsOpened属性
    public bool IsOpened { get; set; } = false;

    public SettingsViewModel ViewModel
    {
        get;
        set;
    } = new();

    public Settings Settings
    {
        get;
        set;
    }

    // 正确声明WallpaperPickingService字段
    public WallpaperPickingService WallpaperPickingService { get; set; }

    public SettingsWindow(WallpaperPickingService wallpaperPickingService,
        SettingsService settingsService)
    {
     
        WallpaperPickingService = wallpaperPickingService;
        LogHelper.Info($"设置界面 OPEN！");
        
        InitializeComponent();
        DataContext = this;
        Settings = settingsService.Settings;
        _updateService = new GitHubUpdateService(Settings);
        settingsService.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == "Settings")
            {
                settingsService.Settings.PropertyChanged += SettingsOnPropertyChanged;
                Settings = settingsService.Settings;
            }
        };
        var style = (Style)FindResource("NotificationsListBoxItemStyle");
        //style.Setters.Add(new EventSetter(ListBoxItem.MouseDoubleClickEvent, new System.Windows.Input.MouseEventHandler(EventSetter_OnHandler)));

        // 初始化 Markdown 引擎
        engine = new Markdown();
    }

    private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 检查是否是需要重启的设置项
        if (e.PropertyName == nameof(Settings.HttpServerEnabled) || 
            e.PropertyName == nameof(Settings.GrpcEnabled) ||
            e.PropertyName == nameof(Settings.HttpServerPort) ||
            e.PropertyName == nameof(Settings.GrpcPort))
        {
            // 显示重启提示
            MessageBox.Show(
                "此设置将在下次启动时生效，请重启应用程序以应用更改。", 
                "需要重启", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }
        
        // 更新服务器状态显示
        UpdateServerStatus();
    }

    /// <summary>
    /// 更新服务器状态显示
    /// </summary>
    private void UpdateServerStatus()
    {
        // 更新HTTP服务器状态
        if (Settings.HttpServerEnabled)
        {
            ViewModel.HttpServerStatusText = "Started";
            ViewModel.HttpServerStatusDetailText = $"Running on port {Settings.HttpServerPort}";
        }
        else
        {
            ViewModel.HttpServerStatusText = "Stopped";
            ViewModel.HttpServerStatusDetailText = "HTTP server is disabled";
        }

        // 更新gRPC服务状态
        if (Settings.GrpcEnabled)
        {
            ViewModel.GrpcServiceStatusText = "Started";
            ViewModel.GrpcServiceStatusDetailText = $"Running on port {Settings.GrpcPort}";
        }
        else
        {
            ViewModel.GrpcServiceStatusText = "Stopped";
            ViewModel.GrpcServiceStatusDetailText = "gRPC service is disabled";
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        //RefreshMonitors();
        //var r = new StreamReader(Application.GetResourceStream(new Uri(" / Assets/LICENSE.txt", UriKind.Relative))!.Stream);
        //ViewModel.License = r.ReadToEnd();
        base.OnInitialized(e);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        Settings.PropertyChanged += SettingsOnPropertyChanged;
        // 初始化服务器状态显示
        UpdateServerStatus();
        base.OnContentRendered(e);
    }

    private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!e.Handled)
        {
            // ListView拦截鼠标滚轮事件
            e.Handled = true;

            // 激发一个鼠标滚轮事件，冒泡给外层ListView接收到
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            var parent = ((System.Windows.Controls.Control)sender).Parent as UIElement;
            if (parent != null)
            {
                parent.RaiseEvent(eventArg);
            }
        }
    }

    private void SettingsWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        AppEx.GetService<SettingsService>().SaveSettings();
        IsOpened = false;
    }

    private void ButtonCrash_OnClick(object sender, RoutedEventArgs e)
    {
        throw new Exception("Crash test.");
    }

    private void HyperlinkMsAppCenter_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo()
        {
            FileName = "https://learn.microsoft.com/zh-cn/appcenter/sdk/data-collected",
            UseShellExecute = true
        });
    }

    private void MyDrawerHost_OnDrawerClosing(object? sender, DrawerClosingEventArgs e)
    {
    }

    private void ButtonDebugToastText_OnClick(object sender, RoutedEventArgs e)
    {

    }


    private void ButtonDebugNetworkError_OnClick(object sender, RoutedEventArgs e)
    {
        //UpdateService.CurrentWorkingStatus = UpdateWorkingStatus.NetworkError;
    }


    private void OpenDrawer(string key)
    {
        MyDrawerHost.IsRightDrawerOpen = true;
        ViewModel.DrawerContent = FindResource(key);
    }

    private async Task<object?> ShowDialog(string key)
    {
        return await DialogHost.Show(FindResource(key), "SettingsWindow");
    }


    private void ButtonContributors_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ContributorsDrawer");
    }

    private void ButtonThirdPartyLibs_OnClick(object sender, RoutedEventArgs e)
    {
        OpenDrawer("ThirdPartyLibs");
    }

    private void AppIcon_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.AppIconClickCount++;
        if (ViewModel.AppIconClickCount >= 10)
        {
            Settings.IsDebugOptionsEnabled = true;
        }
    }

    private void ButtonCloseDebug_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.IsDebugOptionsEnabled = false;
        ViewModel.AppIconClickCount = 0;
    }

    private void MenuItemDebugScreenShot_OnClick(object sender, RoutedEventArgs e)
    {

    }

    private async void ButtonUpdateWallpaper_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            await WallpaperPickingService.GetWallpaperAsync();
        }
        catch (Exception ex)
        {
            LogHelper.Error($"更新壁纸时发生错误: {ex.Message}", ex);
            MessageBox.Show($"更新壁纸失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ButtonBrowseWindows_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var w = new WindowsPicker(Settings.WallpaperClassName)
            {
                Owner = this,
            };

            // ShowDialog 返回的是 bool? (Nullable<bool>)，需要判断 HasValue 并检查值
            if (w.ShowDialog() == true)
            {
                Settings.WallpaperClassName = w.SelectedResult ?? "";
                await WallpaperPickingService.GetWallpaperAsync();
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"浏览窗口时发生错误: {ex.Message}", ex);
            MessageBox.Show($"操作失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            GC.Collect(); // 确保即使发生异常，也会尝试进行垃圾回收
        }
    }

    private void MenuItemExperimentalSettings_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.IsPopupMenuOpened = false;
        OpenDrawer("ExperimentalSettings");
    }

    private async Task EditSubjectAsync(int index)
    {
        ViewModel.SubjectEditText = Settings.Subjects[index];
        var r = (string?)await ShowDialog("EditSubjectDialog");
        if (r == null) return;
        Settings.Subjects[index] = r;
    }

    private async Task EditTagAsync(int index)
    {
        ViewModel.TagEditText = Settings.Tags[index];
        var r = (string?)await ShowDialog("EditTagDialog");
        if (r == null) return;
        Settings.Tags[index] = r;
    }

    private async void ButtonAddSubject_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Subjects.Add("");
        await EditSubjectAsync(Settings.Subjects.Count - 1);
        var r = Settings.Subjects.Last();
        if (r == "")
        {
            Settings.Subjects.RemoveAt(Settings.Subjects.Count - 1);
        }
        else
        {
            ViewModel.SubjectSelectedIndex = Settings.Subjects.Count - 1;
        }
    }

    private async void ButtonEditSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        await EditSubjectAsync(ViewModel.SubjectSelectedIndex);
    }

    private void ButtonDeleteSubject_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SubjectSelectedIndex == -1)
        {
            return;
        }
        Settings.Subjects.RemoveAt(ViewModel.SubjectSelectedIndex);
    }

    private async void ButtonAddTag_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Tags.Add("");
        await EditTagAsync(Settings.Tags.Count - 1);
        var r = Settings.Tags.Last();
        if (r == "")
        {
            Settings.Tags.RemoveAt(Settings.Tags.Count - 1);
        }
        else
        {
            ViewModel.TagSelectedIndex = Settings.Tags.Count - 1;
        }
    }

    private async void ButtonEditTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        await EditTagAsync(ViewModel.TagSelectedIndex);
    }

    private void ButtonDeleteTag_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TagSelectedIndex == -1)
        {
            return;
        }
        Settings.Tags.RemoveAt(ViewModel.TagSelectedIndex);
    }

    private void MenuItemTestHomeworkEditWindow_OnClick(object sender, RoutedEventArgs e)
    {
        AppEx.GetService<HomeworkEditWindow>().Show();
    }

    private void OpenGitHub_Click(object sender, RoutedEventArgs e)
    {
        // Fork项目：原始仓库为Sticky-attention/Sticky-attention，此fork仓库为doudou0720/Sticky-attention
        // 要打开的URL
        string url = "https://github.com/doudou0720/Sticky-attention/";

        // 使用默认浏览器打开URL
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            // 处理异常，例如无法打开浏览器的情况
            MessageBox.Show($"无法打开: {ex.Message}");
        }
    }

    private void IconText_Loaded(object sender, RoutedEventArgs e)
    {

    }

    private void IconText_Loaded_1(object sender, RoutedEventArgs e)
    {

    }




    private void Check_for_updates(object sender, RoutedEventArgs e)
    {
        // 显示进度条和标签
        pbDown.Visibility = Visibility.Visible;
        labelProgress.Visibility = Visibility.Visible;

        // 调用基于XML的更新检查
        CheckForUpdates();
    }

    // 保留原有的CheckForUpdates方法以保持兼容性
    // 保留原有的CheckForUpdates方法以保持兼容性
    private async void CheckForUpdates()
    {
        try
        {
            // 使用GitHubUpdateService检查更新
            var latestRelease = await _updateService.GetLatestReleaseAsync();
            
            if (latestRelease != null)
            {
                var currentVersion = _updateService.GetCurrentVersion();
                if (_updateService.IsNewerVersion(latestRelease.TagName, currentVersion))
                {
                    // 有新版本
                    Dispatcher.Invoke(() =>
                    {
                        versionStatusTextBlock.Text = "检测到最新版本";
                        versionStatusText.Text = _updateService.FormatVersionDisplay(latestRelease.TagName); // 显示格式化后的版本号
                        versionStatusTexts.Text = "";
                        versionStatusText.FontSize = 18;
                        versionStatusText.FontWeight = FontWeights.Bold;

                        versionStatusTextBlock.FontSize = 40;
                        versionStatusTextBlock.FontWeight = FontWeights.Bold;
                        statusIcon.Source = new BitmapImage(new Uri(IconPath01, UriKind.Relative));
                    });

                    // 寻找适合当前系统的更新文件
                    var appropriateAsset = _updateService.FindAppropriateAsset(latestRelease.Assets);
                    if (appropriateAsset != null)
                    {
                        // 显示更新内容（release body）并询问用户是否更新
                        var message = $"发现新版本: {_updateService.FormatVersionDisplay(latestRelease.TagName)}\n\n更新内容:\n{latestRelease.Body}\n\n是否下载并安装此更新？";
                        var result = MessageBox.Show(message, "Sticky-attention 更新", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            using (var client = new WebClient())
                            {
                                // 开始下载
                                await DownloadUpdate(client, appropriateAsset);
                            }

                            // 下载完毕
                            Dispatcher.Invoke(() =>
                            {
                                versionStatusTextBlock.Text = "下载完成，请安装最新版本！";
                                statusIcon.Source = new BitmapImage(new Uri(IconPath03, UriKind.Relative));

                                var installResult = MessageBox.Show("您确定要运行更新程序吗？", "Sticky-attention", MessageBoxButton.YesNo, MessageBoxImage.Question);

                                if (installResult == MessageBoxResult.Yes)
                                {
                                    // 解压文件
                                    UnzipFile(DownloadFilePath, DecompressionFolder);

                                    // 运行解压后的程序并关闭当前程序
                                    string executablePath = Path.Combine(DecompressionFolder, "StickyHomeworks.exe");
                                    RunExecutableAndCloseApp(executablePath);
                                }
                                else
                                {
                                    // 隐藏进度条和标签
                                    pbDown.Visibility = Visibility.Collapsed;
                                    labelProgress.Visibility = Visibility.Collapsed;
                                }
                            });
                        }
                        else
                        {
                            // 用户选择不更新，隐藏进度条和标签
                            Dispatcher.Invoke(() =>
                            {
                                pbDown.Visibility = Visibility.Collapsed;
                                labelProgress.Visibility = Visibility.Collapsed;
                            });
                        }
                    }
                    else
                    {
                        // 没有适合当前系统的更新文件
                        Dispatcher.Invoke(() =>
                        {
                            versionStatusTextBlock.Text = "无合适更新";
                            versionStatusTextBlock.FontSize = 40;
                            versionStatusTextBlock.FontWeight = FontWeights.Bold;
                            versionStatusText.Text = "未找到适用于您系统的更新文件";
                            versionStatusTexts.Text = "";
                            statusIcon.Source = new BitmapImage(new Uri(IconPath02, UriKind.Relative));

                            pbDown.Visibility = Visibility.Collapsed;
                            labelProgress.Visibility = Visibility.Collapsed;
                        });
                    }
                }
                else
                {
                    // 没有新版本
                    Dispatcher.Invoke(() =>
                    {
                        versionStatusTextBlock.Text = "您已是最新！";
                        versionStatusTextBlock.FontSize = 40;
                        versionStatusTextBlock.FontWeight = FontWeights.Bold;
                        statusIcon.Source = new BitmapImage(new Uri(IconPath02, UriKind.Relative));

                        versionStatusText.Text = _updateService.FormatVersionDisplay(_updateService.GetCurrentVersion()); // 显示格式化后的当前版本
                        versionStatusTexts.Text = ""; // 显示当前版本
                        versionStatusText.FontSize = 18;
                        versionStatusText.FontWeight = FontWeights.Bold;

                        pbDown.Visibility = Visibility.Collapsed;
                        labelProgress.Visibility = Visibility.Collapsed;
                    });
                }
            }
            else
            {
                // 无法获取更新信息
                Dispatcher.Invoke(() =>
                {
                    versionStatusTextBlock.Text = "检查更新失败";
                    versionStatusTextBlock.FontSize = 40;
                    versionStatusText.Text = "无法连接到更新服务器";
                    versionStatusTexts.Text = "";
                    pbDown.Visibility = Visibility.Collapsed;
                    labelProgress.Visibility = Visibility.Collapsed;
                });
            }
        }
        catch (Exception ex)
        {
            // 发生异常
            Dispatcher.Invoke(() =>
            {
                versionStatusTextBlock.Text = "发生错误 " ; // 显示具体错误
                versionStatusTextBlock.FontSize = 40;
                versionStatusText.Text = "错误详细: " + ex.Message; // 显示具体错误
                versionStatusTexts.Text = "";
                pbDown.Visibility = Visibility.Collapsed;
                labelProgress.Visibility = Visibility.Collapsed;
            });
        }
    }


    private async Task DownloadUpdate(HttpClient client, string url)
    {
        // 使用镜像URL替换原始URL（如果配置了镜像）
        var mirrorUrl = Settings.UpdateMirrorUrl;
        var finalUrl = _updateService.ReplaceWithMirrorUrl(url, mirrorUrl);
        
        using (HttpResponseMessage response = await client.GetAsync(finalUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            using (Stream contentStream = await response.Content.ReadAsStreamAsync())
            using (FileStream fileStream = new FileStream("update.zip", FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                long totalBytesRead = 0;
                DateTime startTime = DateTime.Now;
                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;

                    // 计算下载速度
                    DateTime now = DateTime.Now;
                    TimeSpan elapsedTime = now - startTime;
                    double seconds = elapsedTime.TotalSeconds;
                    double speed = totalBytesRead / (1024 * seconds); // 以KB/s为单位

                    // 输出到控制台
                    LogHelper.Info($"下载进度: {totalBytesRead}/{totalBytes}, 速度: {speed:F2} KB/s");
                }
            }
        }
    }

    private async Task DownloadUpdate(WebClient client, string url)
    {
        // 使用镜像URL替换原始URL（如果配置了镜像）
        var mirrorUrl = Settings.UpdateMirrorUrl;
        var finalUrl = _updateService.ReplaceWithMirrorUrl(url, mirrorUrl);

        client.DownloadProgressChanged += Client_DownloadProgressChanged;
        client.DownloadFileCompleted += Client_DownloadFileCompleted;

        // 开始下载
        await client.DownloadFileTaskAsync(new Uri(finalUrl), DownloadFilePath);
    }

    private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
        // 更新进度条
        Dispatcher.Invoke(() =>
        {
            pbDown.Value = e.ProgressPercentage;
            labelProgress.Content = $"{e.ProgressPercentage}%";
        });
    }

    private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            // 处理错误
            Dispatcher.Invoke(() =>
            {
                versionStatusTextBlock.Text = "更新时发生错误(001)❌";
                // 隐藏进度条和标签
                pbDown.Visibility = Visibility.Collapsed;
                labelProgress.Visibility = Visibility.Collapsed;
            });
        }
        else
        {
            // 下载完成
        }
    }

    private void UnzipFile(string zipFilePath, string outputFolder)
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        using (ZipArchive archive = ZipFile.OpenRead(zipFilePath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string fullPath = Path.Combine(outputFolder, entry.FullName);
                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(fullPath);
                }
                else
                {
                    entry.ExtractToFile(fullPath, true);
                }
            }
        }
    }

    private (string Version, string Url, string Changelog, bool Mandatory) ParseUpdateInfoFromXml(string xmlContent)
    {
        XDocument doc = XDocument.Parse(xmlContent);
        var versionElement = doc.Root.Element("Version");
        var urlElement = doc.Root.Element("Url");
        var changelogElement = doc.Root.Element("Changelog");
        var mandatoryElement = doc.Root.Element("Mandatory");

        string version = versionElement?.Value ?? "0.0.0";
        string url = urlElement?.Value ?? string.Empty;
        string changelog = changelogElement?.Value ?? string.Empty;
        bool mandatory = bool.TryParse(mandatoryElement?.Value, out bool isMandatory) && isMandatory;

        return (version, url, changelog, mandatory);
    }

    private void RunExecutableAndCloseApp(string filePath)
    {
        try
        {
            // 获取当前应用程序的路径
            string currentAppPath = System.Reflection.Assembly.GetEntryAssembly()?.Location 
                                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName 
                                    ?? throw new InvalidOperationException("无法获取当前应用程序路径");
            
            // 获取当前应用程序所在目录
            string currentAppDirectory = Path.GetDirectoryName(currentAppPath) 
                                         ?? throw new InvalidOperationException("无法获取当前应用程序目录");
            
            // 创建VBScript脚本的路径
            string updateScriptPath = Path.Combine(currentAppDirectory, "update.vbs");
            
            // 创建VBScript脚本来执行文件替换
            string vbsContent = $@"
Set fso = CreateObject(""Scripting.FileSystemObject"")
Set shell = CreateObject(""WScript.Shell"")

' 创建备份目录
backupDir = ""{currentAppDirectory}\\backup""
If Not fso.FolderExists(backupDir) Then
    fso.CreateFolder(backupDir)
End If

' 备份原程序文件，但不备份.config目录
On Error Resume Next
fso.CopyFile ""{currentAppDirectory}\\*.exe"", backupDir & ""\\"" , True
fso.CopyFile ""{currentAppDirectory}\\*.dll"", backupDir & ""\\"" , True

' 特别处理.config目录，只备份其中的文件而不是整个目录
configDir = ""{currentAppDirectory}\\.config""
If fso.FolderExists(configDir) Then
    backupConfigDir = backupDir & ""\\.config""
    fso.CreateFolder(backupConfigDir)
    fso.CopyFile configDir & ""\\*.*"", backupConfigDir & ""\\"" , True
    ' 递归复制子目录
    Set configFolder = fso.GetFolder(configDir)
    For Each subFolder In configFolder.SubFolders
        Set newFolder = fso.CreateFolder(backupConfigDir & ""\\"" & subFolder.Name)
        fso.CopyFile subFolder.Path & ""\\*.*"", newFolder.Path & ""\\"" , True
    Next
End If
On Error Goto 0

' 等待主程序关闭
WScript.Sleep 2000

' 终止主程序进程
shell.Run ""taskkill /f /im StickyHomeworks.exe"", 0, True

' 解压ZIP文件（使用Shell.Application）
Set app = CreateObject(""Shell.Application"")
zipFile = ""{currentAppDirectory}\\{DownloadFilePath}""
destinationFolder = ""{currentAppDirectory}\\temp_unzip""

' 创建临时解压目录
If Not fso.FolderExists(destinationFolder) Then
    fso.CreateFolder(destinationFolder)
End If

' 执行解压
app.NameSpace(destinationFolder).CopyHere app.NameSpace(zipFile).Items, 4 OR 16

' 等待解压完成（最多等待30秒）
count = 0
Do While count < 30
    Set destFolder = app.NameSpace(destinationFolder)
    If Not destFolder Is Nothing Then
        Set items = destFolder.Items
        If items.Count > 0 Then
            Exit Do
        End If
    End If
    WScript.Sleep 1000
    count = count + 1
Loop

' 检查解压是否成功
If Not fso.FolderExists(destinationFolder) Or fso.GetFolder(destinationFolder).Files.Count = 0 Then
    ' 解压失败，恢复备份并重启旧程序
    MsgBox ""更新失败：无法解压新版本文件。正在恢复原版本..."", vbExclamation, ""更新失败""
    RestoreAndRestart
    WScript.Quit
End If

' 复制解压后的文件到当前目录（但不覆盖.config目录）
On Error Resume Next
fso.CopyFile destinationFolder & ""\\*.*"", ""{currentAppDirectory}\\*.*"", True

' 检查复制是否成功
If Err.Number <> 0 Then
    ' 复制失败，恢复备份并重启旧程序
    MsgBox ""更新失败：无法复制新版本文件。正在恢复原版本..."", vbExclamation, ""更新失败""
    RestoreAndRestart
    WScript.Quit
End If
On Error Goto 0

' 删除解压目录和原始ZIP文件
If fso.FolderExists(destinationFolder) Then
    On Error Resume Next
    fso.DeleteFolder destinationFolder, True
    On Error Goto 0
End If

If fso.FileExists(zipFile) Then
    fso.DeleteFile zipFile
End If

' 启动更新后的程序
shell.Run """"""{currentAppDirectory}\\{Path.GetFileName(filePath)}"""""", 1, False

' 删除备份文件和此VBScript文件
WScript.Sleep 2000
On Error Resume Next
fso.DeleteFolder backupDir, True
Set WshShell = CreateObject(""WScript.Shell"")
WshShell.Run ""cmd /c del """"{updateScriptPath}"""""", 0, False
On Error Goto 0

' 恢复并重启的子程序
Sub RestoreAndRestart()
    Set fso = CreateObject(""Scripting.FileSystemObject"")
    Set shell = CreateObject(""WScript.Shell"")
    
    ' 删除可能已复制的新文件（但保留.config目录）
    On Error Resume Next
    Set newFiles = fso.GetFolder(""{currentAppDirectory}"").Files
    For Each file In newFiles
        If fso.FileExists(""backup\"" & file.Name) Then
            fso.DeleteFile file.Path
        End If
    Next
    
    ' 恢复备份的文件（但不处理.config目录）
    fso.CopyFile ""backup\\*.*"", ""{currentAppDirectory}\\*.*"", True
    
    ' 恢复.config目录中的文件
    backupConfigDir = ""backup\\.config""
    targetConfigDir = ""{currentAppDirectory}\\.config""
    If fso.FolderExists(backupConfigDir) Then
        If Not fso.FolderExists(targetConfigDir) Then
            fso.CreateFolder(targetConfigDir)
        End If
        fso.CopyFile backupConfigDir & ""\\*.*"", targetConfigDir & ""\\"" , True
        
        ' 恢复子目录
        Set backupConfigFolder = fso.GetFolder(backupConfigDir)
        For Each subFolder In backupConfigFolder.SubFolders
            targetSubFolder = targetConfigDir & ""\\"" & subFolder.Name
            If Not fso.FolderExists(targetSubFolder) Then
                fso.CreateFolder(targetSubFolder)
            End If
            fso.CopyFile subFolder.Path & ""\\*.*"", targetSubFolder & ""\\"" , True
        Next
    End If
    On Error Goto 0
    
    ' 启动旧程序
    shell.Run """"""{currentAppDirectory}\\{Path.GetFileName(filePath)}"""""", 1, False
    
    ' 删除此VBScript文件
    WScript.Sleep 1000
    Set WshShell = CreateObject(""WScript.Shell"")
    WshShell.Run ""cmd /c del """"{updateScriptPath}"""""", 0, False
End Sub
";

            // 写入VBScript文件
            File.WriteAllText(updateScriptPath, vbsContent, System.Text.Encoding.Unicode);
            
            // 关闭当前应用程序
            Application.Current.Shutdown();
            
            // 启动VBScript脚本
            Process.Start(new ProcessStartInfo
            {
                FileName = "wscript.exe",
                Arguments = $"//B \"{updateScriptPath}\"", // //B 参数隐藏脚本窗口
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"无法启动更新程序: {ex.Message}");
        }
    }



    private void ButtonExit_OnClick(object sender, RoutedEventArgs e)
    {
            // 显示一个消息框询问用户是否要关闭程序
            var result = System.Windows.MessageBox.Show("您确定要执行吗？", "风险提示", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
            // 如果用户选择“是”，则执行关闭逻辑
            Application.Current.Shutdown();
            Close();
        }
            else
            {
                // 如果用户选择“否”，则不执行任何操作
                return;
            }
        }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        SentrySdk.CaptureMessage("Hello Sentry");
        Crashes.GenerateTestCrash();
    }


    private async void CheckForUpdatesFromGitHub()
    {
        try
        {
            // 使用GitHub API获取最新版本信息
            var latestRelease = await _updateService.GetLatestReleaseAsync();
            
            if (latestRelease == null)
            {
                // 无法获取版本信息
                Dispatcher.Invoke(() =>
                {
                    versionStatusTextBlock.Text = "检查更新失败";
                    versionStatusTextBlock.FontSize = 40;
                    versionStatusText.Text = "无法连接到更新服务器";
                    versionStatusTexts.Text = "";
                    pbDown.Visibility = Visibility.Collapsed;
                    labelProgress.Visibility = Visibility.Collapsed;
                });
                return;
            }

            // 检查是否有新版本
            if (_updateService.IsNewerVersion(latestRelease.TagName, _updateService.GetCurrentVersion()))
            {
                // 有新版本
                Dispatcher.Invoke(() =>
                {
                    versionStatusTextBlock.Text = "检测到最新版本";
                    versionStatusText.Text = _updateService.FormatVersionDisplay(latestRelease.TagName); // 显示格式化后的版本号
                    versionStatusTexts.Text = "";
                    versionStatusText.FontSize = 18;
                    versionStatusText.FontWeight = FontWeights.Bold;

                    versionStatusTextBlock.FontSize = 40;
                    versionStatusTextBlock.FontWeight = FontWeights.Bold;
                    statusIcon.Source = new BitmapImage(new Uri(IconPath01, UriKind.Relative));
                });

                // 寻找合适的下载资源
                var appropriateAsset = _updateService.FindAppropriateAsset(latestRelease.Assets);
                if (!string.IsNullOrEmpty(appropriateAsset))
                {
                    using (var client = new HttpClient())
                    {
                        // 开始下载
                        await DownloadUpdate(client, appropriateAsset);

                        // 下载完毕
                        Dispatcher.Invoke(() =>
                        {
                            versionStatusTextBlock.Text = "下载完成，请安装最新版本！";
                            statusIcon.Source = new BitmapImage(new Uri(IconPath03, UriKind.Relative));

                            var result = MessageBox.Show("您确定要运行更新程序吗？", "Sticky-attention", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                // 用户点击了"是"，执行安装逻辑
                                UnzipFile(DownloadFilePath, DecompressionFolder);
                                RunExecutableAndCloseApp(Path.Combine(DecompressionFolder, "Sticky-attention.exe"));
                                Close();
                            }

                            // 隐藏进度条和标签
                            pbDown.Visibility = Visibility.Collapsed;
                            labelProgress.Visibility = Visibility.Collapsed;
                        });
                    }
                }
                else
                {
                    // 未找到合适的下载资源
                    Dispatcher.Invoke(() =>
                    {
                        versionStatusTextBlock.Text = "无合适更新";
                        versionStatusTextBlock.FontSize = 40;
                        versionStatusTextBlock.FontWeight = FontWeights.Bold;
                        versionStatusText.Text = "未找到适用于您系统的更新文件";
                        versionStatusTexts.Text = "";
                        statusIcon.Source = new BitmapImage(new Uri(IconPath02, UriKind.Relative));

                        // 隐藏进度条和标签
                        pbDown.Visibility = Visibility.Collapsed;
                        labelProgress.Visibility = Visibility.Collapsed;
                    });
                }
            }
            else
            {
                // 没有新版本
                Dispatcher.Invoke(() =>
                {
                    versionStatusTextBlock.Text = "您已是最新！";
                    versionStatusTextBlock.FontSize = 40;
                    versionStatusTextBlock.FontWeight = FontWeights.Bold;
                    statusIcon.Source = new BitmapImage(new Uri(IconPath02, UriKind.Relative));

                    versionStatusText.Text = _updateService.FormatVersionDisplay(_updateService.GetCurrentVersion()); // 显示格式化后的当前版本
                    versionStatusTexts.Text = ""; // 显示当前版本
                    versionStatusText.FontSize = 18;
                    versionStatusText.FontWeight = FontWeights.Bold;

                    // 隐藏进度条和标签
                    pbDown.Visibility = Visibility.Collapsed;
                    labelProgress.Visibility = Visibility.Collapsed;
                });
            }
        }
        catch (Exception ex)
        {
            // 处理异常
            Dispatcher.Invoke(() =>
            {
                versionStatusTextBlock.Text = "发生错误 " ; // 显示具体错误
                versionStatusTextBlock.FontSize = 40;
                versionStatusText.Text = "错误详细: " + ex.Message; // 显示具体错误
                versionStatusTexts.Text = "";
                pbDown.Visibility = Visibility.Collapsed;
                labelProgress.Visibility = Visibility.Collapsed;
            });
        }
    }

    private bool IsNewerVersion(string remoteVersion, string currentVersion)
    {
        return Version.Parse(remoteVersion) > Version.Parse(currentVersion);
    }

    private string GetCurrentVersion()
    {
        // 获取当前应用版本
        return App.FullAppVersion; // 使用完整版本信息
    }
}


