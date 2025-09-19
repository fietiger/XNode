using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XDisplay.Core;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 图像容器 - 可移动、可包含子几何图形的图像容器
    /// 支持背景图像显示和局部坐标系管理
    /// </summary>
    public class ImageContainer : GeometryBase, IGeometryContainer
    {
        #region GeometryBase 实现

        public override string TypeName => "ImageContainer";

        public override Rect Bounds
        {
            get
            {
                Rect bounds = new Rect(_position, _size);
                
                // 包含所有子元素的边界
                foreach (var child in _children)
                {
                    if (child.IsVisible)
                    {
                        Rect childWorldBounds = LocalToWorld(child.Bounds);
                        bounds.Union(childWorldBounds);
                    }
                }
                
                return bounds;
            }
        }

        #endregion

        #region IGeometryContainer 实现

        public IList<IGeometry> Children => _children.AsReadOnly();

        public LocalCoordinateSystem LocalCoordinateSystem { get; }

        public bool ClipChildren
        {
            get => _clipChildren;
            set
            {
                if (_clipChildren != value)
                {
                    _clipChildren = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        public event EventHandler<ChildGeometryEventArgs>? ChildAdded;
        public event EventHandler<ChildGeometryEventArgs>? ChildRemoved;

        #endregion

        #region 容器特有属性

        /// <summary>背景图像</summary>
        public ImageSource? BackgroundImage
        {
            get => _backgroundImage;
            set
            {
                if (_backgroundImage != value)
                {
                    _backgroundImage = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        /// <summary>容器位置（世界坐标）</summary>
        public Point Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    UpdateLocalCoordinateSystem();
                    OnGeometryChanged(GeometryChangeType.Position);
                }
            }
        }

        /// <summary>容器尺寸（世界坐标）</summary>
        public Size Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    OnGeometryChanged(GeometryChangeType.Size);
                }
            }
        }

        /// <summary>容器旋转角度（度）</summary>
        public double Rotation
        {
            get => _rotation;
            set
            {
                if (Math.Abs(_rotation - value) > 0.001)
                {
                    _rotation = value;
                    UpdateLocalCoordinateSystem();
                    OnGeometryChanged(GeometryChangeType.Rotation);
                }
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建图像容器
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">尺寸</param>
        public ImageContainer(Point position, Size size)
        {
            _position = position;
            _size = size;
            _rotation = 0.0;
            _clipChildren = true;
            _children = new List<IGeometry>();
            
            LocalCoordinateSystem = new LocalCoordinateSystem();
            UpdateLocalCoordinateSystem();
        }

        /// <summary>
        /// 创建带背景图像的容器
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">尺寸（如果为空，则自动使用图像尺寸）</param>
        /// <param name="backgroundImage">背景图像</param>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public ImageContainer(Point position, Size? size, ImageSource backgroundImage,
            double pixelToPhysicalRatio = 1.0, Point? physicalOriginInImage = null, bool yAxisUp = true)
        {
            _position = position;
            _backgroundImage = backgroundImage;
            
            // 如果未指定尺寸，使用图像的实际尺寸
            if (size.HasValue)
            {
                _size = size.Value;
            }
            else if (backgroundImage != null)
            {
                _size = new Size(backgroundImage.Width, backgroundImage.Height);
            }
            else
            {
                _size = new Size(100, 100); // 默认尺寸
            }
            
            _rotation = 0.0;
            _clipChildren = true;
            _children = new List<IGeometry>();
            
            LocalCoordinateSystem = new LocalCoordinateSystem(
                pixelToPhysicalRatio, 
                physicalOriginInImage ?? new Point(0, 0), 
                yAxisUp);
            UpdateLocalCoordinateSystem();
        }

        #endregion

        #region 静态工厂方法

        /// <summary>
        /// 创建与图像尺寸匹配的图像容器
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="backgroundImage">背景图像</param>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        /// <returns>图像容器</returns>
        public static ImageContainer CreateFromImage(Point position, ImageSource backgroundImage,
            double pixelToPhysicalRatio = 1.0, Point? physicalOriginInImage = null, bool yAxisUp = true)
        {
            return new ImageContainer(position, null, backgroundImage, pixelToPhysicalRatio, physicalOriginInImage, yAxisUp);
        }

        /// <summary>
        /// 创建自定义尺寸的图像容器
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">自定义尺寸</param>
        /// <param name="backgroundImage">背景图像</param>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        /// <returns>图像容器</returns>
        public static ImageContainer CreateWithCustomSize(Point position, Size size, ImageSource backgroundImage,
            double pixelToPhysicalRatio = 1.0, Point? physicalOriginInImage = null, bool yAxisUp = true)
        {
            return new ImageContainer(position, size, backgroundImage, pixelToPhysicalRatio, physicalOriginInImage, yAxisUp);
        }

        /// <summary>
        /// 创建空的图像容器（无背景图像）
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">尺寸</param>
        /// <returns>图像容器</returns>
        public static ImageContainer CreateEmpty(Point position, Size size)
        {
            return new ImageContainer(position, size);
        }

        #endregion

        #region IGeometryContainer 子元素管理

        public void AddChild(IGeometry child, Point localPosition)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (_children.Contains(child)) return;
            
            // 设置子元素位置到本地坐标
            child.SetPosition(localPosition);
            _children.Add(child);
            
            // 订阅子元素变化事件
            child.GeometryChanged += OnChildGeometryChanged;
            
            ChildAdded?.Invoke(this, new ChildGeometryEventArgs(this, child));
            OnGeometryChanged(GeometryChangeType.Appearance);
        }

        public bool RemoveChild(IGeometry child)
        {
            if (child == null || !_children.Contains(child)) return false;
            
            // 取消订阅子元素变化事件
            child.GeometryChanged -= OnChildGeometryChanged;
            
            _children.Remove(child);
            ChildRemoved?.Invoke(this, new ChildGeometryEventArgs(this, child));
            OnGeometryChanged(GeometryChangeType.Appearance);
            return true;
        }

        public void ClearChildren()
        {
            var childrenCopy = _children.ToArray();
            foreach (var child in childrenCopy)
            {
                RemoveChild(child);
            }
        }

        public IGeometry? FindChild(Guid id)
        {
            return _children.FirstOrDefault(c => c.Id == id);
        }

        #endregion

        #region IGeometryContainer 坐标转换

        public Point LocalToWorld(Point localPoint)
        {
            return LocalCoordinateSystem.LocalToParent(localPoint);
        }

        public Point WorldToLocal(Point worldPoint)
        {
            return LocalCoordinateSystem.ParentToLocal(worldPoint);
        }

        public Rect LocalToWorld(Rect localRect)
        {
            return LocalCoordinateSystem.LocalToParent(localRect);
        }

        public Rect WorldToLocal(Rect worldRect)
        {
            return LocalCoordinateSystem.ParentToLocal(worldRect);
        }

        #endregion

        #region GeometryBase 核心方法实现

        public override void Render(DrawingContext context, ViewportTransform? transform)
        {
            if (!IsVisible) return;

            ApplyRenderTransform(context, dc =>
            {
                // 绘制背景图像
                if (_backgroundImage != null)
                {
                    Rect imageRect = new Rect(_position, _size);
                    if (transform != null)
                    {
                        imageRect = transform.WorldToScreen(imageRect);
                    }
                    
                    // 应用旋转变换
                    if (Math.Abs(_rotation) > 0.001)
                    {
                        Point center = new Point(imageRect.X + imageRect.Width / 2, imageRect.Y + imageRect.Height / 2);
                        dc.PushTransform(new RotateTransform(_rotation, center.X, center.Y));
                        dc.DrawImage(_backgroundImage, imageRect);
                        dc.Pop();
                    }
                    else
                    {
                        dc.DrawImage(_backgroundImage, imageRect);
                    }
                }

                // 绘制容器边框
                if (Stroke != null)
                {
                    Rect containerRect = new Rect(_position, _size);
                    if (transform != null)
                    {
                        containerRect = transform.WorldToScreen(containerRect);
                    }
                    
                    if (Math.Abs(_rotation) > 0.001)
                    {
                        Point center = new Point(containerRect.X + containerRect.Width / 2, containerRect.Y + containerRect.Height / 2);
                        dc.PushTransform(new RotateTransform(_rotation, center.X, center.Y));
                        dc.DrawRectangle(Fill, Stroke, containerRect);
                        dc.Pop();
                    }
                    else
                    {
                        dc.DrawRectangle(Fill, Stroke, containerRect);
                    }
                }

                // 设置剪裁区域（如果启用）
                if (_clipChildren)
                {
                    Rect clipRect = new Rect(_position, _size);
                    if (transform != null)
                    {
                        clipRect = transform.WorldToScreen(clipRect);
                    }
                    
                    if (Math.Abs(_rotation) > 0.001)
                    {
                        Point center = new Point(clipRect.X + clipRect.Width / 2, clipRect.Y + clipRect.Height / 2);
                        dc.PushTransform(new RotateTransform(_rotation, center.X, center.Y));
                        dc.PushClip(new RectangleGeometry(clipRect));
                    }
                    else
                    {
                        dc.PushClip(new RectangleGeometry(clipRect));
                    }
                }

                // 绘制子元素
                foreach (var child in _children.Where(c => c.IsVisible).OrderBy(c => c.ZOrder))
                {
                    child.Render(dc, transform);
                }

                // 恢复剪裁
                if (_clipChildren)
                {
                    dc.Pop(); // 剪裁
                    if (Math.Abs(_rotation) > 0.001)
                    {
                        dc.Pop(); // 旋转变换
                    }
                }
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            // 首先检查容器本身
            Rect containerBounds = new Rect(_position, _size);
            if (IsPointInRect(worldPoint, containerBounds, tolerance))
            {
                return true;
            }

            // 检查子元素
            Point localPoint = WorldToLocal(worldPoint);
            return _children.Any(child => child.IsVisible && child.HitTest(localPoint, tolerance));
        }

        public override bool HitTest(Rect worldRect)
        {
            // 检查容器本身
            Rect containerBounds = new Rect(_position, _size);
            if (worldRect.IntersectsWith(containerBounds))
            {
                return true;
            }

            // 检查子元素
            Rect localRect = WorldToLocal(worldRect);
            return _children.Any(child => child.IsVisible && child.HitTest(localRect));
        }

        public override IGeometry Clone()
        {
            var clone = new ImageContainer(_position, _size)
            {
                BackgroundImage = _backgroundImage,
                Rotation = _rotation,
                ClipChildren = _clipChildren,
                Fill = Fill,
                Stroke = Stroke,
                Opacity = Opacity,
                ZOrder = ZOrder,
                IsVisible = IsVisible
            };

            // 克隆子元素
            foreach (var child in _children)
            {
                clone.AddChild(child.Clone(), child.Bounds.Location);
            }

            return clone;
        }

        #endregion

        #region GeometryBase 变换方法实现

        public override void Move(double deltaX, double deltaY)
        {
            Position = new Point(_position.X + deltaX, _position.Y + deltaY);
        }

        public override void SetPosition(Point worldPoint)
        {
            Position = worldPoint;
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            // 缩放容器尺寸
            _size = new Size(_size.Width * scaleX, _size.Height * scaleY);
            
            // 调整位置
            Vector offset = (Vector)(_position - origin);
            offset = new Vector(offset.X * scaleX, offset.Y * scaleY);
            _position = origin + offset;
            
            UpdateLocalCoordinateSystem();
            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override void Rotate(double angle, Point center)
        {
            _rotation += angle;
            
            // 围绕指定中心点旋转容器位置
            Matrix rotMatrix = Matrix.Identity;
            rotMatrix.RotateAt(angle, center.X, center.Y);
            _position = rotMatrix.Transform(_position);
            
            UpdateLocalCoordinateSystem();
            OnGeometryChanged(GeometryChangeType.Rotation);
        }

        #endregion

        #region GeometryBase 编辑支持实现

        public override Point[] GetControlPoints()
        {
            var points = new List<Point>();
            
            // 容器的四个角点（用于调整大小）
            points.Add(_position); // 左上角
            points.Add(new Point(_position.X + _size.Width, _position.Y)); // 右上角
            points.Add(new Point(_position.X + _size.Width, _position.Y + _size.Height)); // 右下角
            points.Add(new Point(_position.X, _position.Y + _size.Height)); // 左下角
            
            // 中心点（用于移动）
            points.Add(new Point(_position.X + _size.Width / 2, _position.Y + _size.Height / 2));
            
            // 旋转控制点
            Point rotationPoint = new Point(_position.X + _size.Width / 2, _position.Y - 20);
            points.Add(rotationPoint);
            
            return points.ToArray();
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            switch (pointIndex)
            {
                case 0: // 左上角 - 调整位置和大小
                    {
                        double newWidth = _position.X + _size.Width - newWorldPoint.X;
                        double newHeight = _position.Y + _size.Height - newWorldPoint.Y;
                        if (newWidth > 0 && newHeight > 0)
                        {
                            _position = newWorldPoint;
                            _size = new Size(newWidth, newHeight);
                        }
                        break;
                    }
                case 1: // 右上角
                    {
                        double newWidth = newWorldPoint.X - _position.X;
                        double newHeight = _position.Y + _size.Height - newWorldPoint.Y;
                        if (newWidth > 0 && newHeight > 0)
                        {
                            _position = new Point(_position.X, newWorldPoint.Y);
                            _size = new Size(newWidth, newHeight);
                        }
                        break;
                    }
                case 2: // 右下角
                    {
                        double newWidth = newWorldPoint.X - _position.X;
                        double newHeight = newWorldPoint.Y - _position.Y;
                        if (newWidth > 0 && newHeight > 0)
                        {
                            _size = new Size(newWidth, newHeight);
                        }
                        break;
                    }
                case 3: // 左下角
                    {
                        double newWidth = _position.X + _size.Width - newWorldPoint.X;
                        double newHeight = newWorldPoint.Y - _position.Y;
                        if (newWidth > 0 && newHeight > 0)
                        {
                            _position = new Point(newWorldPoint.X, _position.Y);
                            _size = new Size(newWidth, newHeight);
                        }
                        break;
                    }
                case 4: // 中心点 - 移动
                    {
                        Point center = new Point(_position.X + _size.Width / 2, _position.Y + _size.Height / 2);
                        Vector offset = newWorldPoint - center;
                        Move(offset.X, offset.Y);
                        break;
                    }
                case 5: // 旋转控制点
                    {
                        Point center = new Point(_position.X + _size.Width / 2, _position.Y + _size.Height / 2);
                        Vector vector = newWorldPoint - center;
                        double angle = Math.Atan2(vector.Y, vector.X) * 180.0 / Math.PI + 90;
                        Rotation = angle;
                        break;
                    }
            }
            
            UpdateLocalCoordinateSystem();
            OnGeometryChanged(GeometryChangeType.Size);
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            return pointIndex switch
            {
                0 or 1 or 2 or 3 => ControlPointType.Resize,
                4 => ControlPointType.Move,
                5 => ControlPointType.Rotate,
                _ => ControlPointType.Custom
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新局部坐标系
        /// </summary>
        private void UpdateLocalCoordinateSystem()
        {
            Point center = new Point(_position.X + _size.Width / 2, _position.Y + _size.Height / 2);
            LocalCoordinateSystem.SetTransform(center, 1.0, _rotation);
        }

        /// <summary>
        /// 子元素几何变化事件处理
        /// </summary>
        private void OnChildGeometryChanged(object? sender, GeometryChangedEventArgs e)
        {
            // 传播子元素的变化
            OnGeometryChanged(GeometryChangeType.Appearance);
        }

        #endregion

        #region 字段

        private Point _position;
        private Size _size;
        private double _rotation;
        private bool _clipChildren;
        private ImageSource? _backgroundImage;
        private readonly List<IGeometry> _children;

        #endregion
    }
}