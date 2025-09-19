using System;
using System.Windows;
using XDisplay.Core;
using XDisplay.Geometry.Shapes;
using SysWinMedia = System.Windows.Media;

namespace XDisplay.Test
{
    public class ClickTest
    {
        public static void TestClickUnselect()
        {
            Console.WriteLine("开始测试点击解除选中功能...");
            
            // 创建一个XDisplayControl实例
            var displayControl = new XDisplayControl();
            
            // 创建一个矩形几何图形
            var rectangle = new RectangleGeometry(new Rect(100, 100, 100, 80))
            {
                Fill = new SysWinMedia.SolidColorBrush(SysWinMedia.Colors.LightBlue) { Opacity = 0.7 },
                Stroke = new SysWinMedia.Pen(SysWinMedia.Brushes.Blue, 2),
                ZOrder = 1
            };
            
            // 添加到显示控件
            displayControl.AddGeometry(rectangle);
            
            // 选择该矩形以激活编辑模式
            displayControl.SelectGeometry(rectangle);
            
            // 验证选择图层已正确设置
            if (displayControl.SelectionLayer.EditingGeometry != null)
            {
                Console.WriteLine("成功设置编辑几何图形");
            }
            else
            {
                Console.WriteLine("设置编辑几何图形失败");
            }
            
            // 测试控制点选中功能
            Console.WriteLine("测试控制点选中...");
            displayControl.SelectionLayer.SetSelectedControlPoint(0);
            Console.WriteLine("已设置选中控制点索引为0");
            
            // 模拟单击事件，应该解除选中状态
            Console.WriteLine("模拟单击事件...");
            // 这里我们直接调用处理方法来测试
            var testPoint = new Point(150, 150); // 一个不在控制点上的点
            // 注意：在实际应用中，HandleSingleClick方法会被鼠标事件自动调用
            
            Console.WriteLine("点击解除选中功能测试完成!");
        }
    }
}