using System;
using System.Windows;
using XDisplay;
using XDisplay.Geometry.Containers;

namespace XDisplay.Test
{
    class Program
    {
        static void TestMain(string[] args)
        {
            Console.WriteLine("XDisplay分层坐标系系统测试");
            Console.WriteLine("========================");
            
            // 测试分层坐标系的创建
            TestHierarchicalCoordinateSystem();
            
            // 测试分层容器的创建
            TestHierarchicalContainer();
            
            // 测试悬停功能
            HoverTest.TestHoverFunctionality();
            
            // 测试控制点状态功能
            ControlPointTest.TestControlPointStates();
            
            // 测试点击解除选中功能
            ClickTest.TestClickUnselect();
            
            // 测试控制点状态重置功能
            ControlPointStateTest.TestControlPointStateReset();
            
            Console.WriteLine("\n所有测试完成！");
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
            
            Console.WriteLine("  坐标系结构:");
            Console.WriteLine(worldCoordinate.PrintTree());
            
            // 测试坐标转换
            Point localPoint = new Point(10, 5);
            Point worldPoint = vehicle1A.ToRoot(localPoint);
            Point backToLocal = vehicle1A.FromRoot(worldPoint);
            
            Console.WriteLine($"  载具A本地坐标({localPoint.X}, {localPoint.Y})");
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
            
            Console.WriteLine($"  容器结构:");
            Console.WriteLine($"    {worldContainer.FullContainerPath}");
            Console.WriteLine($"    {trackContainer.FullContainerPath}");
            Console.WriteLine($"    {vehicleContainer.FullContainerPath}");
            
            Console.WriteLine($"  容器层级:");
            Console.WriteLine($"    世界容器层级: {worldContainer.ContainerLevel}");
            Console.WriteLine($"    轨道容器层级: {trackContainer.ContainerLevel}");
            Console.WriteLine($"    载具容器层级: {vehicleContainer.ContainerLevel}");
        }
    }
}