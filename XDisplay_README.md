# XDisplay 高效显示控件

XDisplay是一个基于WPF的高性能显示控件，参考XNode项目的优秀绘制架构设计，专门用于图像显示、几何图形绘制和交互编辑。

## 🚀 主要特性

### 高效绘制架构
- **分层渲染系统**: 采用多图层分离式架构，不同类型元素独立渲染
- **DrawingVisual优化**: 基于WPF的DrawingVisual实现双缓冲高性能绘制
- **视口裁剪**: 智能边界检查，只绘制可见区域内的元素
- **画笔冻结**: 画笔资源冻结优化，减少GC压力
- **内联优化**: 关键绘制方法使用AggressiveInlining提升性能

### 功能特性
- **图像显示**: 支持多种图像格式的高效显示与管理
- **几何图形绘制**: 内置矩形、圆形、线段、有向线段等基本图形
- **可扩展几何系统**: 提供IGeometry接口，支持自定义几何图形
- **交互编辑**: 支持选择、移动、调整大小等编辑操作
- **缩放平移**: 鼠标滚轮缩放、鼠标拖拽平移
- **多选操作**: 支持Ctrl多选、框选等多种选择方式

## 📦 项目结构

```
XDisplay/
├── Core/                          # 核心架构
│   ├── DisplayCanvas.cs           # 高效绘制画布
│   ├── ViewportTransform.cs       # 视口变换管理
│   └── XDisplayControl.cs         # 主显示控件
├── Geometry/                      # 几何图形系统
│   ├── IGeometry.cs               # 几何图形接口
│   ├── GeometryBase.cs            # 几何图形基类
│   └── Shapes/                    # 具体图形实现
│       ├── RectangleGeometry.cs   # 矩形
│       ├── CircleGeometry.cs      # 圆形
│       ├── LineGeometry.cs        # 线段
│       └── ArrowLineGeometry.cs   # 有向线段
├── Layers/                        # 图层系统
│   ├── LayerBase.cs              # 图层基类
│   ├── ImageLayer.cs             # 图像显示图层
│   ├── GeometryLayer.cs          # 几何图形图层
│   ├── SelectionLayer.cs         # 选择交互图层
│   └── GridLayer.cs              # 背景网格图层
└── XDisplay.Test/                # 测试示例程序
    ├── MainWindow.xaml           # 主窗口界面
    └── MainWindow.xaml.cs        # 主窗口逻辑
```

## 🛠️ 核心技术

### 1. 分层渲染架构
```csharp
// 图层按Z序组织，独立更新
DisplayCanvas -> GridLayer       // 背景网格
              -> ImageLayer       // 图像显示
              -> GeometryLayer    // 几何图形
              -> SelectionLayer   // 选择交互
```

### 2. 高效坐标变换
```csharp
// 世界坐标 ↔ 屏幕坐标 高效转换
Point worldPoint = ViewportTransform.ScreenToWorld(screenPoint);
Point screenPoint = ViewportTransform.WorldToScreen(worldPoint);
```

### 3. 智能视口裁剪
```csharp
// 只绘制可见区域内的元素
if (IsRectVisible(geometryScreenBounds))
{
    geometry.Render(context, Transform);
}
```

### 4. 可扩展几何系统
```csharp
public interface IGeometry
{
    void Render(DrawingContext context, ViewportTransform? transform);
    bool HitTest(Point worldPoint, double tolerance = 1.0);
    Point[] GetControlPoints();
    // ... 更多接口方法
}
```

## 🎯 使用示例

### 基本使用
```xml
<!-- XAML中使用 -->
<xd:XDisplayControl x:Name="DisplayControl" 
                    SelectionChanged="DisplayControl_SelectionChanged"/>
```

```csharp
// 添加几何图形
var rect = new RectangleGeometry(new Rect(50, 50, 100, 80))
{
    Fill = new SolidColorBrush(Colors.LightBlue),
    Stroke = new Pen(Brushes.Blue, 2)
};
DisplayControl.AddGeometry(rect);

// 添加图像
DisplayControl.AddImage(bitmapImage, new Point(200, 100));

// 设置交互模式
DisplayControl.CurrentMode = InteractionMode.DrawRectangle;
```

### 自定义几何图形
```csharp
public class CustomGeometry : GeometryBase
{
    public override string TypeName => "Custom";
    
    public override void Render(DrawingContext context, ViewportTransform? transform)
    {
        // 实现自定义绘制逻辑
    }
    
    public override bool HitTest(Point worldPoint, double tolerance = 1.0)
    {
        // 实现自定义命中测试
    }
    
    // ... 实现其他必需方法
}
```

## 🔧 编译与运行

### 环境要求
- .NET 6.0 或更高版本
- Windows 平台
- Visual Studio 2022 或 Visual Studio Code

### 编译步骤
```bash
# 编译XDisplay库
cd XDisplay
dotnet build

# 编译并运行测试程序
cd ../XDisplay.Test
dotnet run
```

## 🎮 测试程序功能

测试程序提供了完整的功能演示：

### 交互模式
- **选择模式**: 选择、移动、编辑几何图形
- **平移模式**: 拖拽平移视口
- **绘制模式**: 绘制矩形、圆形、线段、箭头等

### 操作方式
- **鼠标滚轮**: 缩放视口
- **左键拖拽**: 根据当前模式执行操作
- **Ctrl+点击**: 多选几何图形
- **框选**: 拖拽选择多个对象
- **Delete键**: 删除选中对象
- **Ctrl+A**: 全选
- **Esc**: 清空选择/切换到选择模式

### 界面功能
- 工具栏提供各种操作按钮
- 状态栏显示当前模式、选择状态、缩放比例等信息
- 支持加载外部图像文件
- 支持显示/隐藏背景网格

## 🚀 性能特性

### 渲染优化
1. **分层架构**: 不同元素类型分层渲染，减少重绘范围
2. **视口裁剪**: 只绘制可见区域，提升大数据量场景性能
3. **画笔冻结**: 预冻结画笔资源，减少运行时开销
4. **方法内联**: 关键路径使用内联优化
5. **增量更新**: 只在数据变化时更新，避免无效重绘

### 内存优化
1. **对象池化**: 复用临时对象，减少GC压力
2. **懒加载**: 按需创建和加载资源
3. **及时清理**: 主动释放不再使用的资源

### 交互优化
1. **高效命中检测**: 使用几何命中检测，支持复杂形状
2. **多级命中**: 支持点击、区域等多种命中测试
3. **拖拽阈值**: 避免误触发拖拽操作

## 🎨 架构设计亮点

### 借鉴XNode的优秀设计
1. **DrawingVisual**: 继承XNode的高效绘制模式
2. **分层架构**: 参考XNode的图层管理系统
3. **坐标变换**: 采用XNode的坐标转换机制
4. **事件驱动**: 使用XNode的事件更新策略

### 自主创新优化
1. **几何图形系统**: 设计可扩展的几何图形接口
2. **交互模式**: 实现多种交互模式切换
3. **编辑支持**: 提供控制点编辑功能
4. **图像集成**: 无缝集成图像显示功能

## 📈 适用场景

- **图形编辑器**: CAD、绘图工具等
- **数据可视化**: 图表、流程图等
- **游戏开发**: 地图编辑器、关卡设计等
- **工业软件**: 监控界面、配置工具等
- **教育软件**: 几何教学、物理仿真等

## 🔮 扩展方向

1. **更多几何图形**: 多边形、贝塞尔曲线、文本等
2. **动画支持**: 集成XNode的动画系统
3. **图层管理**: 可视化图层管理界面
4. **导入导出**: 支持SVG、DXF等格式
5. **协作功能**: 多用户协同编辑
6. **性能监控**: 实时性能分析工具

## 📝 总结

XDisplay高效显示控件成功将XNode项目的优秀绘制架构应用到通用显示场景中，通过分层渲染、视口裁剪、画笔优化等技术实现了高性能的图形显示和交互编辑功能。该控件不仅保持了XNode的技术优势，还在几何图形系统、交互模式设计等方面进行了创新，为开发者提供了一个功能强大、性能优异、易于扩展的显示控件解决方案。