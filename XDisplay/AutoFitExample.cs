using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XDisplay.Core;
using XDisplay.Geometry.Shapes;

namespace XDisplay
{
    /// <summary>
    /// 自动全局显示功能使用示例
    /// 演示如何使用XDisplayControl的AutoFitToAllContent功能
    /// </summary>
    public static class AutoFitExample
    {
        /// <summary>
        /// 演示基本的自动适应功能
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        public static void DemonstrateBasicAutoFit(XDisplayControl displayControl)
        {
            // 清空现有内容
            displayControl.ClearGeometries();
            displayControl.ClearImages();

            // 添加一些几何图形
            displayControl.CreateRectangle(new Rect(100, 100, 200, 150));
            displayControl.CreateCircle(new Point(500, 300), 80);
            displayControl.CreateLine(new Point(0, 0), new Point(300, 200));

            // 自动适应所有内容到视口
            displayControl.AutoFitToAllContent();
        }

        /// <summary>
        /// 演示带图像的自动适应功能
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        /// <param name="imagePath">图像文件路径</param>
        public static void DemonstrateAutoFitWithImages(XDisplayControl displayControl, string imagePath)
        {
            // 清空现有内容
            displayControl.ClearGeometries();
            displayControl.ClearImages();

            try
            {
                // 添加图像
                BitmapImage bitmap = new BitmapImage(new Uri(imagePath));
                displayControl.AddImage(bitmap, new Point(0, 0), new Size(300, 200));

                // 添加一些几何图形在图像周围
                displayControl.CreateRectangle(new Rect(-50, -50, 100, 100));
                displayControl.CreateCircle(new Point(400, 250), 60);

                // 自动适应所有内容（包括图像和几何图形）
                displayControl.AutoFitToAllContent(30); // 使用30像素边距
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载图像失败: {ex.Message}");
                // 回退到仅几何图形的演示
                DemonstrateBasicAutoFit(displayControl);
            }
        }

        /// <summary>
        /// 演示自动适应的实时更新
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        public static void DemonstrateRealTimeAutoFit(XDisplayControl displayControl)
        {
            // 清空现有内容
            displayControl.ClearGeometries();
            displayControl.ClearImages();

            // 订阅内容变化事件以实现自动适应
            displayControl.ContentChanged += (sender, args) =>
            {
                // 内容变化时自动适应
                if (displayControl.HasVisibleContent)
                {
                    displayControl.AutoFitToAllContent();
                }
            };

            // 动态添加内容来测试实时自动适应
            displayControl.CreateRectangle(new Rect(0, 0, 100, 100));
            
            // 稍后添加更多内容（模拟动态场景）
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            
            int step = 0;
            timer.Tick += (sender, args) =>
            {
                step++;
                switch (step)
                {
                    case 1:
                        displayControl.CreateCircle(new Point(200, 200), 50);
                        break;
                    case 2:
                        displayControl.CreateLine(new Point(-100, -100), new Point(300, 300));
                        break;
                    case 3:
                        displayControl.CreateRotatedRectangle(new Rect(150, 50, 80, 120), 45);
                        break;
                    default:
                        timer.Stop();
                        break;
                }
            };
            
            timer.Start();
        }

        /// <summary>
        /// 演示获取内容边界信息
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        public static void DemonstrateContentBoundsInfo(XDisplayControl displayControl)
        {
            // 获取当前所有内容的边界
            Rect contentBounds = displayControl.GetAllContentBounds();
            
            if (contentBounds.IsEmpty)
            {
                Console.WriteLine("当前没有可见内容");
            }
            else
            {
                Console.WriteLine($"内容边界信息:");
                Console.WriteLine($"  位置: ({contentBounds.X:F2}, {contentBounds.Y:F2})");
                Console.WriteLine($"  尺寸: {contentBounds.Width:F2} x {contentBounds.Height:F2}");
                Console.WriteLine($"  总面积: {contentBounds.Width * contentBounds.Height:F2}");
                
                // 检查是否有可见内容
                Console.WriteLine($"有可见内容: {displayControl.HasVisibleContent}");
            }
        }

        /// <summary>
        /// 演示自定义边距的自动适应
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        public static void DemonstrateCustomMarginAutoFit(XDisplayControl displayControl)
        {
            // 创建一些内容
            displayControl.ClearGeometries();
            displayControl.CreateRectangle(new Rect(0, 0, 100, 100));
            displayControl.CreateCircle(new Point(150, 150), 30);

            // 演示不同边距设置的效果
            Console.WriteLine("演示不同边距的自动适应效果:");
            
            // 小边距
            Console.WriteLine("应用10像素边距...");
            displayControl.AutoFitToAllContent(10);
            
            System.Threading.Thread.Sleep(2000); // 暂停以观察效果
            
            // 中等边距
            Console.WriteLine("应用50像素边距...");
            displayControl.AutoFitToAllContent(50);
            
            System.Threading.Thread.Sleep(2000);
            
            // 大边距
            Console.WriteLine("应用100像素边距...");
            displayControl.AutoFitToAllContent(100);
        }

        /// <summary>
        /// 创建复杂场景用于测试自动适应
        /// </summary>
        /// <param name="displayControl">XDisplay控件实例</param>
        public static void CreateComplexSceneForAutoFit(XDisplayControl displayControl)
        {
            // 清空现有内容
            displayControl.ClearGeometries();
            displayControl.ClearImages();

            // 创建分散在不同位置的内容，测试边界计算
            
            // 左上角区域
            displayControl.CreateRectangle(new Rect(-200, -150, 80, 60));
            displayControl.CreateCircle(new Point(-100, -80), 25);
            
            // 右下角区域
            displayControl.CreateRectangle(new Rect(300, 250, 120, 90));
            displayControl.CreateLine(new Point(350, 300), new Point(450, 400));
            
            // 中心区域
            displayControl.CreateRotatedRectangle(new Rect(50, 80, 100, 150), 30);
            displayControl.CreateArrowLine(new Point(0, 0), new Point(200, 100));
            
            // 边缘位置的小元素
            displayControl.CreateCircle(new Point(-300, 200), 15);
            displayControl.CreateCircle(new Point(500, -100), 20);
            
            Console.WriteLine("已创建复杂场景，准备进行自动适应...");
            
            // 应用自动适应
            displayControl.AutoFitToAllContent();
            
            // 显示边界信息
            DemonstrateContentBoundsInfo(displayControl);
        }
    }
}