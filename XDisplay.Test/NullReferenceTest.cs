using System;
using System.Windows;
using XDisplay.Core;
using XDisplay.Geometry.Shapes;
using XDisplay.Layers;
using SysWinMedia = System.Windows.Media;

namespace XDisplay.Test
{
    public class NullReferenceTest
    {
        public static void TestNullReferenceFix()
        {
            Console.WriteLine("开始测试空引用异常修复...");
            
            // 创建一个SelectionLayer实例
            var selectionLayer = new SelectionLayer();
            
            // 创建一个矩形几何图形
            var rectangle = new RectangleGeometry(new Rect(100, 100, 100, 80))
            {
                Fill = new SysWinMedia.SolidColorBrush(SysWinMedia.Colors.LightBlue) { Opacity = 0.7 },
                Stroke = new SysWinMedia.Pen(SysWinMedia.Brushes.Blue, 2),
                ZOrder = 1
            };
            
            // 设置编辑几何图形
            selectionLayer.EditingGeometry = rectangle;
            
            // 触发选择状态改变事件（模拟几何图形被取消选择）
            rectangle.IsSelected = false;
            
            // 再次设置编辑几何图形为null
            selectionLayer.EditingGeometry = null;
            
            // 再次触发选择状态改变事件（应该不会抛出空引用异常）
            rectangle.IsSelected = true;
            
            Console.WriteLine("空引用异常修复测试完成，未发生异常！");
        }
    }
}