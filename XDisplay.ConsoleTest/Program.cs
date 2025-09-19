using System;
using System.Windows;
using XDisplay;
using XDisplay.Geometry.Containers;

namespace XDisplay.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("XDisplay分层坐标系系统测试");
            Console.WriteLine("========================");
            
            // 测试分层坐标系的创建
            TestHierarchicalCoordinateSystem();
            
            // 测试分层容器的创建
            TestHierarchicalContainer();
            
            // 测试坐标转换功能
            TestCoordinateTransformation();
            
            // 测试坐标系层级移动
            CoordinateSystemTest.TestHierarchicalMovement();
            
            // 测试几何图形挂载
            GeometryMountingDemo.DemonstrateGeometryMounting();
            
            Console.WriteLine("\n所有测试完成！");
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }
        
        static void TestHierarchicalCoordinateSystem()
        {
            Console.WriteLine("\n1. 测试分层坐标系创建...");
            
            // 创建根坐标系
            var worldCoordinate = new HierarchicalCoordinateSystem("世界坐标系", "设备的全局坐标系");
            
            // 创建子坐标系
            var track1 = new HierarchicalCoordinateSystem("轨道1", worldCoordinate, 
                new Point(100, 200), 1.0, 0, false, "第一条运输轨道");
            
            var vehicle1A = new HierarchicalCoordinateSystem("载具A", track1, 
                new Point(50, 0), 1.0, 0, false, "轨道1上的载具A");
            
            var pcb1A = new HierarchicalCoordinateSystem("电路板1", vehicle1A, 
                new Point(20, 10), 1.0, 0, true, "载具A上的主电路板");
            
            var subPcb1A1 = new HierarchicalCoordinateSystem("子拼板1", pcb1A, 
                new Point(5, 5), 1.0, 0, true, "电路板1的第一个子拼板");
            
            Console.WriteLine("  坐标系结构:");
            Console.WriteLine(worldCoordinate.PrintTree());
            
            // 测试坐标转换
            Point localPoint = new Point(10, 5);
            Point worldPoint = subPcb1A1.ToRoot(localPoint);
            Point backToLocal = subPcb1A1.FromRoot(worldPoint);
            
            Console.WriteLine($"  子拼板1本地坐标({localPoint.X}, {localPoint.Y})");
            Console.WriteLine($"  转换到世界坐标({worldPoint.X:F2}, {worldPoint.Y:F2})");
            Console.WriteLine($"  反向转换({backToLocal.X:F2}, {backToLocal.Y:F2})");
            Console.WriteLine($"  转换误差: {Math.Abs(localPoint.X - backToLocal.X):F6}, {Math.Abs(localPoint.Y - backToLocal.Y):F6}");
        }
        
        static void TestHierarchicalContainer()
        {
            Console.WriteLine("\n2. 测试分层容器创建...");
            
            // 创建根容器
            var worldContainer = new HierarchicalImageContainer(
                "世界坐标系", 
                new Point(0, 0), 
                new Size(1000, 800), 
                "设备的全局坐标系统");
            
            // 创建子容器
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
            
            Console.WriteLine($"  容器结构:");
            Console.WriteLine($"    {worldContainer.FullContainerPath}");
            Console.WriteLine($"    {trackContainer.FullContainerPath}");
            Console.WriteLine($"    {vehicleContainer.FullContainerPath}");
            Console.WriteLine($"    {pcbContainer.FullContainerPath}");
            
            Console.WriteLine($"  容器层级:");
            Console.WriteLine($"    世界容器层级: {worldContainer.ContainerLevel}");
            Console.WriteLine($"    轨道容器层级: {trackContainer.ContainerLevel}");
            Console.WriteLine($"    载具容器层级: {vehicleContainer.ContainerLevel}");
            Console.WriteLine($"    电路板容器层级: {pcbContainer.ContainerLevel}");
        }
        
        static void TestCoordinateTransformation()
        {
            Console.WriteLine("\n3. 测试坐标转换功能...");
            
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
            
            var subPcbContainer = new HierarchicalImageContainer(
                "子拼板1", pcbContainer, 
                new Point(5, 5), 
                new Size(10, 8), 
                "电路板1的子拼板");
            
            // 在子拼板上定义一个点
            Point localPoint = new Point(2, 3);
            Console.WriteLine($"  子拼板1本地坐标: ({localPoint.X:F2}, {localPoint.Y:F2})");
            
            // 转换到世界坐标系
            Point worldPoint = subPcbContainer.TransformToRoot(localPoint);
            Console.WriteLine($"  转换到世界坐标系: ({worldPoint.X:F2}, {worldPoint.Y:F2})");
            
            // 转换到载具坐标系
            Point vehiclePoint = subPcbContainer.TransformToContainer(localPoint, vehicleContainer);
            Console.WriteLine($"  转换到载具坐标系: ({vehiclePoint.X:F2}, {vehiclePoint.Y:F2})");
            
            // 转换到轨道坐标系
            Point trackPoint = subPcbContainer.TransformToContainer(localPoint, trackContainer);
            Console.WriteLine($"  转换到轨道坐标系: ({trackPoint.X:F2}, {trackPoint.Y:F2})");
            
            // 验证反向转换
            Point backToOriginal = vehicleContainer.TransformToContainer(vehiclePoint, subPcbContainer);
            Console.WriteLine($"  反向转换回原坐标系: ({backToOriginal.X:F2}, {backToOriginal.Y:F2})");
            
            double errorX = Math.Abs(backToOriginal.X - localPoint.X);
            double errorY = Math.Abs(backToOriginal.Y - localPoint.Y);
            Console.WriteLine($"  转换精度误差: ({errorX:F6}, {errorY:F6})");
        }
    }
}