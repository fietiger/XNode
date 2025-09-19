using System;
using System.Windows;
using XDisplay.Geometry.Containers;

namespace XDisplay.ConsoleTest
{
    /// <summary>
    /// 坐标系层级移动测试
    /// </summary>
    public class CoordinateSystemTest
    {
        /// <summary>
        /// 测试父坐标系移动时子坐标系是否正确跟随
        /// </summary>
        public static void TestHierarchicalMovement()
        {
            Console.WriteLine("=== 坐标系层级移动测试 ===");
            
            // 创建完整的层次结构
            var worldContainer = new HierarchicalImageContainer(
                "世界坐标系",
                new Point(0, 0),
                new Size(1000, 800),
                "设备的全局坐标系统");
            
            var trackContainer = new HierarchicalImageContainer(
                "轨道1", worldContainer,
                new Point(100, 200),
                new Size(400, 50),
                "第一条运输轨道");
            
            var vehicleContainer = new HierarchicalImageContainer(
                "载具A", trackContainer,
                new Point(50, 10),
                new Size(80, 30),
                "轨道1上的载具A");
            
            var pcbContainer = new HierarchicalImageContainer(
                "电路板1", vehicleContainer,
                new Point(20, 5),
                new Size(30, 20),
                "载具A上的电路板1");
            
            // 在电路板上定义一个点
            Point localPoint = new Point(10, 8);
            Console.WriteLine($"电路板上点的本地坐标: ({localPoint.X}, {localPoint.Y})");
            
            // 转换到世界坐标系
            Point worldPointBefore = pcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"移动前电路板上点的世界坐标: ({worldPointBefore.X:F2}, {worldPointBefore.Y:F2})");
            
            // 移动轨道（父容器）
            Console.WriteLine("\n移动轨道坐标系...");
            trackContainer.SetContainerTransform(new Point(150, 250), 1.0, 0);
            
            // 再次转换到世界坐标系
            Point worldPointAfter = pcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"移动后电路板上点的世界坐标: ({worldPointAfter.X:F2}, {worldPointAfter.Y:F2})");
            
            // 验证坐标是否正确变化
            double expectedDeltaX = 50; // 150 - 100
            double expectedDeltaY = 50; // 250 - 200
            double actualDeltaX = worldPointAfter.X - worldPointBefore.X;
            double actualDeltaY = worldPointAfter.Y - worldPointBefore.Y;
            
            Console.WriteLine($"\n预期坐标变化: ({expectedDeltaX}, {expectedDeltaY})");
            Console.WriteLine($"实际坐标变化: ({actualDeltaX:F2}, {actualDeltaY:F2})");
            
            if (Math.Abs(actualDeltaX - expectedDeltaX) < 0.01 && 
                Math.Abs(actualDeltaY - expectedDeltaY) < 0.01)
            {
                Console.WriteLine("✓ 测试通过：子坐标系正确跟随父坐标系移动");
            }
            else
            {
                Console.WriteLine("✗ 测试失败：子坐标系未正确跟随父坐标系移动");
            }
            
            // 测试移动载具
            Console.WriteLine("\n移动载具坐标系...");
            Point worldPointBefore2 = pcbContainer.TransformToRoot(localPoint);
            vehicleContainer.SetContainerTransform(new Point(100, 20), 1.0, 0);
            Point worldPointAfter2 = pcbContainer.TransformToRoot(localPoint);
            
            double actualDeltaX2 = worldPointAfter2.X - worldPointBefore2.X;
            double actualDeltaY2 = worldPointAfter2.Y - worldPointBefore2.Y;
            Console.WriteLine($"载具移动后坐标变化: ({actualDeltaX2:F2}, {actualDeltaY2:F2})");
            
            if (Math.Abs(actualDeltaX2) > 0.01 || Math.Abs(actualDeltaY2) > 0.01)
            {
                Console.WriteLine("✓ 测试通过：子坐标系正确跟随载具移动");
            }
            else
            {
                Console.WriteLine("✗ 测试失败：子坐标系未正确跟随载具移动");
            }
        }
    }
}