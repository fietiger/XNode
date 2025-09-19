using System;
using System.Windows;
using System.Windows.Media;
using XDisplay.Core;

namespace XDisplay.Geometry.Shapes
{
    /// <summary>
    /// 可旋转矩形几何图形 - 支持任意角度旋转的矩形
    /// </summary>
    public class RotatedRectangleGeometry : GeometryBase
    {
        #region 属性

        public override string TypeName => "RotatedRectangle";

        public override Rect Bounds => CalculateBounds();

        /// <summary>矩形区域（世界坐标，未旋转状态）</summary>
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

        /// <summary>旋转角度（度数）</summary>
        public double RotationAngle
        {
            get => _rotationAngle;
            set
            {
                if (Math.Abs(_rotationAngle - value) > 0.001)
                {
                    _rotationAngle = value;
                    OnGeometryChanged(GeometryChangeType.Rotation);
                }
            }
        }

        /// <summary>旋转中心点（世界坐标）</summary>
        public Point RotationCenter
        {
            get => _rotationCenter;
            set
            {
                if (_rotationCenter != value)
                {
                    _rotationCenter = value;
                    OnGeometryChanged(GeometryChangeType.Rotation);
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

        public RotatedRectangleGeometry() : this(new Rect(0, 0, 100, 100), 0)
        {
        }

        public RotatedRectangleGeometry(Rect rect, double rotationAngle = 0)
        {
            _rect = rect;
            _rotationAngle = rotationAngle;
            _rotationCenter = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            _cornerRadius = 0;
        }

        public RotatedRectangleGeometry(Point topLeft, Size size, double rotationAngle = 0)
        {
            _rect = new Rect(topLeft, size);
            _rotationAngle = rotationAngle;
            _rotationCenter = new Point(_rect.X + _rect.Width / 2, _rect.Y + _rect.Height / 2);
            _cornerRadius = 0;
        }

        public RotatedRectangleGeometry(double x, double y, double width, double height, double rotationAngle = 0)
        {
            _rect = new Rect(x, y, width, height);
            _rotationAngle = rotationAngle;
            _rotationCenter = new Point(x + width / 2, y + height / 2);
            _cornerRadius = 0;
        }

        #endregion

        #region GeometryBase 实现

        public override void Render(DrawingContext context, ViewportTransform? transform)
        {
            if (!IsVisible) return;

            ApplyRenderTransform(context, ctx =>
            {
                Rect screenRect = transform?.WorldToScreen(_rect) ?? _rect;
                Point screenCenter = transform?.WorldToScreen(_rotationCenter) ?? _rotationCenter;

                // 应用旋转变换
                double renderAngle = _rotationAngle;
                // 当Y轴向上时，需要翻转旋转角度
                if (transform != null && transform.YAxisUp)
                {
                    renderAngle = -_rotationAngle;
                }
                
                var rotateTransform = new RotateTransform(renderAngle, screenCenter.X, screenCenter.Y);
                ctx.PushTransform(rotateTransform);

                try
                {
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
                }
                finally
                {
                    ctx.Pop(); // 弹出旋转变换
                }
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            // 将测试点旋转到矩形的本地坐标系
            Point localPoint = RotatePointAroundCenter(worldPoint, _rotationCenter, -_rotationAngle);
            
            // 在本地坐标系中进行命中测试
            Rect expandedRect = new Rect(
                _rect.X - tolerance,
                _rect.Y - tolerance,
                _rect.Width + 2 * tolerance,
                _rect.Height + 2 * tolerance);

            return expandedRect.Contains(localPoint);
        }

        public override bool HitTest(Rect worldRect)
        {
            // 简化实现：检查旋转后的矩形的四个角点是否与测试矩形相交
            Point[] corners = GetRotatedCorners();
            
            // 检查任意角点是否在测试矩形内
            foreach (Point corner in corners)
            {
                if (worldRect.Contains(corner))
                    return true;
            }
            
            // 检查测试矩形的角点是否在旋转矩形内
            Point[] testCorners = {
                new Point(worldRect.Left, worldRect.Top),
                new Point(worldRect.Right, worldRect.Top),
                new Point(worldRect.Right, worldRect.Bottom),
                new Point(worldRect.Left, worldRect.Bottom)
            };

            foreach (Point testCorner in testCorners)
            {
                if (HitTest(testCorner))
                    return true;
            }

            return false;
        }

        public override IGeometry Clone()
        {
            var clone = new RotatedRectangleGeometry(_rect, _rotationAngle)
            {
                RotationCenter = _rotationCenter,
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
            _rotationCenter = new Point(_rotationCenter.X + deltaX, _rotationCenter.Y + deltaY);
            OnGeometryChanged(GeometryChangeType.Position);
        }

        public override void SetPosition(Point worldPoint)
        {
            // 将新位置设置为矩形的旋转中心
            double deltaX = worldPoint.X - _rotationCenter.X;
            double deltaY = worldPoint.Y - _rotationCenter.Y;
            Move(deltaX, deltaY);
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            // 缩放矩形
            double newX = origin.X + ((_rect.X - origin.X) * scaleX);
            double newY = origin.Y + ((_rect.Y - origin.Y) * scaleY);
            double newWidth = _rect.Width * scaleX;
            double newHeight = _rect.Height * scaleY;

            _rect = new Rect(newX, newY, Math.Abs(newWidth), Math.Abs(newHeight));
            
            // 缩放旋转中心
            _rotationCenter = new Point(
                origin.X + ((_rotationCenter.X - origin.X) * scaleX),
                origin.Y + ((_rotationCenter.Y - origin.Y) * scaleY));

            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override void Rotate(double angle, Point center)
        {
            _rotationAngle += angle;
            
            // 如果旋转中心不是当前的旋转中心，需要围绕新中心旋转
            if (center != _rotationCenter)
            {
                _rotationCenter = RotatePointAroundCenter(_rotationCenter, center, angle);
                
                // 同时旋转矩形中心
                Point rectCenter = new Point(_rect.X + _rect.Width / 2, _rect.Y + _rect.Height / 2);
                Point newRectCenter = RotatePointAroundCenter(rectCenter, center, angle);
                
                _rect.X = newRectCenter.X - _rect.Width / 2;
                _rect.Y = newRectCenter.Y - _rect.Height / 2;
            }
            
            OnGeometryChanged(GeometryChangeType.Rotation);
        }

        #endregion

        #region 编辑支持

        public override Point[] GetControlPoints()
        {
            Point[] corners = GetRotatedCorners();
            Point[] result = new Point[corners.Length + 2]; // 四个角点 + 中心移动点 + 一个旋转控制点
            
            // 复制角点
            Array.Copy(corners, result, corners.Length);
            
            // 添加中心移动控制点
            result[4] = _rotationCenter;
            
            // 添加旋转控制点（在矩形右边中点）
            Point rightCenter = new Point(_rect.X + _rect.Width, _rect.Y + _rect.Height / 2);
            result[5] = RotatePointAroundCenter(rightCenter, _rotationCenter, _rotationAngle);
            
            return result;
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            if (pointIndex < 4) // 角点控制
            {
                // 获取当前角点的世界坐标
                Point[] currentCorners = GetRotatedCorners();
                
                // 确定固定点（拖动角点的对角）
                Point fixedCorner = pointIndex switch
                {
                    0 => currentCorners[2], // 拖动左上，固定右下
                    1 => currentCorners[3], // 拖动右上，固定左下
                    2 => currentCorners[0], // 拖动右下，固定左上
                    3 => currentCorners[1], // 拖动左下，固定右上
                    _ => currentCorners[0]
                };
                
                // 将拖动点和固定点都转换到本地坐标系
                Point localNewPoint = RotatePointAroundCenter(newWorldPoint, _rotationCenter, -_rotationAngle);
                Point localFixedPoint = RotatePointAroundCenter(fixedCorner, _rotationCenter, -_rotationAngle);
                
                // 计算新的矩形（确保非负尺寸）
                double left = Math.Min(localNewPoint.X, localFixedPoint.X);
                double top = Math.Min(localNewPoint.Y, localFixedPoint.Y);
                double right = Math.Max(localNewPoint.X, localFixedPoint.X);
                double bottom = Math.Max(localNewPoint.Y, localFixedPoint.Y);
                
                double width = Math.Max(0, right - left);
                double height = Math.Max(0, bottom - top);
                
                // 更新矩形
                _rect = new Rect(left, top, width, height);
                
                // 重新计算旋转中心（新矩形的中心点）
                Point newLocalCenter = new Point(left + width / 2, top + height / 2);
                _rotationCenter = RotatePointAroundCenter(newLocalCenter, _rotationCenter, _rotationAngle);
                
                OnGeometryChanged(GeometryChangeType.Size);
            }
            else if (pointIndex == 4) // 中心移动控制点
            {
                // 移动整个矩形
                SetPosition(newWorldPoint);
            }
            else if (pointIndex == 5) // 旋转控制点
            {
                // 计算新的旋转角度
                Vector centerToOld = new Vector(1, 0); // 0度方向（右侧）
                Vector centerToNew = newWorldPoint - _rotationCenter;
                
                double newAngle = Vector.AngleBetween(centerToOld, centerToNew);
                
                // 在数学坐标系中，需要调整角度方向
                if (GlobalYAxisUp)
                {
                    newAngle = -newAngle;
                }
                
                _rotationAngle = newAngle;
                
                OnGeometryChanged(GeometryChangeType.Rotation);
            }
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            if (pointIndex < 4)
                return ControlPointType.Resize;
            else if (pointIndex == 4)
                return ControlPointType.Move;
            else
                return ControlPointType.Rotate;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 计算包围边界矩形
        /// </summary>
        private Rect CalculateBounds()
        {
            Point[] corners = GetRotatedCorners();
            
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            
            foreach (Point corner in corners)
            {
                minX = Math.Min(minX, corner.X);
                minY = Math.Min(minY, corner.Y);
                maxX = Math.Max(maxX, corner.X);
                maxY = Math.Max(maxY, corner.Y);
            }
            
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 获取旋转后的四个角点
        /// </summary>
        private Point[] GetRotatedCorners()
        {
            Point[] corners = {
                new Point(_rect.Left, _rect.Top),       // 左上
                new Point(_rect.Right, _rect.Top),      // 右上
                new Point(_rect.Right, _rect.Bottom),   // 右下
                new Point(_rect.Left, _rect.Bottom)     // 左下
            };

            // 如果有旋转角度，旋转所有角点
            if (Math.Abs(_rotationAngle) > 0.001)
            {
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = RotatePointAroundCenter(corners[i], _rotationCenter, _rotationAngle);
                }
            }

            return corners;
        }

        /// <summary>
        /// 围绕指定中心点旋转点
        /// </summary>
        /// <param name="point">要旋转的点</param>
        /// <param name="center">旋转中心</param>
        /// <param name="angleDegrees">旋转角度（度）</param>
        /// <returns>旋转后的点</returns>
        private static Point RotatePointAroundCenter(Point point, Point center, double angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180.0;
            double cos = Math.Cos(angleRadians);
            double sin = Math.Sin(angleRadians);

            // 平移到原点
            double x = point.X - center.X;
            double y = point.Y - center.Y;

            // 旋转
            double newX = x * cos - y * sin;
            double newY = x * sin + y * cos;

            // 平移回去
            return new Point(newX + center.X, newY + center.Y);
        }

        #endregion

        #region 静态属性 - 坐标系状态

        /// <summary>
        /// 全局坐标系状态：是否Y轴向上（用于角度计算修正）
        /// </summary>
        public static bool GlobalYAxisUp { get; set; } = false;

        #endregion

        private Rect _rect;
        private double _rotationAngle;
        private Point _rotationCenter;
        private double _cornerRadius;

    }
}