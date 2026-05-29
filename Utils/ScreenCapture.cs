using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PanoramaFuturesAI.Utils;

/// <summary>
/// UI 截图工具类
/// </summary>
public static class ScreenCapture
{
    /// <summary>
    /// 获取临时截图文件路径（保存在项目根目录 temp 文件夹）
    /// </summary>
    public static string GetTempImagePath()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var tempPath = Path.Combine(baseDir, "temp");

        if (!Directory.Exists(tempPath))
            Directory.CreateDirectory(tempPath);

        return Path.Combine(tempPath, $"strategy_{DateTime.Now:yyyyMMdd_HHmmss}.png");
    }

    /// <summary>
    /// 将 UI 元素截图并保存为 PNG 图片
    /// 使用元素的实际渲染尺寸进行截图
    /// </summary>
    public static string CaptureToFile(FrameworkElement element, string filePath, int scale = 2)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // 如果在 ScrollViewer 中，先滚动到顶部
        var scrollViewer = FindParentScrollViewer(element);
        double savedVerticalOffset = 0;

        if (scrollViewer != null)
        {
            savedVerticalOffset = scrollViewer.VerticalOffset;
            scrollViewer.ScrollToTop();
        }

        // 等待图表等控件完成渲染
        element.UpdateLayout();
        System.Windows.Application.Current?.Dispatcher.Invoke(
            () => { },
            System.Windows.Threading.DispatcherPriority.Loaded);
        System.Threading.Thread.Sleep(50); // 短暂等待图表渲染

        // 获取实际尺寸
        double width = element.ActualWidth;
        double height = element.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            width = 600;
            height = 700;
        }

        var pixelWidth = (int)(width * scale);
        var pixelHeight = (int)(height * scale);

        // 创建渲染目标
        var renderBitmap = new RenderTargetBitmap(
            pixelWidth, pixelHeight,
            96 * scale, 96 * scale,
            PixelFormats.Pbgra32);

        // 使用 DrawingVisual 直接绘制内容
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            // 使用 VisualBrush 填充整个目标区域
            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var rect = new Rect(0, 0, pixelWidth, pixelHeight);
            context.DrawRectangle(brush, null, rect);
        }

        renderBitmap.Render(drawingVisual);

        // 恢复滚动位置
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(savedVerticalOffset);
        }

        // 确保目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // 编码并保存
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = new FileStream(filePath, FileMode.Create);
        encoder.Save(stream);

        return filePath;
    }

    /// <summary>
    /// 将 UI 元素截图并返回字节数组
    /// </summary>
    public static byte[] CaptureToBytes(FrameworkElement element, int scale = 2)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        // 如果在 ScrollViewer 中，先滚动到顶部
        var scrollViewer = FindParentScrollViewer(element);
        double savedVerticalOffset = 0;

        if (scrollViewer != null)
        {
            savedVerticalOffset = scrollViewer.VerticalOffset;
            scrollViewer.ScrollToTop();
        }

        // 等待图表等控件完成渲染
        element.UpdateLayout();
        System.Windows.Application.Current?.Dispatcher.Invoke(
            () => { },
            System.Windows.Threading.DispatcherPriority.Loaded);
        System.Threading.Thread.Sleep(50);

        double width = element.ActualWidth;
        double height = element.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            width = 600;
            height = 700;
        }

        var pixelWidth = (int)(width * scale);
        var pixelHeight = (int)(height * scale);

        var renderBitmap = new RenderTargetBitmap(
            pixelWidth, pixelHeight,
            96 * scale, 96 * scale,
            PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            var brush = new VisualBrush(element)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var rect = new Rect(0, 0, pixelWidth, pixelHeight);
            context.DrawRectangle(brush, null, rect);
        }

        renderBitmap.Render(drawingVisual);

        // 恢复滚动位置
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(savedVerticalOffset);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// 查找父级 ScrollViewer
    /// </summary>
    private static ScrollViewer? FindParentScrollViewer(FrameworkElement element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (parent is ScrollViewer sv)
                return sv;
            if (parent is FrameworkElement fe)
                parent = VisualTreeHelper.GetParent(fe);
            else
                break;
        }
        return null;
    }
}
