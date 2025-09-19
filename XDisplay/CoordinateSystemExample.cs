using System;
using System.Linq;
using System.Windows;
using XDisplay.Geometry.Containers;

namespace XDisplay
{
    /// <summary>
    /// 多层嵌套坐标系示例 - 演示设备坐标系、轨道坐标系、载具坐标系、电路板坐标系、子拼板坐标系的层次结构
    /// </summary>
    public class CoordinateSystemExample
    {
        /// <summary>
        /// 创建您描述的多层坐标系结构
        /// </summary>
        /// <returns>根坐标系（世界/设备坐标系）</returns>
        public static HierarchicalCoordinateSystem CreateMultiLayerCoordinateSystem()
        {
            // 1. 创建世界坐标系（设备坐标系）- 根坐标系
            var worldCoordinate = new HierarchicalCoordinateSystem("世界坐标系", "设备的全局坐标系");

            // 2. 创建两个轨道坐标系
            var track1 = new HierarchicalCoordinateSystem("轨道1", worldCoordinate, 
                new Point(100, 200), 1.0, 0, false, "第一条运输轨道");
            var track2 = new HierarchicalCoordinateSystem("轨道2", worldCoordinate, 
                new Point(500, 200), 1.0, 15, false, "第二条运输轨道，旋转15度");

            // 3. 在轨道1上创建载具坐标系
            var vehicle1A = new HierarchicalCoordinateSystem("载具A", track1, 
                new Point(50, 0), 1.0, 0, false, "轨道1上的载具A");
            var vehicle1B = new HierarchicalCoordinateSystem("载具B", track1, 
                new Point(150, 0), 1.0, 0, false, "轨道1上的载具B");

            // 4. 在轨道2上创建载具坐标系
            var vehicle2A = new HierarchicalCoordinateSystem("载具C", track2, 
                new Point(80, 0), 1.0, 0, false, "轨道2上的载具C");

            // 5. 在载具A上创建两个电路板坐标系
            var pcb1A = new HierarchicalCoordinateSystem("电路板1", vehicle1A, 
                new Point(20, 10), 1.0, 0, true, "载具A上的主电路板，Y轴向上");
            var pcb2A = new HierarchicalCoordinateSystem("电路板2", vehicle1A, 
                new Point(20, -10), 1.0, 0, true, "载具A上的辅助电路板");

            // 6. 在载具B上创建电路板坐标系
            var pcb1B = new HierarchicalCoordinateSystem("电路板1", vehicle1B, 
                new Point(15, 0), 1.0, 90, true, "载具B上的电路板，旋转90度");

            // 7. 在载具C上创建电路板坐标系
            var pcb1C = new HierarchicalCoordinateSystem("电路板1", vehicle2A, 
                new Point(25, 5), 0.5, 0, true, "载具C上的小型电路板，缩放0.5倍");

            // 8. 在电路板上创建子拼板坐标系
            var subPcb1A1 = new HierarchicalCoordinateSystem("子拼板1", pcb1A, 
                new Point(5, 5), 1.0, 0, true, "电路板1A的第一个子拼板");
            var subPcb1A2 = new HierarchicalCoordinateSystem("子拼板2", pcb1A, 
                new Point(15, 5), 1.0, 0, true, "电路板1A的第二个子拼板");
            var subPcb1A3 = new HierarchicalCoordinateSystem("子拼板3", pcb1A, 
                new Point(10, 15), 1.0, 45, true, "电路板1A的第三个子拼板，旋转45度");

            var subPcb2A1 = new HierarchicalCoordinateSystem("子拼板1", pcb2A, 
                new Point(8, 8), 1.0, 0, true, "电路板2A的子拼板");

            var subPcb1B1 = new HierarchicalCoordinateSystem("子拼板1", pcb1B, 
                new Point(6, 6), 1.0, 0, true, "电路板1B的子拼板");

            var subPcb1C1 = new HierarchicalCoordinateSystem("子拼板1", pcb1C, 
                new Point(4, 4), 1.0, 0, true, "电路板1C的子拼板");

            return worldCoordinate;
        }

        /// <summary>
        /// 演示坐标转换功能
        /// </summary>
        public static void DemonstrateCoordinateTransformation()
        {
            // 创建多层坐标系
            var worldCoordinate = CreateMultiLayerCoordinateSystem();
            
            Console.WriteLine("=== 多层嵌套坐标系结构 ===");
            Console.WriteLine(worldCoordinate.PrintTree());
            
            // 获取具体的坐标系
            var subPcb = worldCoordinate.FindByPath("世界坐标系/轨道1/载具A/电路板1/子拼板1");
            var anotherSubPcb = worldCoordinate.FindByPath("世界坐标系/轨道2/载具C/电路板1/子拼板1");
            
            if (subPcb != null && anotherSubPcb != null)
            {
                Console.WriteLine("=== 坐标转换示例 ===");
                
                // 在子拼板1上定义一个点
                Point localPoint = new Point(2, 3);
                Console.WriteLine($"子拼板1本地坐标: ({localPoint.X:F2}, {localPoint.Y:F2})");
                
                // 转换到世界坐标系
                Point worldPoint = subPcb.ToRoot(localPoint);
                Console.WriteLine($"转换到世界坐标系: ({worldPoint.X:F2}, {worldPoint.Y:F2})");
                
                // 转换到另一个子拼板的坐标系
                Point anotherLocalPoint = subPcb.TransformTo(localPoint, anotherSubPcb);
                Console.WriteLine($"转换到另一个子拼板坐标系: ({anotherLocalPoint.X:F2}, {anotherLocalPoint.Y:F2})");
                
                // 验证反向转换
                Point backToOriginal = anotherSubPcb.TransformTo(anotherLocalPoint, subPcb);
                Console.WriteLine($"反向转换回原坐标系: ({backToOriginal.X:F2}, {backToOriginal.Y:F2})");
                
                Console.WriteLine($"转换精度误差: ({Math.Abs(backToOriginal.X - localPoint.X):F6}, {Math.Abs(backToOriginal.Y - localPoint.Y):F6})");
            }
        }

        /// <summary>
        /// 演示坐标系查找功能
        /// </summary>
        public static void DemonstrateCoordinateSystemSearch()
        {
            var worldCoordinate = CreateMultiLayerCoordinateSystem();
            
            Console.WriteLine("=== 坐标系查找示例 ===");
            
            // 通过路径查找
            var pcb = worldCoordinate.FindByPath("世界坐标系/轨道1/载具A/电路板1");
            if (pcb != null)
            {
                Console.WriteLine($"通过路径找到: {pcb.FullPath}");
                Console.WriteLine($"坐标系层级: {pcb.Level}");
                Console.WriteLine($"描述: {pcb.Description}");
            }
            
            // 递归查找
            var allSubPcbs = worldCoordinate.GetAllDescendants()
                .Where(cs => cs.Name.StartsWith("子拼板"))
                .ToList();
            
            Console.WriteLine($"找到 {allSubPcbs.Count} 个子拼板坐标系:");
            foreach (var subPcb in allSubPcbs)
            {
                Console.WriteLine($"  - {subPcb.FullPath}");
            }
        }

        /// <summary>
        /// 演示动态坐标系管理
        /// </summary>
        public static void DemonstrateDynamicCoordinateManagement()
        {
            var worldCoordinate = CreateMultiLayerCoordinateSystem();
            
            Console.WriteLine("=== 动态坐标系管理示例 ===");
            
            // 获取轨道1
            var track1 = worldCoordinate.FindChild("轨道1");
            if (track1 != null)
            {
                // 动态添加新载具
                var newVehicle = new HierarchicalCoordinateSystem("载具D", track1, 
                    new Point(250, 0), 1.0, 0, false, "动态添加的新载具");
                
                Console.WriteLine($"动态添加载具: {newVehicle.FullPath}");
                
                // 在新载具上添加电路板
                var newPcb = new HierarchicalCoordinateSystem("电路板1", newVehicle, 
                    new Point(20, 0), 1.0, 0, true, "新载具的电路板");
                
                Console.WriteLine($"动态添加电路板: {newPcb.FullPath}");
                
                // 移动载具（修改坐标系变换）
                Console.WriteLine("移动载具前的位置变换:");
                Console.WriteLine($"  原点: ({newVehicle.LocalCoordinateSystem.Origin.X}, {newVehicle.LocalCoordinateSystem.Origin.Y})");
                
                newVehicle.SetTransform(new Point(300, 10), 1.0, 30); // 移动并旋转
                
                Console.WriteLine("移动载具后的位置变换:");
                Console.WriteLine($"  原点: ({newVehicle.LocalCoordinateSystem.Origin.X}, {newVehicle.LocalCoordinateSystem.Origin.Y})");
                Console.WriteLine($"  旋转: {newVehicle.LocalCoordinateSystem.Rotation}度");
            }
        }
    }
}