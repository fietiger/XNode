# XDisplay 坐标系统和容器功能说明

## 概述

我们已经成功实现了您要求的坐标系统和树形结构功能。新的设计包括：

1. **局部坐标系统** (LocalCoordinateSystem)
2. **图像容器** (ImageContainer) 
3. **通用几何容器** (GeometryContainer)
4. **坐标转换支持**

## 核心功能

### 1. LocalCoordinateSystem - 局部坐标系

支持以下功能：
- **物理坐标映射**：像素到毫米的比例转换
- **坐标系翻转**：支持Y轴向上的物理坐标系
- **变换操作**：平移、缩放、旋转
- **坐标转换**：本地坐标 ↔ 父坐标系 ↔ 物理坐标 ↔ 图像像素坐标

### 2. ImageContainer - 图像容器

特点：
- **可移动的图像**：图像不再是静态的，可以作为几何图形移动、缩放、旋转
- **背景图像支持**：可以设置背景图像，并配置物理坐标系
- **子元素管理**：可以包含其他几何图形作为子元素
- **局部坐标系**：子元素使用容器的局部坐标系
- **剪裁控制**：可选择是否裁剪子元素到容器边界

### 3. GeometryContainer - 通用几何容器

特点：
- **基于任意几何图形**：将任何现有几何图形转换为容器
- **保持原有功能**：容器本身具备原几何图形的所有功能
- **层次化管理**：支持子元素的添加、移除、查找
- **坐标继承**：子元素自动继承容器的坐标系

## 使用示例

### 创建PCB场景

```csharp
// 1. 创建PCB图像容器
var pcbContainer = new ImageContainer(
    new Point(100, 100),     // 位置
    new Size(200, 150),      // 尺寸
    pcbImage,                // 背景图像
    0.04,                    // 1像素 = 0.04mm
    new Point(500, 600),     // 物理原点在图像中的位置
    true                     // Y轴向上
);

// 2. 在PCB上添加元件（使用物理坐标：毫米）
var resistor = new RectangleGeometry(new Rect(0, 0, 5, 2)); // 5mm x 2mm
pcbContainer.AddChild(resistor, new Point(10, 15)); // 物理坐标(10mm, 15mm)

var chip = new RectangleGeometry(new Rect(0, 0, 8, 8)); // 8mm x 8mm
pcbContainer.AddChild(chip, new Point(30, 25)); // 物理坐标(30mm, 25mm)
```

### 坐标转换

```csharp
// 物理坐标 → 本地逻辑坐标
Point localPoint = container.LocalCoordinateSystem.PhysicalToLocal(physicalPoint);

// 本地坐标 → 世界坐标
Point worldPoint = container.LocalToWorld(localPoint);

// 图像像素坐标 → 物理坐标
Point physicalPoint = container.LocalCoordinateSystem.ImagePixelToPhysical(pixelPoint);
```

## 测试程序功能

当前的测试程序演示了以下功能：

1. **基础几何图形**：矩形、圆形、线段、箭头、旋转矩形
2. **图像容器示例**：带物理坐标系的"PCB"容器
3. **几何容器示例**：基于矩形的通用容器
4. **交互操作**：选择、移动、缩放、旋转
5. **新建容器**：通过"创建图像容器"按钮创建新容器

## 优势

1. **层次化管理**：父子关系清晰，便于管理复杂场景
2. **坐标系自动映射**：子元素自动使用父容器的坐标系
3. **变换传播**：移动容器时，所有子元素自动跟随
4. **物理单位支持**：直接使用毫米等物理单位进行设计
5. **图像精确定位**：支持图像上的精确坐标映射
6. **通用性**：适用于PCB设计、机械图纸、地图标注等多种场景

## 解决的问题

✅ **图像移动问题**：图像现在可以作为容器移动、缩放、旋转
✅ **多坐标系转换**：物理坐标、图像坐标、世界坐标间的转换
✅ **Y轴方向**：支持物理坐标系的Y轴向上
✅ **层次化管理**：树形结构管理几何图形
✅ **通用性设计**：不局限于"电路板"，适用于各种容器场景

## 新增功能：图像容器尺寸自动匹配

### ✅ 图像容器尺寸自动匹配

现在当您创建带背景图像的 ImageContainer 时，容器的尺寸会**自动匹配图像的实际尺寸**！

#### 使用方式：

1. **自动尺寸匹配**（推荐）：
```csharp
// 容器尺寸自动匹配图像尺寸
var container = ImageContainer.CreateFromImage(position, backgroundImage, 
    pixelToPhysicalRatio, physicalOriginInImage, yAxisUp);
```

2. **自定义尺寸**：
```csharp
// 使用自定义尺寸（可能导致图像拉伸）
var container = ImageContainer.CreateWithCustomSize(position, customSize, backgroundImage,
    pixelToPhysicalRatio, physicalOriginInImage, yAxisUp);
```

3. **空容器**：
```csharp
// 创建无背景图像的空容器
var container = ImageContainer.CreateEmpty(position, size);
```

#### 优势：
- **保持图像原始比例**：避免图像拉伸变形
- **简化使用**：无需手动计算图像尺寸
- **精确显示**：图像在容器中完美显示
- **坐标系准确**：物理坐标系基于真实图像尺寸

#### 测试方法：
1. 运行 XDisplay.Test 程序
2. 点击"创建图像容器"按钮
3. 选择一个图像文件
4. 查看提示信息，确认容器尺寸与图像尺寸匹配

测试程序会显示：
- 图像尺寸：[宽] x [高] 像素
- 容器尺寸：[宽] x [高]
- "容器尺寸自动匹配了图像尺寸！"