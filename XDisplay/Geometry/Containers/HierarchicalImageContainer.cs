using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XDisplay.Core;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 分层图像容器 - 支持多层嵌套坐标系的图像容器
    /// 结合了ImageContainer的功能和HierarchicalCoordinateSystem的分层管理
    /// </summary>
    public class HierarchicalImageContainer : GeometryBase, IHierarchicalContainer
    {
        #region GeometryBase 实现

        public override string TypeName => "HierarchicalImageContainer";

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
                
                // 包含所有子容器的边界
                foreach (var childContainer in _childContainers)
                {
                    if (childContainer.IsVisible)
                    {
                        bounds.Union(childContainer.Bounds);
                    }
                }
                
                return bounds;
            }
        }

        #endregion

        #region IGeometryContainer 实现

        public IList<IGeometry> Children => _children.AsReadOnly();

        public LocalCoordinateSystem LocalCoordinateSystem => HierarchicalCoordinateSystem.LocalCoordinateSystem;

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

        #region IHierarchicalContainer 实现

        public HierarchicalCoordinateSystem HierarchicalCoordinateSystem { get; }

        public IHierarchicalContainer? ParentContainer
        {
            get => _parentContainer;
            set
            {
                if (_parentContainer != value)
                {
                    // 从旧父容器移除
                    // 从旧父容器移除
                    if (_parentContainer != null)
                    {
                        var parentAsContainer = _parentContainer as HierarchicalImageContainer;
                        parentAsContainer?._childContainers.Remove(this);
                    }
                    
                    _parentContainer = value;
                    
                    // 添加到新父容器
                    if (_parentContainer != null)
                    {
                        var newParentAsContainer = _parentContainer as HierarchicalImageContainer;
                        newParentAsContainer?._childContainers.Add(this);
                        // 更新分层坐标系的父级关系
                        HierarchicalCoordinateSystem.Parent = _parentContainer.HierarchicalCoordinateSystem;
                    }
                    else
                    {
                        HierarchicalCoordinateSystem.Parent = null;
                    }
                }
            }
        }

        public IReadOnlyList<IHierarchicalContainer> ChildContainers => _childContainers.AsReadOnly();

        public string ContainerName
        {
            get => HierarchicalCoordinateSystem.Name;
            set
            {
                // HierarchicalCoordinateSystem的Name是只读的，这里我们需要重新创建或修改实现
                // 暂时通过Description来记录用户设置的名称
                ContainerDescription = $"用户名称: {value}";
            }
        }

        public string ContainerDescription
        {
            get => HierarchicalCoordinateSystem.Description;
            set => HierarchicalCoordinateSystem.Description = value;
        }

        public bool IsRootContainer => ParentContainer == null;

        public int ContainerLevel => HierarchicalCoordinateSystem.Level;

        public string FullContainerPath => HierarchicalCoordinateSystem.FullPath;

        public event EventHandler<ChildContainerEventArgs>? ChildContainerAdded;
        public event EventHandler<ChildContainerEventArgs>? ChildContainerRemoved;
        public event EventHandler<ContainerTransformChangedEventArgs>? ContainerTransformChanged;

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
                    UpdateCoordinateSystemTransform();
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
                    UpdateCoordinateSystemTransform();
                    OnGeometryChanged(GeometryChangeType.Rotation);
                    OnContainerTransformChanged(ContainerTransformType.Rotation);
                }
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建根分层图像容器
        /// </summary>
        /// <param name="name">容器名称</param>
        /// <param name="position">位置</param>
        /// <param name="size">尺寸</param>
        /// <param name="description">容器描述</param>
        public HierarchicalImageContainer(string name, Point position, Size size, string description = "")
        {
            HierarchicalCoordinateSystem = new HierarchicalCoordinateSystem(name, description);
            _position = position;
            _size = size;
            _rotation = 0.0;
            _clipChildren = true;
            _children = new List<IGeometry>();
            _childContainers = new List<IHierarchicalContainer>();
            
            UpdateCoordinateSystemTransform();
        }

        /// <summary>
        /// 创建子分层图像容器
        /// </summary>
        /// <param name="name">容器名称</param>
        /// <param name="parentContainer">父容器</param>
        /// <param name="localPosition">在父容器坐标系中的位置</param>
        /// <param name="size">尺寸</param>
        /// <param name="description">容器描述</param>
        public HierarchicalImageContainer(string name, IHierarchicalContainer parentContainer, 
            Point localPosition, Size size, string description = "")
        {
            HierarchicalCoordinateSystem = new HierarchicalCoordinateSystem(name, 
                parentContainer.HierarchicalCoordinateSystem, description);
            
            _position = localPosition;
            _size = size;
            _rotation = 0.0;
            _clipChildren = true;
            _children = new List<IGeometry>();
            _childContainers = new List<IHierarchicalContainer>();
            
            ParentContainer = parentContainer;
            UpdateCoordinateSystemTransform();
        }

        /// <summary>
        /// 创建带背景图像的分层容器
        /// </summary>
        /// <param name="name">容器名称</param>
        /// <param name="parentContainer">父容器（null表示根容器）</param>
        /// <param name="localPosition">在父容器坐标系中的位置</param>
        /// <param name="size">尺寸（如果为空，则自动使用图像尺寸）</param>
        /// <param name="backgroundImage">背景图像</param>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        /// <param name="description">容器描述</param>
        public HierarchicalImageContainer(string name, IHierarchicalContainer? parentContainer,
            Point localPosition, Size? size, ImageSource backgroundImage,
            double pixelToPhysicalRatio = 1.0, Point? physicalOriginInImage = null, bool yAxisUp = true,
            string description = "")
        {
            if (parentContainer != null)
            {
                HierarchicalCoordinateSystem = new HierarchicalCoordinateSystem(name, 
                    parentContainer.HierarchicalCoordinateSystem, localPosition, 1.0, 0.0, yAxisUp, description);
            }
            else
            {
                HierarchicalCoordinateSystem = new HierarchicalCoordinateSystem(name, description);
            }
            
            _backgroundImage = backgroundImage;
            _position = localPosition;
            
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
            _childContainers = new List<IHierarchicalContainer>();
            
            // 设置物理坐标映射
            LocalCoordinateSystem.PixelToPhysicalRatio = pixelToPhysicalRatio;
            LocalCoordinateSystem.PhysicalOriginInImage = physicalOriginInImage ?? new Point(0, 0);
            LocalCoordinateSystem.YAxisUp = yAxisUp;
            
            ParentContainer = parentContainer;
            UpdateCoordinateSystemTransform();
        }

        #endregion

        #region IGeometryContainer 子元素管理

        public void AddChild(IGeometry child, Point localPosition)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (_children.Contains(child)) return;
            
            child.SetPosition(localPosition);
            _children.Add(child);
            
            child.GeometryChanged += OnChildGeometryChanged;
            
            ChildAdded?.Invoke(this, new ChildGeometryEventArgs(this, child));
            OnGeometryChanged(GeometryChangeType.Appearance);
        }

        public bool RemoveChild(IGeometry child)
        {
            if (child == null || !_children.Contains(child)) return false;
            
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

        #region IHierarchicalContainer 分层容器管理

        public void AddChildContainer(IHierarchicalContainer childContainer, Point localPosition)
        {
            if (childContainer == null) throw new ArgumentNullException(nameof(childContainer));
            if (_childContainers.Contains(childContainer)) return;
            
            // 设置父子关系（这会自动处理坐标系的父子关系）
            childContainer.ParentContainer = this;
            
            // 设置位置
            if (childContainer is HierarchicalImageContainer hierarchicalChild)
            {
                hierarchicalChild.Position = localPosition;
            }
            
            ChildContainerAdded?.Invoke(this, new ChildContainerEventArgs(this, childContainer));
            OnGeometryChanged(GeometryChangeType.Appearance);
        }

        public bool RemoveChildContainer(IHierarchicalContainer childContainer)
        {
            if (childContainer == null || !_childContainers.Contains(childContainer)) return false;
            
            childContainer.ParentContainer = null;
            ChildContainerRemoved?.Invoke(this, new ChildContainerEventArgs(this, childContainer));
            OnGeometryChanged(GeometryChangeType.Appearance);
            return true;
        }

        public IHierarchicalContainer? FindChildContainer(string containerName)
        {
            return _childContainers.FirstOrDefault(c => c.HierarchicalCoordinateSystem.Name == containerName);
        }

        public IHierarchicalContainer? FindDescendantContainer(string containerName)
        {
            var directChild = FindChildContainer(containerName);
            if (directChild != null) return directChild;

            foreach (var child in _childContainers)
            {
                var descendant = child.FindDescendantContainer(containerName);
                if (descendant != null) return descendant;
            }

            return null;
        }

        public IHierarchicalContainer? FindContainerByPath(string path)
        {
            var root = GetRootContainer();
            var coordinateSystem = root.HierarchicalCoordinateSystem.FindByPath(path);
            // 这里需要通过其他方式找到对应的容器
            // 简化实现，返回null
            return null;
        }

        public IHierarchicalContainer GetRootContainer()
        {
            var current = this;
            while (current.ParentContainer != null)
            {
                current = current.ParentContainer as HierarchicalImageContainer;
                if (current == null) break;
            }
            return current;
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

        #region IHierarchicalContainer 分层坐标转换

        public Point TransformToRoot(Point localPoint)
        {
            return HierarchicalCoordinateSystem.ToRoot(localPoint);
        }

        public Point TransformFromRoot(Point rootPoint)
        {
            return HierarchicalCoordinateSystem.FromRoot(rootPoint);
        }

        public Point TransformToContainer(Point localPoint, IHierarchicalContainer targetContainer)
        {
            return HierarchicalCoordinateSystem.TransformTo(localPoint, targetContainer.HierarchicalCoordinateSystem);
        }

        public Point TransformFromContainer(Point sourcePoint, IHierarchicalContainer sourceContainer)
        {
            return HierarchicalCoordinateSystem.TransformFrom(sourcePoint, sourceContainer.HierarchicalCoordinateSystem);
        }

        public Rect TransformRectToRoot(Rect localRect)
        {
            Point topLeft = TransformToRoot(localRect.TopLeft);
            Point bottomRight = TransformToRoot(localRect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        public Rect TransformRectFromRoot(Rect rootRect)
        {
            Point topLeft = TransformFromRoot(rootRect.TopLeft);
            Point bottomRight = TransformFromRoot(rootRect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        #endregion

        #region IHierarchicalContainer 分层容器变换

        public void SetContainerTransform(Point origin, double scale = 1.0, double rotation = 0.0)
        {
            _position = origin;
            LocalCoordinateSystem.Scale = scale;
            _rotation = rotation;
            UpdateCoordinateSystemTransform();
            OnContainerTransformChanged(ContainerTransformType.Combined);
        }

        public void SetPhysicalCoordinateMapping(double pixelToPhysicalRatio, Point physicalOriginInImage, bool yAxisUp = true)
        {
            LocalCoordinateSystem.PixelToPhysicalRatio = pixelToPhysicalRatio;
            LocalCoordinateSystem.PhysicalOriginInImage = physicalOriginInImage;
            LocalCoordinateSystem.YAxisUp = yAxisUp;
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

                // 绘制子几何图形
                foreach (var child in _children.Where(c => c.IsVisible).OrderBy(c => c.ZOrder))
                {
                    child.Render(dc, transform);
                }

                // 绘制子容器
                foreach (var childContainer in _childContainers.Where(c => c.IsVisible).OrderBy(c => c.ZOrder))
                {
                    childContainer.Render(dc, transform);
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

            // 检查子几何图形
            Point localPoint = WorldToLocal(worldPoint);
            if (_children.Any(child => child.IsVisible && child.HitTest(localPoint, tolerance)))
            {
                return true;
            }

            // 检查子容器
            return _childContainers.Any(childContainer => childContainer.IsVisible && childContainer.HitTest(worldPoint, tolerance));
        }

        public override bool HitTest(Rect worldRect)
        {
            // 检查容器本身
            Rect containerBounds = new Rect(_position, _size);
            if (worldRect.IntersectsWith(containerBounds))
            {
                return true;
            }

            // 检查子几何图形
            Rect localRect = WorldToLocal(worldRect);
            if (_children.Any(child => child.IsVisible && child.HitTest(localRect)))
            {
                return true;
            }

            // 检查子容器
            return _childContainers.Any(childContainer => childContainer.IsVisible && childContainer.HitTest(worldRect));
        }

        public override IGeometry Clone()
        {
            var clone = new HierarchicalImageContainer(HierarchicalCoordinateSystem.Name, null, _position, _size, HierarchicalCoordinateSystem.Description)
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

            // 克隆子几何图形
            foreach (var child in _children)
            {
                clone.AddChild(child.Clone(), child.Bounds.Location);
            }

            // 克隆子容器
            foreach (var childContainer in _childContainers)
            {
                var clonedChild = childContainer.Clone() as HierarchicalImageContainer;
                if (clonedChild != null)
                {
                    clone.AddChildContainer(clonedChild, new Point(0, 0)); // 位置会在克隆过程中恢复
                }
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
            
            // 更新坐标系缩放
            LocalCoordinateSystem.Scale *= Math.Sqrt(scaleX * scaleY); // 使用几何平均值
            
            UpdateCoordinateSystemTransform();
            OnGeometryChanged(GeometryChangeType.Size);
            OnContainerTransformChanged(ContainerTransformType.Scale);
        }

        public override void Rotate(double angle, Point center)
        {
            _rotation += angle;
            
            // 围绕指定中心点旋转容器位置
            Matrix rotMatrix = Matrix.Identity;
            rotMatrix.RotateAt(angle, center.X, center.Y);
            _position = rotMatrix.Transform(_position);
            
            UpdateCoordinateSystemTransform();
            OnGeometryChanged(GeometryChangeType.Rotation);
            OnContainerTransformChanged(ContainerTransformType.Rotation);
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
                case 0: // 左上角
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
            
            UpdateCoordinateSystemTransform();
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
        /// 更新坐标系变换
        /// </summary>
        private void UpdateCoordinateSystemTransform()
        {
            Point center = new Point(_position.X + _size.Width / 2, _position.Y + _size.Height / 2);
            LocalCoordinateSystem.SetTransform(center, LocalCoordinateSystem.Scale, _rotation);
        }

        /// <summary>
        /// 子几何图形变化事件处理
        /// </summary>
        private void OnChildGeometryChanged(object? sender, GeometryChangedEventArgs e)
        {
            OnGeometryChanged(GeometryChangeType.Appearance);
        }

        /// <summary>
        /// 触发容器变换改变事件
        /// </summary>
        private void OnContainerTransformChanged(ContainerTransformType transformType)
        {
            ContainerTransformChanged?.Invoke(this, new ContainerTransformChangedEventArgs(this, transformType));
        }

        #endregion

        #region 字段

        private Point _position;
        private Size _size;
        private double _rotation;
        private bool _clipChildren;
        private ImageSource? _backgroundImage;
        private readonly List<IGeometry> _children;
        private readonly List<IHierarchicalContainer> _childContainers;
        private IHierarchicalContainer? _parentContainer;

        #endregion
    }
}