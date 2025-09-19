using System;
using System.Windows;
using XDisplay.Core;
using XDisplay.Geometry.Shapes;
using SysWinMedia = System.Windows.Media;

namespace XDisplay.Test
{
    public class HoverTest
    {
        public static void TestHoverFunctionality()
        {
            Console.WriteLine("开始测试悬停功能...");
            
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
            
            // 测试控制点悬停功能
            // 获取控制点位置进行测试
            var controlPoints = rectangle.GetControlPoints();
            if (controlPoints.Length > 0)
            {
                // 测试命中测试功能
                var hitResult = displayControl.SelectionLayer.HitTestControlPoint(controlPoints[0]);
                Console.WriteLine($"控制点命中测试结果: {hitResult}");
                
                // 测试悬停设置功能
                displayControl.SelectionLayer.SetHoveredControlPoint(0);
                Console.WriteLine("已设置悬停控制点索引为0");
                
                // 测试拖拽设置功能
                displayControl.SelectionLayer.SetDraggedControlPoint(0);
                Console.WriteLine("已设置拖拽控制点索引为0");
                
                // 测试清除功能
                displayControl.SelectionLayer.SetHoveredControlPoint(-1);
                displayControl.SelectionLayer.SetDraggedControlPoint(-1);
                Console.WriteLine("已清除悬停和拖拽状态");
            }
            
            Console.WriteLine("悬停功能测试完成!");
        }
    }
}