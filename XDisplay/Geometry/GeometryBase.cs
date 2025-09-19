using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry
{
    /// <summary>
    /// 几何图形基类 - 实现IGeometry接口的通用功能
    /// 为具体几何图形提供标准实现和辅助方法
    /// </summary>
    public abstract class GeometryBase : IGeometry
    {
        #region IGeometry 基本属性实现

        public Guid Id { get; }

        public abstract string TypeName { get; }

        public abstract Rect Bounds { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnGeometryChanged(GeometryChangeType.Selection);
                }
            }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnGeometryChanged(GeometryChangeType.Visibility);
                }
            }
        }

        public int ZOrder
        {
            get => _zOrder;
            set
            {
                if (_zOrder != value)
                {
                    _zOrder = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        #endregion

        #region IGeometry 外观属性实现

        public Brush? Fill
        {
            get => _fill;
            set
            {
                if (_fill != value)
                {
                    _fill = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        public Pen? Stroke
        {
            get => _stroke;
            set
            {
                if (_stroke != value)
                {
                    _stroke = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        public double Opacity
        {
            get => _opacity;
            set
            {
                double newOpacity = Math.Max(0.0, Math.Min(1.0, value));
                if (Math.Abs(_opacity - newOpacity) > 0.001)
                {
                    _opacity = newOpacity;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        #endregion

        #region 构造函数

        protected GeometryBase()
        {
            Id = Guid.NewGuid();
            _isSelected = false;
            _isVisible = true;
            _zOrder = 0;
            _opacity = 1.0;

            // 设置默认外观
            _fill = null;
            _stroke = new Pen(Brushes.Black, 1.0);
            _stroke.Freeze(); // 冻结画笔提高性能
        }

        #endregion

        #region IGeometry 核心方法 - 抽象方法

        public abstract void Render(DrawingContext context, Core.ViewportTransform? transform);

        public abstract bool HitTest(Point worldPoint, double tolerance = 1.0);

        public abstract bool HitTest(Rect worldRect);

        public abstract IGeometry Clone();

        #endregion

        #region IGeometry 变换方法 - 抽象方法

        public abstract void Move(double deltaX, double deltaY);

        public abstract void SetPosition(Point worldPoint);

        public abstract void Scale(double scaleX, double scaleY, Point origin);

        public abstract void Rotate(double angle, Point center);

        #endregion

        #region IGeometry 编辑支持 - 抽象方法

        public abstract Point[] GetControlPoints();

        public abstract void UpdateControlPoint(int pointIndex, Point newWorldPoint);

        public abstract ControlPointType GetControlPointType(int pointIndex);

        #endregion

        #region IGeometry 事件

        public event EventHandler<GeometryChangedEventArgs>? GeometryChanged;

        #endregion

        #region 保护方法

        /// <summary>
        /// 触发几何图形改变事件
        /// </summary>
        /// <param name="changeType">改变类型</param>
        protected virtual void OnGeometryChanged(GeometryChangeType changeType)
        {
            GeometryChanged?.Invoke(this, new GeometryChangedEventArgs(this, changeType));
        }

        /// <summary>
        /// 应用渲染变换（处理透明度、选择状态等）
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <param name="renderAction">实际的绘制操作</param>
        protected void ApplyRenderTransform(DrawingContext context, Action<DrawingContext> renderAction)
        {
            if (!IsVisible) return;

            // 处理透明度
            if (Math.Abs(Opacity - 1.0) > 0.001)
            {
                context.PushOpacity(Opacity);
                renderAction(context);
                context.Pop();
            }
            else
            {
                renderAction(context);
            }

            // 如果被选中，绘制选择指示器
            if (IsSelected)
            {
                DrawSelectionIndicator(context);
            }
        }

        /// <summary>
        /// 绘制选择指示器
        /// </summary>
        /// <param name="context">绘制上下文</param>
        protected virtual void DrawSelectionIndicator(DrawingContext context)
        {
            // 不在几何图形级别绘制选择指示器
            // 选择指示器由SelectionLayer统一管理和绘制
            // 这样可以避免坐标变换问题并保持一致的选择外观
        }

        /// <summary>
        /// 点是否在矩形范围内（带容差）
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="rect">矩形</param>
        /// <param name="tolerance">容差</param>
        /// <returns>是否在范围内</returns>
        protected static bool IsPointInRect(Point point, Rect rect, double tolerance)
        {
            return point.X >= rect.Left - tolerance &&
                   point.X <= rect.Right + tolerance &&
                   point.Y >= rect.Top - tolerance &&
                   point.Y <= rect.Bottom + tolerance;
        }

        /// <summary>
        /// 计算点到线段的距离
        /// </summary>
        /// <param name="point">测试点</param>
        /// <param name="lineStart">线段起点</param>
        /// <param name="lineEnd">线段终点</param>
        /// <returns>距离</returns>
        protected static double DistanceToLineSegment(Point point, Point lineStart, Point lineEnd)
        {
            double A = point.X - lineStart.X;
            double B = point.Y - lineStart.Y;
            double C = lineEnd.X - lineStart.X;
            double D = lineEnd.Y - lineStart.Y;

            double dot = A * C + B * D;
            double lenSq = C * C + D * D;

            if (lenSq == 0) return Math.Sqrt(A * A + B * B);

            double param = dot / lenSq;

            double xx, yy;

            if (param < 0)
            {
                xx = lineStart.X;
                yy = lineStart.Y;
            }
            else if (param > 1)
            {
                xx = lineEnd.X;
                yy = lineEnd.Y;
            }
            else
            {
                xx = lineStart.X + param * C;
                yy = lineStart.Y + param * D;
            }

            double dx = point.X - xx;
            double dy = point.Y - yy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region 字段

        private bool _isSelected;
        private bool _isVisible;
        private int _zOrder;
        private double _opacity;
        private Brush? _fill;
        private Pen? _stroke;

        #endregion
    }
}