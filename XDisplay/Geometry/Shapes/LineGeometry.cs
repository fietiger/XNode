using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Shapes
{
    /// <summary>
    /// 线段几何图形
    /// </summary>
    public class LineGeometry : GeometryBase
    {
        #region 属性

        public override string TypeName => "Line";

        public override Rect Bounds
        {
            get
            {
                double left = Math.Min(_startPoint.X, _endPoint.X);
                double top = Math.Min(_startPoint.Y, _endPoint.Y);
                double right = Math.Max(_startPoint.X, _endPoint.X);
                double bottom = Math.Max(_startPoint.Y, _endPoint.Y);
                
                // 确保线段至少有1像素的边界
                if (Math.Abs(right - left) < 1) right = left + 1;
                if (Math.Abs(bottom - top) < 1) bottom = top + 1;
                
                return new Rect(left, top, right - left, bottom - top);
            }
        }

        /// <summary>起点（世界坐标）</summary>
        public Point StartPoint
        {
            get => _startPoint;
            set
            {
                if (_startPoint != value)
                {
                    _startPoint = value;
                    OnGeometryChanged(GeometryChangeType.Position);
                }
            }
        }

        /// <summary>终点（世界坐标）</summary>
        public Point EndPoint
        {
            get => _endPoint;
            set
            {
                if (_endPoint != value)
                {
                    _endPoint = value;
                    OnGeometryChanged(GeometryChangeType.Position);
                }
            }
        }

        /// <summary>线段粗细</summary>
        public double Thickness
        {
            get => _thickness;
            set
            {
                double newThickness = Math.Max(0.1, value);
                if (Math.Abs(_thickness - newThickness) > 0.001)
                {
                    _thickness = newThickness;
                    UpdateStrokePen();
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        /// <summary>线段长度</summary>
        public double Length
        {
            get
            {
                double dx = _endPoint.X - _startPoint.X;
                double dy = _endPoint.Y - _startPoint.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        #endregion

        #region 构造函数

        public LineGeometry() : this(new Point(0, 0), new Point(100, 100))
        {
        }

        public LineGeometry(Point startPoint, Point endPoint)
        {
            _startPoint = startPoint;
            _endPoint = endPoint;
            _thickness = 1.0;
            UpdateStrokePen();
        }

        public LineGeometry(double x1, double y1, double x2, double y2)
        {
            _startPoint = new Point(x1, y1);
            _endPoint = new Point(x2, y2);
            _thickness = 1.0;
            UpdateStrokePen();
        }

        #endregion

        #region GeometryBase 实现

        public override void Render(DrawingContext context, Core.ViewportTransform? transform)
        {
            if (!IsVisible) return;

            ApplyRenderTransform(context, ctx =>
            {
                Point screenStart = transform?.WorldToScreen(_startPoint) ?? _startPoint;
                Point screenEnd = transform?.WorldToScreen(_endPoint) ?? _endPoint;

                // 使用当前的画笔设置绘制线段
                Pen renderPen = Stroke ?? CreateDefaultPen(transform);
                ctx.DrawLine(renderPen, screenStart, screenEnd);
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            double distance = DistanceToLineSegment(worldPoint, _startPoint, _endPoint);
            return distance <= tolerance + _thickness / 2;
        }

        public override bool HitTest(Rect worldRect)
        {
            Rect lineBounds = Bounds;
            return lineBounds.IntersectsWith(worldRect);
        }

        public override IGeometry Clone()
        {
            var clone = new LineGeometry(_startPoint, _endPoint)
            {
                Thickness = _thickness,
                Fill = Fill,
                Stroke = Stroke,
                Opacity = Opacity,
                IsVisible = IsVisible,
                ZOrder = ZOrder
            };
            return clone;
        }

        #endregion

        #region 变换方法

        public override void Move(double deltaX, double deltaY)
        {
            _startPoint.X += deltaX;
            _startPoint.Y += deltaY;
            _endPoint.X += deltaX;
            _endPoint.Y += deltaY;
            OnGeometryChanged(GeometryChangeType.Position);
        }

        public override void SetPosition(Point worldPoint)
        {
            // 将起点移动到指定位置，终点相对移动
            double deltaX = worldPoint.X - _startPoint.X;
            double deltaY = worldPoint.Y - _startPoint.Y;
            Move(deltaX, deltaY);
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            // 缩放起点和终点
            double newStartX = origin.X + ((_startPoint.X - origin.X) * scaleX);
            double newStartY = origin.Y + ((_startPoint.Y - origin.Y) * scaleY);
            double newEndX = origin.X + ((_endPoint.X - origin.X) * scaleX);
            double newEndY = origin.Y + ((_endPoint.Y - origin.Y) * scaleY);

            _startPoint = new Point(newStartX, newStartY);
            _endPoint = new Point(newEndX, newEndY);
            
            // 缩放线段粗细
            double avgScale = (Math.Abs(scaleX) + Math.Abs(scaleY)) / 2;
            _thickness *= avgScale;
            UpdateStrokePen();

            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override void Rotate(double angle, Point center)
        {
            double radians = angle * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            // 旋转起点
            double startDeltaX = _startPoint.X - center.X;
            double startDeltaY = _startPoint.Y - center.Y;
            double newStartX = center.X + startDeltaX * cos - startDeltaY * sin;
            double newStartY = center.Y + startDeltaX * sin + startDeltaY * cos;
            _startPoint = new Point(newStartX, newStartY);

            // 旋转终点
            double endDeltaX = _endPoint.X - center.X;
            double endDeltaY = _endPoint.Y - center.Y;
            double newEndX = center.X + endDeltaX * cos - endDeltaY * sin;
            double newEndY = center.Y + endDeltaX * sin + endDeltaY * cos;
            _endPoint = new Point(newEndX, newEndY);

            OnGeometryChanged(GeometryChangeType.Rotation);
        }

        #endregion

        #region 编辑支持

        public override Point[] GetControlPoints()
        {
            return new Point[]
            {
                _startPoint,  // 起点
                _endPoint,    // 终点
                new Point((_startPoint.X + _endPoint.X) / 2, (_startPoint.Y + _endPoint.Y) / 2)  // 中点
            };
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            switch (pointIndex)
            {
                case 0: // 起点
                    _startPoint = newWorldPoint;
                    OnGeometryChanged(GeometryChangeType.Position);
                    break;
                case 1: // 终点
                    _endPoint = newWorldPoint;
                    OnGeometryChanged(GeometryChangeType.Position);
                    break;
                case 2: // 中点 - 移动整条线段
                    Point currentMidpoint = new Point((_startPoint.X + _endPoint.X) / 2, (_startPoint.Y + _endPoint.Y) / 2);
                    double deltaX = newWorldPoint.X - currentMidpoint.X;
                    double deltaY = newWorldPoint.Y - currentMidpoint.Y;
                    Move(deltaX, deltaY);
                    break;
            }
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            return pointIndex == 2 ? ControlPointType.Move : ControlPointType.Resize;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新描边画笔
        /// </summary>
        private void UpdateStrokePen()
        {
            if (Stroke is Pen pen)
            {
                Stroke = new Pen(pen.Brush, _thickness)
                {
                    DashStyle = pen.DashStyle,
                    StartLineCap = pen.StartLineCap,
                    EndLineCap = pen.EndLineCap
                };
                Stroke.Freeze();
            }
        }

        /// <summary>
        /// 创建默认画笔
        /// </summary>
        private Pen CreateDefaultPen(Core.ViewportTransform? transform)
        {
            double screenThickness = _thickness;
            if (transform != null)
            {
                screenThickness *= transform.Scale;
            }
            
            var pen = new Pen(Brushes.Black, screenThickness);
            pen.Freeze();
            return pen;
        }

        #endregion

        #region 字段

        private Point _startPoint;
        private Point _endPoint;
        private double _thickness;

        #endregion
    }
}