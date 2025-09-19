using System;
using System.Windows;
using XDisplay.Geometry.Containers;
using XDisplay.Geometry.Shapes;

namespace XDisplay
{
    /// <summary>
    /// 几何图形挂载到坐标系演示
    /// </summary>
    public class GeometryMountingDemo
    {
        /// <summary>
        /// 演示如何将几何图形挂载到坐标系下
        /// </summary>
        public static void DemonstrateGeometryMounting()
        {
            Console.WriteLine("=== 几何图形挂载到坐标系演示 ===");
            
            // 1. 创建世界坐标系（根容器）
            var worldContainer = new HierarchicalImageContainer(
                "世界坐标系",
                new Point(0, 0),
                new Size(1000, 800),
                "设备的全局坐标系统");
            
            // 2. 创建轨道坐标系
            var trackContainer = new HierarchicalImageContainer(
                "轨道1", worldContainer,
                new Point(100, 200),
                new Size(400, 50),
                "第一条运输轨道");
            
            // 3. 创建载具坐标系
            var vehicleContainer = new HierarchicalImageContainer(
                "载具A", trackContainer,
                new Point(50, 10),
                new Size(80, 30),
                "轨道1上的载具A");
            
            // 4. 创建电路板坐标系
            var pcbContainer = new HierarchicalImageContainer(
                "电路板1", vehicleContainer,
                new Point(20, 5),
                new Size(30, 20),
                "载具A上的电路板1");
            
            // 5. 在不同的坐标系下挂载几何图形
            
            // 在世界坐标系下添加一个矩形
            var worldRect = new RectangleGeometry(new Rect(0, 0, 50, 30));
            worldContainer.AddChild(worldRect, new Point(500, 400)); // 在世界坐标系中心附近
            
            // 在轨道坐标系下添加一个圆形
            var trackCircle = new CircleGeometry(new Point(0, 0), 10);
            trackContainer.AddChild(trackCircle, new Point(200, 25)); // 轨道中心
            
            // 在载具坐标系下添加一条线
            var vehicleLine = new LineGeometry(new Point(0, 0), new Point(40, 15));
            vehicleContainer.AddChild(vehicleLine, new Point(20, 10)); // 载具中心
            
            // 在电路板坐标系下添加多个几何图形
            var pcbRect = new RectangleGeometry(new Rect(0, 0, 5, 3));
            var pcbCircle = new CircleGeometry(new Point(0, 0), 2);
            var pcbLine = new LineGeometry(new Point(0, 0), new Point(10, 5));
            
            pcbContainer.AddChild(pcbRect, new Point(10, 8));   // 电路板上位置(10, 8)
            pcbContainer.AddChild(pcbCircle, new Point(20, 12)); // 电路板上位置(20, 12)
            pcbContainer.AddChild(pcbLine, new Point(5, 3));    // 电路板上位置(5, 3)
            
            Console.WriteLine("已成功在不同坐标系下挂载几何图形:");
            Console.WriteLine($"  世界坐标系下: 1个矩形");
            Console.WriteLine($"  轨道坐标系下: 1个圆形");
            Console.WriteLine($"  载具坐标系下: 1条线");
            Console.WriteLine($"  电路板坐标系下: 1个矩形, 1个圆形, 1条线");
            
            // 6. 演示坐标转换
            Console.WriteLine("\n=== 坐标转换演示 ===");
            
            // 电路板上的矩形在本地坐标系中的位置
            Point localPoint = new Point(10, 8);
            Console.WriteLine($"电路板上矩形的本地坐标: ({localPoint.X}, {localPoint.Y})");
            
            // 转换到世界坐标系
            Point worldPoint = pcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"转换到世界坐标系: ({worldPoint.X:F2}, {worldPoint.Y:F2})");
            
            // 转换到载具坐标系
            Point vehiclePoint = pcbContainer.TransformToContainer(localPoint, vehicleContainer);
            Console.WriteLine($"转换到载具坐标系: ({vehiclePoint.X:F2}, {vehiclePoint.Y:F2})");
            
            // 移动载具并查看坐标变化
            Console.WriteLine("\n=== 移动载具后的坐标变化 ===");
            Point originalWorldPos = pcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"移动前电路板上矩形的世界坐标: ({originalWorldPos.X:F2}, {originalWorldPos.Y:F2})");
            
            // 移动载具
            vehicleContainer.SetContainerTransform(new Point(100, 20), 1.0, 0);
            
            Point movedWorldPos = pcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"移动后电路板上矩形的世界坐标: ({movedWorldPos.X:F2}, {movedWorldPos.Y:F2})");
            
            // 验证子坐标系确实跟随移动
            double deltaX = movedWorldPos.X - originalWorldPos.X;
            double deltaY = movedWorldPos.Y - originalWorldPos.Y;
            Console.WriteLine($"坐标变化量: ({deltaX:F2}, {deltaY:F2})");
        }
    }
}