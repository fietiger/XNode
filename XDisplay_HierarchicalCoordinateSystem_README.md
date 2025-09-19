# 多层嵌套坐标系统使用指南

## 概述

XDisplay 现在支持完整的多层嵌套用户定义坐标系，可以实现您描述的复杂层次结构：
- 世界坐标系（设备坐标系）
- 轨道坐标系
- 载具坐标系
- 电路板坐标系
- 子拼板坐标系

## 核心组件

### 1. HierarchicalCoordinateSystem
分层坐标系管理器，提供：
- 树形层次结构管理
- 多层坐标转换
- 按名称和路径查找
- 物理单位映射

### 2. IHierarchicalContainer
分层容器接口，扩展了基础容器功能，添加：
- 分层坐标系管理
- 容器间坐标转换
- 子容器管理

### 3. HierarchicalImageContainer
具体的分层图像容器实现，支持：
- 背景图像显示
- 子几何图形管理
- 子容器嵌套
- 可视化编辑

## 使用方法

### 创建多层坐标系结构

```csharp
// 1. 创建根坐标系（世界/设备坐标系）
var worldContainer = new HierarchicalImageContainer(
    "世界坐标系", 
    new Point(0, 0), 
    new Size(1000, 800), 
    "设备的全局坐标系统");

// 2. 创建轨道坐标系
var track1 = new HierarchicalImageContainer(
    "轨道1", worldContainer, 
    new Point(100, 200),  // 在世界坐标系中的位置
    new Size(400, 50), 
    "第一条运输轨道");

worldContainer.AddChildContainer(track1, new Point(100, 200));

// 3. 创建载具坐标系
var vehicle = new HierarchicalImageContainer(
    "载具A", track1, 
    new Point(50, 10),    // 在轨道坐标系中的位置
    new Size(80, 30), 
    "轨道1上的载具A");

track1.AddChildContainer(vehicle, new Point(50, 10));

// 4. 创建电路板坐标系
var pcb = new HierarchicalImageContainer(
    "电路板1", vehicle, 
    new Point(10, 5),     // 在载具坐标系中的位置
    new Size(30, 20), 
    "载具A上的主电路板");

vehicle.AddChildContainer(pcb, new Point(10, 5));

// 5. 创建子拼板坐标系
var subPcb = new HierarchicalImageContainer(
    "子拼板1", pcb, 
    new Point(2, 2),      // 在电路板坐标系中的位置
    new Size(8, 6), 
    "电路板1的第一个子拼板");

pcb.AddChildContainer(subPcb, new Point(2, 2));
```

### 坐标转换

```csharp
// 在子拼板上定义一个点
Point localPoint = new Point(2, 3);

// 转换到根坐标系（世界坐标系）
Point worldPoint = subPcb.TransformToRoot(localPoint);

// 转换到指定容器的坐标系
Point vehiclePoint = subPcb.TransformToContainer(localPoint, vehicle);

// 从根坐标系转换到本地坐标系
Point backToLocal = subPcb.TransformFromRoot(worldPoint);

// 在不同容器间直接转换
Point anotherPcbPoint = subPcb.TransformToContainer(localPoint, anotherSubPcb);
```

### 坐标系查找

```csharp
// 通过路径查找
var container = worldContainer.FindContainerByPath("世界坐标系/轨道1/载具A/电路板1");

// 递归查找后代容器
var foundContainer = worldContainer.FindDescendantContainer("子拼板1");

// 查找直接子容器
var childContainer = worldContainer.FindChildContainer("轨道1");
```

### 物理坐标映射

```csharp
// 为电路板设置物理坐标映射
pcb.SetPhysicalCoordinateMapping(
    0.1,                      // 1像素 = 0.1毫米
    new Point(5, 5),          // 物理原点在图像的(5,5)位置
    true                      // Y轴向上
);

// 像素坐标转物理坐标
Point physicalPoint = pcb.LocalCoordinateSystem.ImagePixelToPhysical(pixelPoint);

// 物理坐标转像素坐标
Point pixelPoint = pcb.LocalCoordinateSystem.PhysicalToImagePixel(physicalPoint);
```

### 动态管理

```csharp
// 动态添加新容器
var newTrack = new HierarchicalImageContainer("轨道3", worldContainer, 
    new Point(100, 600), new Size(400, 50));
worldContainer.AddChildContainer(newTrack, new Point(100, 600));

// 移动容器（修改坐标系变换）
vehicle.SetContainerTransform(
    new Point(250, 15),   // 新位置
    1.2,                  // 缩放比例
    30                    // 旋转角度（度）
);

// 移除容器
worldContainer.RemoveChildContainer(track);
```

## 应用场景

### 1. 制造设备控制系统
- **世界坐标系**：整个设备的全局坐标
- **工作台坐标系**：各个工作台的局部坐标
- **工件坐标系**：工件在工作台上的坐标
- **特征坐标系**：工件上具体特征的坐标

### 2. 自动化运输系统
- **设备坐标系**：整个运输系统
- **轨道坐标系**：各条运输轨道
- **载具坐标系**：运输载具
- **货物坐标系**：载具上的货物位置

### 3. 印刷电路板检测
- **相机坐标系**：检测设备的全局坐标
- **电路板坐标系**：被检测的电路板
- **区域坐标系**：电路板上的功能区域
- **元件坐标系**：具体电子元件的位置

### 4. 机器视觉标定
- **世界坐标系**：真实物理世界坐标
- **相机坐标系**：相机的观察坐标
- **图像坐标系**：图像像素坐标
- **ROI坐标系**：感兴趣区域的局部坐标

## 优势特性

### 1. 完全的层次化支持
- 支持任意深度的嵌套
- 自动维护父子关系
- 级联坐标变换

### 2. 高效的坐标转换
- 直接转换算法，避免多次中间转换
- 矩阵变换优化
- 支持逆向转换验证

### 3. 灵活的查找机制
- 路径查找：`"世界/轨道1/载具A/电路板1"`
- 名称查找：递归查找指定名称的容器
- 类型筛选：按容器类型过滤

### 4. 物理单位集成
- 像素到物理单位的自动转换
- 支持不同的物理单位系统
- Y轴方向可配置

### 5. 动态管理能力
- 运行时添加/移除坐标系
- 动态修改坐标系变换
- 实时更新坐标转换关系

## 示例代码

完整的使用示例请参考：
- `HierarchicalCoordinateSystemDemo.cs` - 完整演示
- `CoordinateSystemExample.cs` - 基础示例

运行演示：
```csharp
// 创建完整的多层结构
var worldContainer = HierarchicalCoordinateSystemDemo.CreateCompleteHierarchy();

// 演示坐标转换
HierarchicalCoordinateSystemDemo.DemonstrateCoordinateTransformations();

// 演示动态管理
HierarchicalCoordinateSystemDemo.DemonstrateDynamicManagement();

// 演示查找功能
HierarchicalCoordinateSystemDemo.DemonstrateSearchAndTraversal();

// 演示物理坐标映射
HierarchicalCoordinateSystemDemo.DemonstratePhysicalCoordinateMapping();
```

## 注意事项

1. **性能考虑**：深层嵌套会增加坐标转换的计算复杂度，建议合理设计层次深度
2. **命名规范**：使用清晰、一致的命名规范，便于查找和管理
3. **坐标系同步**：修改容器变换时，相关的坐标转换会自动更新
4. **内存管理**：及时清理不再使用的容器，避免内存泄漏
5. **线程安全**：当前实现不是线程安全的，多线程环境下需要额外的同步机制