# XDisplay 坐标系设置功能说明

## 功能概述

XDisplay项目现在支持灵活的坐标系设置，您可以根据需要选择不同的坐标系方向：

### 支持的坐标系类型

1. **屏幕坐标系**（默认）
   - X轴：向右为正
   - Y轴：向下为正
   - 这是传统的屏幕/计算机图形坐标系

2. **数学坐标系**
   - X轴：向右为正  
   - Y轴：向上为正
   - 这是传统的数学坐标系

## 使用方法

### 代码方式

```csharp
// 设置为数学坐标系（Y轴向上）
displayControl.SetCoordinateSystem(yAxisUp: true);

// 设置为屏幕坐标系（Y轴向下） 
displayControl.SetCoordinateSystem(yAxisUp: false);

// 或者通过属性设置
displayControl.YAxisUp = true;  // 数学坐标系
displayControl.YAxisUp = false; // 屏幕坐标系
```

### 界面操作

在测试应用程序中，您可以通过以下方式切换坐标系：

1. **工具栏按钮**：
   - "数学坐标系(Y向上)" 按钮
   - "屏幕坐标系(Y向下)" 按钮

2. **菜单选项**：
   - 坐标系 → 数学坐标系 (Y向上)
   - 坐标系 → 屏幕坐标系 (Y向下)
   - 坐标系 → 坐标系演示

## 演示功能

### 坐标系演示
通过菜单"坐标系 → 坐标系演示"可以：
- 选择使用数学坐标系或屏幕坐标系
- 自动创建演示图形来展示坐标系差异
- 包含：
  - 红色圆点：原点(0,0)
  - 蓝色圆点：X轴正方向(50,0)
  - 绿色圆点：Y轴正方向(0,50) - **注意观察在不同坐标系下的位置差异！**
  - 黄色矩形：第一象限示例
  - 各色小点：四个象限的标记点
  - 坐标轴线：帮助理解坐标系方向

## 技术实现

### 核心组件

1. **ViewportTransform**
   - 负责屏幕坐标与世界坐标的转换
   - 新增 `YAxisUp` 属性控制Y轴方向
   - 新增 `SetCoordinateSystem()` 方法

2. **XDisplayControl**
   - 提供坐标系设置的公开接口
   - `YAxisUp` 属性和 `SetCoordinateSystem()` 方法

3. **LocalCoordinateSystem**
   - 已有的Y轴翻转支持
   - 用于局部坐标系的更复杂变换

### 坐标转换原理

```csharp
// 屏幕坐标转世界坐标
public Point ScreenToWorld(Point screenPoint)
{
    Point center = ViewportCenter;
    double worldX = (screenPoint.X - center.X - _offset.X) / _scale;
    double worldY = (screenPoint.Y - center.Y - _offset.Y) / _scale;
    
    // 应用Y轴翻转
    if (_yAxisUp)
    {
        worldY = -worldY;
    }
    
    return new Point(worldX, worldY);
}

// 世界坐标转屏幕坐标
public Point WorldToScreen(Point worldPoint)
{
    Point center = ViewportCenter;
    double screenX = worldPoint.X * _scale + center.X + _offset.X;
    double screenY = worldPoint.Y * _scale + center.Y + _offset.Y;
    
    // 应用Y轴翻转
    if (_yAxisUp)
    {
        screenY = center.Y + center.Y - screenY;
    }
    
    return new Point(screenX, screenY);
}
```

## 使用场景

### 数学坐标系适用于：
- 科学计算和数据可视化
- 数学函数图形绘制
- 工程制图应用
- 需要符合数学习惯的应用

### 屏幕坐标系适用于：
- 用户界面设计
- 游戏开发
- 传统的计算机图形应用
- 与WPF坐标系统保持一致的场景

## 注意事项

1. **坐标系切换后**，所有现有的几何图形将按新坐标系重新显示
2. **鼠标坐标**显示会自动适应当前坐标系
3. **绘制操作**（如绘制矩形、圆形等）会按当前坐标系进行
4. **视口操作**（缩放、平移）在两种坐标系下都能正常工作

## 兼容性

- 该功能完全向后兼容
- 默认使用屏幕坐标系，不影响现有代码
- 可以在运行时动态切换坐标系
- 所有现有的几何图形和图像容器都支持坐标系转换

---

这个功能让XDisplay控件能够适应更多的应用场景，特别是需要数学坐标系的科学和工程应用。