using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Shapes
{
    /// <summary>
    /// 圆形几何图形
    /// </summary>
    public class CircleGeometry : GeometryBase
    {
        #region 属性

        public override string TypeName => "Circle";

        public override Rect Bounds => new Rect(_center.X - _radius, _center.Y - _radius, 2 * _radius, 2 * _radius);

        /// <summary>圆心位置（世界坐标）</summary>
        public Point Center
        {
            get => _center;
            set
            {
                if (_center != value)
                {
                    _center = value;
                    OnGeometryChanged(GeometryChangeType.Position);
                }
            }
        }

        /// <summary>半径（世界坐标单位）</summary>
        public double Radius
        {
            get => _radius;
            set
            {
                double newRadius = Math.Max(0, value);
                if (Math.Abs(_radius - newRadius) > 0.001)
                {
                    _radius = newRadius;
                    OnGeometryChanged(GeometryChangeType.Size);
                }
            }
        }

        #endregion

        #region 构造函数

        public CircleGeometry() : this(new Point(0, 0), 50)
        {
        }

        public CircleGeometry(Point center, double radius)
        {
            _center = center;
            _radius = Math.Max(0, radius);
        }

        public CircleGeometry(double centerX, double centerY, double radius)
        {
            _center = new Point(centerX, centerY);
            _radius = Math.Max(0, radius);
        }

        #endregion

        #region GeometryBase 实现

        public override void Render(DrawingContext context, Core.ViewportTransform? transform)
        {
            if (!IsVisible || _radius <= 0) return;

            ApplyRenderTransform(context, ctx =>
            {
                Point screenCenter = transform?.WorldToScreen(_center) ?? _center;
                double screenRadius = _radius;
                
                if (transform != null)
                {
                    screenRadius *= transform.Scale;
                }

                ctx.DrawEllipse(Fill, Stroke, screenCenter, screenRadius, screenRadius);
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            double distance = Math.Sqrt(Math.Pow(worldPoint.X - _center.X, 2) + Math.Pow(worldPoint.Y - _center.Y, 2));
            return distance <= _radius + tolerance;
        }

        public override bool HitTest(Rect worldRect)
        {
            Rect circleBounds = Bounds;
            return circleBounds.IntersectsWith(worldRect);
        }

        public override IGeometry Clone()
        {
            var clone = new CircleGeometry(_center, _radius)
            {
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
            _center.X += deltaX;
            _center.Y += deltaY;
            OnGeometryChanged(GeometryChangeType.Position);
        }

        public override void SetPosition(Point worldPoint)
        {
            _center = worldPoint;
            OnGeometryChanged(GeometryChangeType.Position);
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            // 移动圆心
            double newCenterX = origin.X + ((_center.X - origin.X) * scaleX);
            double newCenterY = origin.Y + ((_center.Y - origin.Y) * scaleY);
            _center = new Point(newCenterX, newCenterY);

            // 缩放半径（使用平均缩放因子）
            double avgScale = (scaleX + scaleY) / 2;
            _radius *= Math.Abs(avgScale);

            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override void Rotate(double angle, Point center)
        {
            // 圆形旋转不改变形状，只需要旋转圆心位置
            double radians = angle * Math.PI / 180;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);

            double deltaX = _center.X - center.X;
            double deltaY = _center.Y - center.Y;

            double newX = center.X + deltaX * cos - deltaY * sin;
            double newY = center.Y + deltaX * sin + deltaY * cos;

            _center = new Point(newX, newY);
            OnGeometryChanged(GeometryChangeType.Position);
        }

        #endregion

        #region 编辑支持

        public override Point[] GetControlPoints()
        {
            return new Point[]
            {
                _center,  // 中心点（移动）
                new Point(_center.X + _radius, _center.Y),  // 右
                new Point(_center.X, _center.Y - _radius),  // 上
                new Point(_center.X - _radius, _center.Y),  // 左
                new Point(_center.X, _center.Y + _radius),  // 下
            };
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            switch (pointIndex)
            {
                case 0: // 中心点 - 移动
                    SetPosition(newWorldPoint);
                    break;
                case 1: // 右边点 - 调整半径
                    _radius = Math.Abs(newWorldPoint.X - _center.X);
                    OnGeometryChanged(GeometryChangeType.Size);
                    break;
                case 2: // 上边点 - 调整半径
                    _radius = Math.Abs(_center.Y - newWorldPoint.Y);
                    OnGeometryChanged(GeometryChangeType.Size);
                    break;
                case 3: // 左边点 - 调整半径
                    _radius = Math.Abs(_center.X - newWorldPoint.X);
                    OnGeometryChanged(GeometryChangeType.Size);
                    break;
                case 4: // 下边点 - 调整半径
                    _radius = Math.Abs(newWorldPoint.Y - _center.Y);
                    OnGeometryChanged(GeometryChangeType.Size);
                    break;
            }
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            return pointIndex == 0 ? ControlPointType.Move : ControlPointType.Resize;
        }

        #endregion

        #region 字段

        private Point _center;
        private double _radius;

        #endregion
    }
}