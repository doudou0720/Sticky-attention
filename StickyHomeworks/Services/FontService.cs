using System;
using System.Windows;
using System.Windows.Media;

namespace StickyHomeworks.Services;

/// <summary>
/// 字体服务，用于管理应用程序中的字体资源
/// </summary>
public static class FontService
{
    private static FontFamily? _defaultFontFamily;
    private static FontFamily? _monoFontFamily;
    private static bool _isInitialized = false;

    /// <summary>
    /// 获取默认字体
    /// </summary>
    public static FontFamily DefaultFontFamily
    {
        get
        {
            if (!_isInitialized)
                Initialize();
            return _defaultFontFamily ?? new FontFamily("Microsoft YaHei UI");
        }
    }

    /// <summary>
    /// 获取等宽字体
    /// </summary>
    public static FontFamily MonoFontFamily
    {
        get
        {
            if (!_isInitialized)
                Initialize();
            return _monoFontFamily ?? new FontFamily("Consolas");
        }
    }

    /// <summary>
    /// 初始化字体服务
    /// </summary>
    private static void Initialize()
    {
        if (_isInitialized)
            return;

        try
        {
            // 尝试加载自定义字体
            _defaultFontFamily = (FontFamily)Application.Current.FindResource("LXGWWenKaiScreen");
        }
        catch (Exception ex)
        {
            // 如果自定义字体加载失败，使用系统默认中文字体
            System.Diagnostics.Debug.WriteLine($"Failed to load LXGWWenKaiScreen font: {ex.Message}");
            _defaultFontFamily = new FontFamily("Microsoft YaHei UI");
        }

        try
        {
            // 尝试加载等宽字体
            _monoFontFamily = (FontFamily)Application.Current.FindResource("LXGWWenKaiMono");
        }
        catch (Exception ex)
        {
            // 如果等宽字体加载失败，使用系统默认等宽字体
            System.Diagnostics.Debug.WriteLine($"Failed to load LXGWWenKaiMono font: {ex.Message}");
            _monoFontFamily = new FontFamily("Consolas");
        }

        _isInitialized = true;
    }

    /// <summary>
    /// 检查指定字体资源是否可用
    /// </summary>
    /// <param name="fontResourceKey">字体资源键名</param>
    /// <returns>如果字体可用返回true，否则返回false</returns>
    public static bool IsFontAvailable(string fontResourceKey)
    {
        try
        {
            var font = Application.Current.FindResource(fontResourceKey) as FontFamily;
            return font != null;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 获取指定资源键的字体，如果不可用则返回指定的默认字体
    /// </summary>
    /// <param name="fontResourceKey">字体资源键名</param>
    /// <param name="fallbackFont">回退字体</param>
    /// <returns>可用的字体或回退字体</returns>
    public static FontFamily GetFontFamilyOrDefault(string fontResourceKey, FontFamily fallbackFont)
    {
        try
        {
            var font = Application.Current.FindResource(fontResourceKey) as FontFamily;
            return font ?? fallbackFont;
        }
        catch
        {
            return fallbackFont;
        }
    }
}