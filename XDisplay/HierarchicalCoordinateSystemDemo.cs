using System;
using System.Linq;
using System.Windows;
using XDisplay.Geometry.Containers;
using XDisplay.Geometry.Shapes;

namespace XDisplay
{
    /// <summary>
    /// 分层坐标系统演示 - 完整演示您所需的多层嵌套坐标系结构
    /// 世界坐标系 -> 轨道坐标系 -> 载具坐标系 -> 电路板坐标系 -> 子拼板坐标系
    /// </summary>
    public class HierarchicalCoordinateSystemDemo
    {
        /// <summary>
        /// 创建完整的多层坐标系结构
        /// </summary>
        /// <returns>根容器（世界坐标系）</returns>
        public static HierarchicalImageContainer CreateCompleteHierarchy()
        {
            // 1. 创建世界坐标系（设备坐标系）- 根容器
            var worldContainer = new HierarchicalImageContainer(
                "世界坐标系", 
                new Point(0, 0), 
                new Size(1000, 800), 
                "设备的全局坐标系统");

            // 2. 创建两个轨道坐标系
            var track1Container = new HierarchicalImageContainer(
                "轨道1", worldContainer, 
                new Point(100, 200), 
                new Size(400, 50), 
                "第一条运输轨道");

            var track2Container = new HierarchicalImageContainer(
                "轨道2", worldContainer, 
                new Point(100, 400), 
                new Size(400, 50), 
                "第二条运输轨道");

            // 将轨道添加到世界坐标系
            worldContainer.AddChildContainer(track1Container, new Point(100, 200));
            worldContainer.AddChildContainer(track2Container, new Point(100, 400));

            // 3. 在轨道1上创建载具坐标系
            var vehicle1A = new HierarchicalImageContainer(
                "载具A", track1Container, 
                new Point(50, 10), 
                new Size(80, 30), 
                "轨道1上的载具A");

            var vehicle1B = new HierarchicalImageContainer(
                "载具B", track1Container, 
                new Point(200, 10), 
                new Size(80, 30), 
                "轨道1上的载具B");

            track1Container.AddChildContainer(vehicle1A, new Point(50, 10));
            track1Container.AddChildContainer(vehicle1B, new Point(200, 10));

            // 4. 在轨道2上创建载具坐标系
            var vehicle2A = new HierarchicalImageContainer(
                "载具C", track2Container, 
                new Point(150, 10), 
                new Size(80, 30), 
                "轨道2上的载具C");

            track2Container.AddChildContainer(vehicle2A, new Point(150, 10));

            // 5. 在载具A上创建两个电路板坐标系
            var pcb1A = new HierarchicalImageContainer(
                "电路板1", vehicle1A, 
                new Point(10, 5), 
                new Size(30, 20), 
                "载具A上的主电路板");

            var pcb2A = new HierarchicalImageContainer(
                "电路板2", vehicle1A, 
                new Point(45, 5), 
                new Size(30, 20), 
                "载具A上的辅助电路板");

            vehicle1A.AddChildContainer(pcb1A, new Point(10, 5));
            vehicle1A.AddChildContainer(pcb2A, new Point(45, 5));

            // 6. 在载具B上创建电路板坐标系
            var pcb1B = new HierarchicalImageContainer(
                "电路板1", vehicle1B, 
                new Point(25, 5), 
                new Size(30, 20), 
                "载具B上的电路板");

            vehicle1B.AddChildContainer(pcb1B, new Point(25, 5));

            // 7. 在载具C上创建电路板坐标系
            var pcb1C = new HierarchicalImageContainer(
                "电路板1", vehicle2A, 
                new Point(25, 5), 
                new Size(30, 20), 
                "载具C上的电路板");

            vehicle2A.AddChildContainer(pcb1C, new Point(25, 5));

            // 8. 在电路板上创建子拼板坐标系
            // 电路板1A的子拼板
            var subPcb1A1 = new HierarchicalImageContainer(
                "子拼板1", pcb1A, 
                new Point(2, 2), 
                new Size(8, 6), 
                "电路板1A的第一个子拼板");

            var subPcb1A2 = new HierarchicalImageContainer(
                "子拼板2", pcb1A, 
                new Point(12, 2), 
                new Size(8, 6), 
                "电路板1A的第二个子拼板");

            var subPcb1A3 = new HierarchicalImageContainer(
                "子拼板3", pcb1A, 
                new Point(22, 2), 
                new Size(6, 6), 
                "电路板1A的第三个子拼板");

            pcb1A.AddChildContainer(subPcb1A1, new Point(2, 2));
            pcb1A.AddChildContainer(subPcb1A2, new Point(12, 2));
            pcb1A.AddChildContainer(subPcb1A3, new Point(22, 2));

            // 电路板2A的子拼板
            var subPcb2A1 = new HierarchicalImageContainer(
                "子拼板1", pcb2A, 
                new Point(5, 3), 
                new Size(10, 8), 
                "电路板2A的子拼板");

            pcb2A.AddChildContainer(subPcb2A1, new Point(5, 3));

            // 电路板1B的子拼板
            var subPcb1B1 = new HierarchicalImageContainer(
                "子拼板1", pcb1B, 
                new Point(8, 4), 
                new Size(12, 10), 
                "电路板1B的子拼板");

            pcb1B.AddChildContainer(subPcb1B1, new Point(8, 4));

            // 电路板1C的子拼板
            var subPcb1C1 = new HierarchicalImageContainer(
                "子拼板1", pcb1C, 
                new Point(6, 3), 
                new Size(15, 12), 
                "电路板1C的子拼板");

            pcb1C.AddChildContainer(subPcb1C1, new Point(6, 3));

            // 9. 在子拼板上添加一些几何图形作为示例
            AddGeometryElementsToSubPcbs(subPcb1A1, subPcb1A2, subPcb2A1, subPcb1B1, subPcb1C1);

            return worldContainer;
        }

        /// <summary>
        /// 在子拼板上添加几何图形元素
        /// </summary>
        private static void AddGeometryElementsToSubPcbs(params HierarchicalImageContainer[] subPcbs)
        {
            foreach (var subPcb in subPcbs)
            {
                // 添加一些示例几何图形
                var rect = new RectangleGeometry(new Rect(1, 1, 3, 2));
                var circle = new CircleGeometry(new Point(2, 4), 1);
                var line = new LineGeometry(new Point(0, 0), new Point(4, 3));

                subPcb.AddChild(rect, new Point(1, 1));
                subPcb.AddChild(circle, new Point(2, 4));
                subPcb.AddChild(line, new Point(0, 0));
            }
        }

        /// <summary>
        /// 演示坐标转换功能
        /// </summary>
        public static void DemonstrateCoordinateTransformations()
        {
            var worldContainer = CreateCompleteHierarchy();
            
            Console.WriteLine("=== 多层嵌套坐标系结构演示 ===");
            Console.WriteLine(worldContainer.HierarchicalCoordinateSystem.PrintTree());
            
            // 获取不同层级的容器
            var subPcb1A1 = worldContainer.FindContainerByPath("世界坐标系/轨道1/载具A/电路板1/子拼板1");
            var subPcb1C1 = worldContainer.FindContainerByPath("世界坐标系/轨道2/载具C/电路板1/子拼板1");
            
            if (subPcb1A1 != null && subPcb1C1 != null)
            {
                Console.WriteLine("\n=== 坐标转换演示 ===");
                
                // 在子拼板1A1上定义一个点
                Point localPoint = new Point(2, 3);
                Console.WriteLine($"子拼板1A1本地坐标: ({localPoint.X:F2}, {localPoint.Y:F2})");
                
                // 转换到世界坐标系
                Point worldPoint = subPcb1A1.TransformToRoot(localPoint);
                Console.WriteLine($"转换到世界坐标系: ({worldPoint.X:F2}, {worldPoint.Y:F2})");
                
                // 转换到载具A坐标系
                var vehicleA = worldContainer.FindContainerByPath("世界坐标系/轨道1/载具A");
                if (vehicleA != null)
                {
                    Point vehiclePoint = subPcb1A1.TransformToContainer(localPoint, vehicleA);
                    Console.WriteLine($"转换到载具A坐标系: ({vehiclePoint.X:F2}, {vehiclePoint.Y:F2})");
                }
                
                // 转换到轨道1坐标系
                var track1 = worldContainer.FindContainerByPath("世界坐标系/轨道1");
                if (track1 != null)
                {
                    Point trackPoint = subPcb1A1.TransformToContainer(localPoint, track1);
                    Console.WriteLine($"转换到轨道1坐标系: ({trackPoint.X:F2}, {trackPoint.Y:F2})");
                }
                
                // 转换到另一个子拼板的坐标系
                Point anotherSubPcbPoint = subPcb1A1.TransformToContainer(localPoint, subPcb1C1);
                Console.WriteLine($"转换到子拼板1C1坐标系: ({anotherSubPcbPoint.X:F2}, {anotherSubPcbPoint.Y:F2})");
                
                // 验证反向转换
                Point backToOriginal = subPcb1C1.TransformToContainer(anotherSubPcbPoint, subPcb1A1);
                Console.WriteLine($"反向转换回原坐标系: ({backToOriginal.X:F2}, {backToOriginal.Y:F2})");
                
                double errorX = Math.Abs(backToOriginal.X - localPoint.X);
                double errorY = Math.Abs(backToOriginal.Y - localPoint.Y);
                Console.WriteLine($"转换精度误差: ({errorX:F6}, {errorY:F6})");
            }
        }

        /// <summary>
        /// 演示动态坐标系管理
        /// </summary>
        public static void DemonstrateDynamicManagement()
        {
            var worldContainer = CreateCompleteHierarchy();
            
            Console.WriteLine("\n=== 动态坐标系管理演示 ===");
            
            // 动态添加新轨道
            var newTrack = new HierarchicalImageContainer(
                "轨道3", worldContainer,
                new Point(100, 600),
                new Size(400, 50),
                "动态添加的第三条轨道");
            
            worldContainer.AddChildContainer(newTrack, new Point(100, 600));
            Console.WriteLine($"动态添加轨道: {newTrack.FullContainerPath}");
            
            // 在新轨道上添加载具
            var newVehicle = new HierarchicalImageContainer(
                "载具D", newTrack,
                new Point(100, 10),
                new Size(80, 30),
                "新轨道上的载具");
            
            newTrack.AddChildContainer(newVehicle, new Point(100, 10));
            Console.WriteLine($"动态添加载具: {newVehicle.FullContainerPath}");
            
            // 移动载具（修改坐标系变换）
            Console.WriteLine("\n载具移动前的坐标系变换:");
            Console.WriteLine($"  层级: {newVehicle.ContainerLevel}");
            Console.WriteLine($"  位置: ({newVehicle.Position.X}, {newVehicle.Position.Y})");
            
            // 移动载具
            newVehicle.SetContainerTransform(new Point(250, 15), 1.2, 30);
            
            Console.WriteLine("载具移动后的坐标系变换:");
            Console.WriteLine($"  位置: ({newVehicle.Position.X}, {newVehicle.Position.Y})");
            Console.WriteLine($"  缩放: {newVehicle.LocalCoordinateSystem.Scale:F2}");
            Console.WriteLine($"  旋转: {newVehicle.Rotation:F1}度");
            
            // 测试移动后的坐标转换
            Point testPoint = new Point(10, 5);
            Point worldPointBefore = new Point(100 + 10, 10 + 5); // 移动前的预期世界坐标
            Point worldPointAfter = newVehicle.TransformToRoot(testPoint);
            
            Console.WriteLine($"\n载具内部点({testPoint.X}, {testPoint.Y})转换到世界坐标:");
            Console.WriteLine($"  移动前预期: ({worldPointBefore.X}, {worldPointBefore.Y})");
            Console.WriteLine($"  移动后实际: ({worldPointAfter.X:F2}, {worldPointAfter.Y:F2})");
        }

        /// <summary>
        /// 演示查找和遍历功能
        /// </summary>
        public static void DemonstrateSearchAndTraversal()
        {
            var worldContainer = CreateCompleteHierarchy();
            
            Console.WriteLine("\n=== 查找和遍历演示 ===");
            
            // 查找所有电路板
            var allContainers = worldContainer.HierarchicalCoordinateSystem.GetAllDescendants();
            var allPcbs = allContainers.Where(cs => cs.Name.StartsWith("电路板")).ToList();
            
            Console.WriteLine($"找到 {allPcbs.Count} 个电路板:");
            foreach (var pcb in allPcbs)
            {
                Console.WriteLine($"  - {pcb.FullPath} (层级: {pcb.Level})");
            }
            
            // 查找所有子拼板
            var allSubPcbs = allContainers.Where(cs => cs.Name.StartsWith("子拼板")).ToList();
            Console.WriteLine($"\n找到 {allSubPcbs.Count} 个子拼板:");
            foreach (var subPcb in allSubPcbs)
            {
                Console.WriteLine($"  - {subPcb.FullPath}");
            }
            
            // 按路径查找特定容器
            var specificContainer = worldContainer.FindContainerByPath("世界坐标系/轨道1/载具A/电路板1");
            if (specificContainer != null)
            {
                Console.WriteLine($"\n通过路径找到容器: {specificContainer.FullContainerPath}");
                Console.WriteLine($"描述: {specificContainer.ContainerDescription}");
                Console.WriteLine($"子容器数量: {specificContainer.ChildContainers.Count}");
                
                // 列出直接子容器
                Console.WriteLine("直接子容器:");
                foreach (var child in specificContainer.ChildContainers)
                {
                    Console.WriteLine($"  - {child.HierarchicalCoordinateSystem.Name}");
                }
            }
        }

        /// <summary>
        /// 演示物理坐标映射
        /// </summary>
        public static void DemonstratePhysicalCoordinateMapping()
        {
            var worldContainer = CreateCompleteHierarchy();
            
            Console.WriteLine("\n=== 物理坐标映射演示 ===");
            
            // 为电路板设置物理坐标映射（例如：1像素 = 0.1毫米）
            var pcb = worldContainer.FindContainerByPath("世界坐标系/轨道1/载具A/电路板1");
            if (pcb != null)
            {
                // 设置物理坐标映射：1像素 = 0.1毫米，物理原点在图像的(5, 5)位置
                pcb.SetPhysicalCoordinateMapping(0.1, new Point(5, 5), true);
                
                Console.WriteLine("电路板物理坐标映射设置:");
                Console.WriteLine($"  像素到物理比例: {pcb.LocalCoordinateSystem.PixelToPhysicalRatio}mm/pixel");
                Console.WriteLine($"  物理原点位置: ({pcb.LocalCoordinateSystem.PhysicalOriginInImage.X}, {pcb.LocalCoordinateSystem.PhysicalOriginInImage.Y})");
                Console.WriteLine($"  Y轴向上: {pcb.LocalCoordinateSystem.YAxisUp}");
                
                // 演示像素坐标到物理坐标的转换
                Point pixelPoint = new Point(15, 10);
                Point physicalPoint = pcb.LocalCoordinateSystem.ImagePixelToPhysical(pixelPoint);
                
                Console.WriteLine($"\n像素坐标({pixelPoint.X}, {pixelPoint.Y})转换为物理坐标:");
                Console.WriteLine($"  物理坐标: ({physicalPoint.X:F2}mm, {physicalPoint.Y:F2}mm)");
                
                // 反向转换验证
                Point backToPixel = pcb.LocalCoordinateSystem.PhysicalToImagePixel(physicalPoint);
                Console.WriteLine($"  反向转换: ({backToPixel.X:F2}, {backToPixel.Y:F2})");
            }
        }
    }
}