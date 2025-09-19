# XDisplay 自动全局显示功能说明

## 功能概述

XDisplay 控件新增了自动全局显示功能，能够智能计算所有可见内容（包括图像和几何图形）的最小外接轴对齐矩形，并自动选择合适的缩放比例，使内容在视口中最大化显示且不发生变形。

## 核心功能

### 1. 自动全局显示 (`AutoFitToAllContent`)

```csharp
public void AutoFitToAllContent(double margin = 50)
```

- **功能**: 自动计算所有可见内容的边界，并调整视口以最大化显示所有内容
- **参数**: 
  - `margin`: 边距（像素），默认为50像素
- **特点**:
  - 支持图像和几何图形的混合显示
  - 保持内容的宽高比不变形
  - 智能选择最优缩放比例
  - 自动居中显示

### 2. 获取内容边界 (`GetAllContentBounds`)

```csharp
public Rect GetAllContentBounds()
```

- **功能**: 计算所有可见内容的最小外接轴对齐矩形
- **返回值**: 世界坐标系下的边界矩形，如果没有内容则返回空矩形
- **用途**: 可用于自定义的视口调整逻辑

### 3. 内容检测 (`HasVisibleContent`)

```csharp
public bool HasVisibleContent { get; }
```

- **功能**: 检测当前是否有可见的内容（图像或几何图形）
- **用途**: 用于决定是否执行自动适应操作

### 4. 内容变化事件 (`ContentChanged`)

```csharp
public event EventHandler? ContentChanged;
```

- **功能**: 当内容发生变化时触发（添加、删除、可见性变化）
- **用途**: 支持实时自动适应功能

## 使用示例

### 基本使用

```csharp
// 添加一些内容
displayControl.CreateRectangle(new Rect(100, 100, 200, 150));
displayControl.CreateCircle(new Point(500, 300), 80);
displayControl.AddImage(bitmap, new Point(0, 0));

// 自动适应所有内容
displayControl.AutoFitToAllContent();
```

### 自定义边距

```csharp
// 使用较小的边距以获得更紧密的适应
displayControl.AutoFitToAllContent(20);

// 使用较大的边距以获得更宽松的显示
displayControl.AutoFitToAllContent(100);
```

### 实时自动适应

```csharp
// 启用实时自动适应
displayControl.ContentChanged += (sender, args) =>
{
    if (displayControl.HasVisibleContent)
    {
        displayControl.AutoFitToAllContent();
    }
};
```

### 获取内容信息

```csharp
// 检查是否有内容
if (displayControl.HasVisibleContent)
{
    // 获取内容边界
    Rect bounds = displayControl.GetAllContentBounds();
    Console.WriteLine($"内容范围: {bounds}");
    
    // 执行自动适应
    displayControl.AutoFitToAllContent();
}
```

## 技术实现

### 边界计算算法

1. **几何图形边界**: 遍历所有可见的几何图形，获取其 `Bounds` 属性
2. **图像边界**: 遍历所有可见的图像，根据位置和尺寸计算边界
3. **联合计算**: 使用 `Rect.Union` 方法计算所有内容的最小外接矩形

### 缩放策略

1. **计算视口可用空间**: 视口尺寸减去指定的边距
2. **计算缩放比例**: 分别计算X和Y方向的缩放比例
3. **选择最小值**: 选择较小的缩放比例以确保内容完全可见且不变形
4. **应用变换**: 使用 `ViewportTransform.FitToBounds` 方法应用计算结果

### 事件驱动更新

- **图像变化**: 监听 `ImageLayer.ImageContentChanged` 事件
- **几何图形变化**: 在添加、删除、可见性变化时触发 `ContentChanged` 事件
- **实时响应**: 支持内容变化时的自动适应

## 测试程序功能

测试程序 (`XDisplay.Test`) 提供了以下演示功能：

### 工具栏按钮

- **自动全局显示**: 直接调用 `AutoFitToAllContent()` 方法

### 菜单功能

- **创建复杂场景**: 创建分散在不同位置的多种内容用于测试
- **自动适应演示**: 展示边界计算和自动适应过程
- **内容边界信息**: 显示当前内容的详细边界信息
- **启用实时自动适应**: 开启内容变化时的自动适应功能

### 使用步骤

1. 启动测试程序
2. 使用菜单"演示" → "创建复杂场景"创建测试内容
3. 点击工具栏的"自动全局显示"按钮查看效果
4. 或使用菜单中的各种演示功能

## 优势特点

1. **智能化**: 自动计算最优的显示参数，无需手动调整
2. **全面性**: 同时支持图像和几何图形的混合显示
3. **保真性**: 确保内容不发生变形，保持原有宽高比
4. **灵活性**: 支持自定义边距和实时更新
5. **高效性**: 仅在有可见内容时执行计算，避免不必要的操作
6. **可扩展性**: 提供事件和属性支持自定义的适应逻辑

## 适用场景

- **CAD/设计软件**: 查看整个设计图纸
- **图像查看器**: 适应图像和标注的显示
- **数据可视化**: 自动适应图表和图形的显示范围
- **游戏编辑器**: 查看整个场景或关卡
- **流程图编辑器**: 显示完整的流程图结构

## 注意事项

1. **性能考虑**: 在内容较多时，边界计算可能需要一定时间
2. **边距设置**: 合理设置边距以获得最佳的视觉效果
3. **实时更新**: 谨慎使用实时自动适应，避免在频繁变化时造成视觉干扰
4. **空内容处理**: 没有可见内容时会重置视口到默认状态

## 版本信息

- **添加版本**: XDisplay v1.1
- **兼容性**: 向后兼容，不影响现有功能
- **依赖**: 基于现有的 `ViewportTransform.FitToBounds` 方法实现