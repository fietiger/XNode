using System;
using System.Windows;
using XDisplay.Core;
using XDisplay.Geometry.Shapes;
using XDisplay.Layers;
using SysWinMedia = System.Windows.Media;

namespace XDisplay.Test
{
    public class ControlPointStateTest
    {
        public static void TestControlPointStateReset()
        {
            Console.WriteLine("开始测试控制点状态重置功能...");
            
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
            
            // 模拟用户操作：选中并拖拽控制点
            Console.WriteLine("模拟控制点操作...");
            displayControl.SelectionLayer.SetSelectedControlPoint(0);
            displayControl.SelectionLayer.SetDraggedControlPoint(0);
            
            // 验证控制点状态（通过SelectionLayer的属性）
            Console.WriteLine($"操作前 - 悬停控制点索引: {GetHoveredControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"操作前 - 选中控制点索引: {GetSelectedControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"操作前 - 拖拽控制点索引: {GetDraggedControlPointIndex(displayControl.SelectionLayer)}");
            
            // 模拟完成拖拽操作（应该清除控制点状态）
            // 直接调用EndDragOperation方法来模拟鼠标释放
            var endDragMethod = typeof(XDisplayControl).GetMethod("EndDragOperation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            endDragMethod?.Invoke(displayControl, new object[] { new Point(150, 150) });
            
            // 验证控制点状态是否已清除
            Console.WriteLine($"操作后 - 悬停控制点索引: {GetHoveredControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"操作后 - 选中控制点索引: {GetSelectedControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"操作后 - 拖拽控制点索引: {GetDraggedControlPointIndex(displayControl.SelectionLayer)}");
            
            // 再次选择几何图形
            displayControl.SelectGeometry(rectangle);
            
            // 验证控制点状态是否正确重置
            Console.WriteLine($"再次选择后 - 悬停控制点索引: {GetHoveredControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"再次选择后 - 选中控制点索引: {GetSelectedControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"再次选择后 - 拖拽控制点索引: {GetDraggedControlPointIndex(displayControl.SelectionLayer)}");
            
            // 测试清除选择功能
            Console.WriteLine("测试清除选择功能...");
            displayControl.ClearSelection();
            Console.WriteLine($"清除选择后 - 悬停控制点索引: {GetHoveredControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"清除选择后 - 选中控制点索引: {GetSelectedControlPointIndex(displayControl.SelectionLayer)}");
            Console.WriteLine($"清除选择后 - 拖拽控制点索引: {GetDraggedControlPointIndex(displayControl.SelectionLayer)}");
            
            Console.WriteLine("控制点状态重置测试完成!");
        }
        
        // 通过反射获取SelectionLayer私有字段的值（仅用于测试）
        private static int GetHoveredControlPointIndex(SelectionLayer selectionLayer)
        {
            var field = typeof(SelectionLayer).GetField("_hoveredControlPointIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)(field?.GetValue(selectionLayer) ?? -1);
        }
        
        private static int GetSelectedControlPointIndex(SelectionLayer selectionLayer)
        {
            var field = typeof(SelectionLayer).GetField("_selectedControlPointIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)(field?.GetValue(selectionLayer) ?? -1);
        }
        
        private static int GetDraggedControlPointIndex(SelectionLayer selectionLayer)
        {
            var field = typeof(SelectionLayer).GetField("_draggedControlPointIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)(field?.GetValue(selectionLayer) ?? -1);
        }
    }
}