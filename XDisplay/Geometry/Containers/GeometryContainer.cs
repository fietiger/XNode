using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XDisplay.Core;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 通用几何容器 - 基于现有几何图形的容器实现
    /// 可以将任何几何图形作为容器，支持子元素管理和坐标系
    /// </summary>
    public class GeometryContainer : GeometryBase, IGeometryContainer
    {
        #region GeometryBase 实现

        public override string TypeName => $"{_baseGeometry.TypeName}Container";

        public override Rect Bounds
        {
            get
            {
                Rect bounds = _baseGeometry.Bounds;
                
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

        /// <summary>基础几何图形</summary>
        public IGeometry BaseGeometry => _baseGeometry;

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

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建几何容器
        /// </summary>
        /// <param name="baseGeometry">基础几何图形</param>
        public GeometryContainer(IGeometry baseGeometry)
        {
            _baseGeometry = baseGeometry ?? throw new ArgumentNullException(nameof(baseGeometry));
            _clipChildren = false;
            _children = new List<IGeometry>();
            
            LocalCoordinateSystem = new LocalCoordinateSystem();
            
            // 同步基础几何图形的属性
            SyncFromBaseGeometry();
            
            // 订阅基础几何图形的变化
            _baseGeometry.GeometryChanged += OnBaseGeometryChanged;
        }

        /// <summary>
        /// 创建带背景图像的几何容器
        /// </summary>
        /// <param name="baseGeometry">基础几何图形</param>
        /// <param name="backgroundImage">背景图像</param>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public GeometryContainer(IGeometry baseGeometry, ImageSource backgroundImage,
            double pixelToPhysicalRatio = 1.0, Point? physicalOriginInImage = null, bool yAxisUp = true)
        {
            _baseGeometry = baseGeometry ?? throw new ArgumentNullException(nameof(baseGeometry));
            _backgroundImage = backgroundImage;
            _clipChildren = true; // 有背景图像时默认启用剪裁
            _children = new List<IGeometry>();
            
            LocalCoordinateSystem = new LocalCoordinateSystem(
                pixelToPhysicalRatio, 
                physicalOriginInImage ?? new Point(0, 0), 
                yAxisUp);
            
            // 同步基础几何图形的属性
            SyncFromBaseGeometry();
            
            // 订阅基础几何图形的变化
            _baseGeometry.GeometryChanged += OnBaseGeometryChanged;
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
                    Rect imageRect = _baseGeometry.Bounds;
                    if (transform != null)
                    {
                        imageRect = transform.WorldToScreen(imageRect);
                    }
                    dc.DrawImage(_backgroundImage, imageRect);
                }

                // 绘制基础几何图形
                _baseGeometry.Render(dc, transform);

                // 设置剪裁区域（如果启用）
                if (_clipChildren)
                {
                    Rect clipRect = _baseGeometry.Bounds;
                    if (transform != null)
                    {
                        clipRect = transform.WorldToScreen(clipRect);
                    }
                    
                    // 根据基础几何图形的类型创建合适的剪裁几何
                    System.Windows.Media.Geometry clipGeometry = CreateClipGeometry(clipRect);
                    dc.PushClip(clipGeometry);
                }

                // 绘制子元素
                foreach (var child in _children.Where(c => c.IsVisible).OrderBy(c => c.ZOrder))
                {
                    child.Render(dc, transform);
                }

                // 恢复剪裁
                if (_clipChildren)
                {
                    dc.Pop();
                }
            });
        }

        public override bool HitTest(Point worldPoint, double tolerance = 1.0)
        {
            // 首先检查基础几何图形
            if (_baseGeometry.HitTest(worldPoint, tolerance))
            {
                return true;
            }

            // 检查子元素
            Point localPoint = WorldToLocal(worldPoint);
            return _children.Any(child => child.IsVisible && child.HitTest(localPoint, tolerance));
        }

        public override bool HitTest(Rect worldRect)
        {
            // 检查基础几何图形
            if (_baseGeometry.HitTest(worldRect))
            {
                return true;
            }

            // 检查子元素
            Rect localRect = WorldToLocal(worldRect);
            return _children.Any(child => child.IsVisible && child.HitTest(localRect));
        }

        public override IGeometry Clone()
        {
            var clone = new GeometryContainer(_baseGeometry.Clone())
            {
                BackgroundImage = _backgroundImage,
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
            _baseGeometry.Move(deltaX, deltaY);
            UpdateLocalCoordinateSystem();
        }

        public override void SetPosition(Point worldPoint)
        {
            _baseGeometry.SetPosition(worldPoint);
            UpdateLocalCoordinateSystem();
        }

        public override void Scale(double scaleX, double scaleY, Point origin)
        {
            _baseGeometry.Scale(scaleX, scaleY, origin);
            UpdateLocalCoordinateSystem();
        }

        public override void Rotate(double angle, Point center)
        {
            _baseGeometry.Rotate(angle, center);
            UpdateLocalCoordinateSystem();
        }

        #endregion

        #region GeometryBase 编辑支持实现

        public override Point[] GetControlPoints()
        {
            // 返回基础几何图形的控制点
            return _baseGeometry.GetControlPoints();
        }

        public override void UpdateControlPoint(int pointIndex, Point newWorldPoint)
        {
            _baseGeometry.UpdateControlPoint(pointIndex, newWorldPoint);
            UpdateLocalCoordinateSystem();
        }

        public override ControlPointType GetControlPointType(int pointIndex)
        {
            return _baseGeometry.GetControlPointType(pointIndex);
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 添加子元素到物理坐标位置
        /// </summary>
        /// <param name="child">子几何图形</param>
        /// <param name="physicalPosition">物理坐标位置</param>
        public void AddChildAtPhysicalPosition(IGeometry child, Point physicalPosition)
        {
            Point localPosition = LocalCoordinateSystem.PhysicalToLocal(physicalPosition);
            AddChild(child, localPosition);
        }

        /// <summary>
        /// 设置物理坐标系参数
        /// </summary>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public void SetPhysicalCoordinateSystem(double pixelToPhysicalRatio, Point physicalOriginInImage, bool yAxisUp = true)
        {
            LocalCoordinateSystem.PixelToPhysicalRatio = pixelToPhysicalRatio;
            LocalCoordinateSystem.PhysicalOriginInImage = physicalOriginInImage;
            LocalCoordinateSystem.YAxisUp = yAxisUp;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 从基础几何图形同步属性
        /// </summary>
        private void SyncFromBaseGeometry()
        {
            Fill = _baseGeometry.Fill;
            Stroke = _baseGeometry.Stroke;
            Opacity = _baseGeometry.Opacity;
            ZOrder = _baseGeometry.ZOrder;
            IsVisible = _baseGeometry.IsVisible;
            
            UpdateLocalCoordinateSystem();
        }

        /// <summary>
        /// 更新局部坐标系
        /// </summary>
        private void UpdateLocalCoordinateSystem()
        {
            Rect bounds = _baseGeometry.Bounds;
            Point center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            LocalCoordinateSystem.SetTransform(center, 1.0, 0.0);
        }

        /// <summary>
        /// 创建剪裁几何图形
        /// </summary>
        /// <param name="bounds">边界矩形</param>
        /// <returns>剪裁几何图形</returns>
        private System.Windows.Media.Geometry CreateClipGeometry(Rect bounds)
        {
            // 根据基础几何图形类型创建合适的剪裁几何
            // 目前使用矩形剪裁，后续可以根据具体几何图形类型优化
            return new RectangleGeometry(bounds);
        }

        /// <summary>
        /// 基础几何图形变化事件处理
        /// </summary>
        private void OnBaseGeometryChanged(object? sender, GeometryChangedEventArgs e)
        {
            // 同步基础几何图形的属性变化
            if (e.ChangeType == GeometryChangeType.Position || 
                e.ChangeType == GeometryChangeType.Size || 
                e.ChangeType == GeometryChangeType.Rotation)
            {
                UpdateLocalCoordinateSystem();
            }
            
            // 传播变化事件
            OnGeometryChanged(e.ChangeType);
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

        private readonly IGeometry _baseGeometry;
        private bool _clipChildren;
        private ImageSource? _backgroundImage;
        private readonly List<IGeometry> _children;

        #endregion
    }
}