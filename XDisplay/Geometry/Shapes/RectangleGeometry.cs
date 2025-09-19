using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Shapes
{
    /// <summary>
    /// 矩形几何图形
    /// </summary>
    public class RectangleGeometry : GeometryBase
    {
        #region 属性

        public override string TypeName => "Rectangle";

        public override Rect Bounds => _rect;

        /// <summary>矩形区域（世界坐标）</summary>
        public Rect Rectangle
        {
            get => _rect;
            set
            {
                if (_rect != value)
                {
                    _rect = value;
                    OnGeometryChanged(GeometryChangeType.Size);
                }
            }
        }

        /// <summary>圆角半径</summary>
        public double CornerRadius
        {
            get => _cornerRadius;
            set
            {
                if (Math.Abs(_cornerRadius - value) > 0.001)
                {
                    _cornerRadius = Math.Max(0, value);
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        #endregion

        #region 构造函数

        public RectangleGeometry() : this(new Rect(0, 0, 100, 100))
        {
        }

        public RectangleGeometry(Rect rect)
        {
            _rect = rect;
            _cornerRadius = 0;
        }

        public RectangleGeometry(Point topLeft, Size size)
        {
            _rect = new Rect(topLeft, size);
            _cornerRadius = 0;
        }

        public RectangleGeometry(double x, double y, double width, double height)
        {
            _rect = new Rect(x, y, width, height);
            _cornerRadius = 0;
        }

        #endregion

        #region GeometryBase 实现

        public override void Render(DrawingContext context, Core.ViewportTransform? transform)
        {
            if (!IsVisible) return;

            ApplyRenderTransform(context, ctx =>
            {
                Rect screenRect = transform?.WorldToScreen(_rect) ?? _rect;
                
                if (_cornerRadius > 0)
                {
                    double screenRadius = Math.Min(_cornerRadius, Math.Min(screenRect.Width, screenRect.Height) / 2);
                    if (transform != null)
                    {
                        screenRadius *= transform.Scale;
                    }
                    ctx.DrawRoundedRectangle(Fill, Stroke, screenRect, screenRadius, screenRadius);
                }
                else
                {
                    ctx.DrawRectangle(Fill, Stroke, screenRect);
                }
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            Rect expandedRect = new Rect(
                _rect.X - tolerance,
                _rect.Y - tolerance,
                _rect.Width + 2 * tolerance,
                _rect.Height + 2 * tolerance);

            return expandedRect.Contains(worldPoint);
        }

        public override bool HitTest(Rect worldRect)
        {
            return _rect.IntersectsWith(worldRect);
        }

        public override IGeometry Clone()
        {
            var clone = new RectangleGeometry(_rect)
            {
                CornerRadius = _cornerRadius,
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
            _rect.Offset(deltaX, deltaY);
            OnGeometryChanged(GeometryChangeType.Position);
        }

        public override void SetPosition(Point worldPoint)
        {
            // 将新位置设置为矩形的中心点
            double centerX = _rect.X + _rect.Width / 2;
            double centerY = _rect.Y + _rect.Height / 2;
            double deltaX = worldPoint.X - centerX;
            double deltaY = worldPoint.Y - centerY;
            Move(deltaX, deltaY);
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            // 计算新的矩形
            double newX = origin.X + ((_rect.X - origin.X) * scaleX);
            double newY = origin.Y + ((_rect.Y - origin.Y) * scaleY);
            double newWidth = _rect.Width * scaleX;
            double newHeight = _rect.Height * scaleY;

            _rect = new Rect(newX, newY, Math.Abs(newWidth), Math.Abs(newHeight));
            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override void Rotate(double angle, Point center)
        {
            // 矩形旋转较复杂，这里简化实现
            // 实际项目中可能需要维护变换矩阵
            OnGeometryChanged(GeometryChangeType.Rotation);
        }

        #endregion

        #region 编辑支持

        public override Point[] GetControlPoints()
        {
            return new Point[]
            {
                new Point(_rect.Left + _rect.Width/2, _rect.Top + _rect.Height/2), // 中心点（移动）
                new Point(_rect.Left, _rect.Top),       // 左上
                new Point(_rect.Right, _rect.Top),      // 右上
                new Point(_rect.Right, _rect.Bottom),   // 右下
                new Point(_rect.Left, _rect.Bottom),    // 左下
                new Point(_rect.Left + _rect.Width/2, _rect.Top),     // 上中
                new Point(_rect.Right, _rect.Top + _rect.Height/2),   // 右中
                new Point(_rect.Left + _rect.Width/2, _rect.Bottom),  // 下中
                new Point(_rect.Left, _rect.Top + _rect.Height/2),    // 左中
            };
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            switch (pointIndex)
            {
                case 0: // 中心点 - 移动
                    SetPosition(newWorldPoint);
                    break;
                case 1: // 左上
                    _rect = new Rect(newWorldPoint.X, newWorldPoint.Y, 
                                   Math.Max(0, _rect.Right - newWorldPoint.X), Math.Max(0, _rect.Bottom - newWorldPoint.Y));
                    break;
                case 2: // 右上
                    _rect = new Rect(_rect.Left, newWorldPoint.Y,
                                   Math.Max(0, newWorldPoint.X - _rect.Left), Math.Max(0, _rect.Bottom - newWorldPoint.Y));
                    break;
                case 3: // 右下
                    _rect = new Rect(_rect.Left, _rect.Top,
                                   Math.Max(0, newWorldPoint.X - _rect.Left), Math.Max(0, newWorldPoint.Y - _rect.Top));
                    break;
                case 4: // 左下
                    _rect = new Rect(newWorldPoint.X, _rect.Top,
                                   Math.Max(0, _rect.Right - newWorldPoint.X), Math.Max(0, newWorldPoint.Y - _rect.Top));
                    break;
                case 5: // 上中
                    _rect = new Rect(_rect.Left, newWorldPoint.Y,
                                   _rect.Width, Math.Max(0, _rect.Bottom - newWorldPoint.Y));
                    break;
                case 6: // 右中
                    _rect = new Rect(_rect.Left, _rect.Top,
                                   Math.Max(0, newWorldPoint.X - _rect.Left), _rect.Height);
                    break;
                case 7: // 下中
                    _rect = new Rect(_rect.Left, _rect.Top,
                                   _rect.Width, Math.Max(0, newWorldPoint.Y - _rect.Top));
                    break;
                case 8: // 左中
                    _rect = new Rect(newWorldPoint.X, _rect.Top,
                                   Math.Max(0, _rect.Right - newWorldPoint.X), _rect.Height);
                    break;
            }
            
            if (pointIndex > 0) // 如果不是中心点，则是尺寸调整
            {
                OnGeometryChanged(GeometryChangeType.Size);
            }
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            return pointIndex == 0 ? ControlPointType.Move : ControlPointType.Resize;
        }

        #endregion

        #region 字段

        private Rect _rect;
        private double _cornerRadius;

        #endregion
    }
}