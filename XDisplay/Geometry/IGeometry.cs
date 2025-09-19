using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry
{
    /// <summary>
    /// 几何图形接口 - 定义所有几何图形的基本契约
    /// 提供可扩展的几何图形系统，支持自定义图形
    /// </summary>
    public interface IGeometry
    {
        #region 基本属性

        /// <summary>几何图形唯一标识</summary>
        Guid Id { get; }

        /// <summary>几何图形类型名称</summary>
        string TypeName { get; }

        /// <summary>几何图形边界矩形（世界坐标）</summary>
        Rect Bounds { get; }

        /// <summary>是否被选中</summary>
        bool IsSelected { get; set; }

        /// <summary>是否可见</summary>
        bool IsVisible { get; set; }

        /// <summary>Z序（用于控制绘制顺序）</summary>
        int ZOrder { get; set; }

        #endregion

        #region 外观属性

        /// <summary>填充画刷</summary>
        Brush? Fill { get; set; }

        /// <summary>描边画笔</summary>
        Pen? Stroke { get; set; }

        /// <summary>透明度（0.0-1.0）</summary>
        double Opacity { get; set; }

        #endregion

        #region 核心方法

        /// <summary>
        /// 绘制几何图形
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <param name="transform">视口变换（用于坐标转换）</param>
        void Render(DrawingContext context, Core.ViewportTransform? transform);

        /// <summary>
        /// 命中测试 - 检测指定点是否在几何图形内
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <param name="tolerance">容差值（世界坐标单位）</param>
        /// <returns>是否命中</returns>
        bool HitTest(Point worldPoint, double tolerance = 1.0);

        /// <summary>
        /// 边界命中测试 - 检测指定矩形是否与几何图形相交
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>是否相交</returns>
        bool HitTest(Rect worldRect);

        /// <summary>
        /// 克隆几何图形
        /// </summary>
        /// <returns>几何图形的副本</returns>
        IGeometry Clone();

        #endregion

        #region 变换方法

        /// <summary>
        /// 移动几何图形
        /// </summary>
        /// <param name="deltaX">X轴移动量（世界坐标）</param>
        /// <param name="deltaY">Y轴移动量（世界坐标）</param>
        void Move(double deltaX, double deltaY);

        /// <summary>
        /// 设置几何图形位置
        /// </summary>
        /// <param name="worldPoint">新位置（世界坐标）</param>
        void SetPosition(Point worldPoint);

        /// <summary>
        /// 缩放几何图形
        /// </summary>
        /// <param name="scaleX">X轴缩放因子</param>
        /// <param name="scaleY">Y轴缩放因子</param>
        /// <param name="origin">缩放原点（世界坐标）</param>
        void Scale(double scaleX, double scaleY, Point origin);

        /// <summary>
        /// 旋转几何图形
        /// </summary>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="center">旋转中心（世界坐标）</param>
        void Rotate(double angle, Point center);

        #endregion

        #region 编辑支持

        /// <summary>
        /// 获取控制点列表（用于编辑手柄）
        /// </summary>
        /// <returns>控制点数组</returns>
        Point[] GetControlPoints();

        /// <summary>
        /// 更新控制点
        /// </summary>
        /// <param name="pointIndex">控制点索引</param>
        /// <param name="newWorldPoint">新的世界坐标位置</param>
        void UpdateControlPoint(int pointIndex, Point newWorldPoint);

        /// <summary>
        /// 获取控制点类型
        /// </summary>
        /// <param name="pointIndex">控制点索引</param>
        /// <returns>控制点类型</returns>
        ControlPointType GetControlPointType(int pointIndex);

        #endregion

        #region 事件

        /// <summary>几何图形属性改变时触发</summary>
        event EventHandler<GeometryChangedEventArgs>? GeometryChanged;

        #endregion
    }

    /// <summary>
    /// 控制点类型枚举
    /// </summary>
    public enum ControlPointType
    {
        /// <summary>移动控制点</summary>
        Move,
        /// <summary>调整大小控制点</summary>
        Resize,
        /// <summary>旋转控制点</summary>
        Rotate,
        /// <summary>自定义控制点</summary>
        Custom
    }

    /// <summary>
    /// 几何图形改变事件参数
    /// </summary>
    public class GeometryChangedEventArgs : EventArgs
    {
        /// <summary>几何图形</summary>
        public IGeometry Geometry { get; }

        /// <summary>改变类型</summary>
        public GeometryChangeType ChangeType { get; }

        /// <summary>构造函数</summary>
        public GeometryChangedEventArgs(IGeometry geometry, GeometryChangeType changeType)
        {
            Geometry = geometry;
            ChangeType = changeType;
        }
    }

    /// <summary>
    /// 几何图形改变类型
    /// </summary>
    public enum GeometryChangeType
    {
        /// <summary>位置改变</summary>
        Position,
        /// <summary>大小改变</summary>
        Size,
        /// <summary>旋转改变</summary>
        Rotation,
        /// <summary>外观改变</summary>
        Appearance,
        /// <summary>可见性改变</summary>
        Visibility,
        /// <summary>选择状态改变</summary>
        Selection
    }
}