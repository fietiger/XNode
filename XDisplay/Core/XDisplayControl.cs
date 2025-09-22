using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XDisplay.Core;
using XDisplay.Geometry;
using XDisplay.Geometry.Shapes;
using XDisplay.Layers;

namespace XDisplay.Core
{
    /// <summary>
    /// XDisplay主控件 - 高效的显示控件，支持图像显示、几何图形绘制和交互编辑
    /// 基于XNode项目的高效绘制架构设计
    /// </summary>
    public class XDisplayControl : UserControl
    {
        #region 依赖属性

        public static readonly DependencyProperty BackgroundGridVisibleProperty =
            DependencyProperty.Register(nameof(BackgroundGridVisible), typeof(bool), typeof(XDisplayControl),
                new PropertyMetadata(true, OnBackgroundGridVisibleChanged));

        /// <summary>背景网格是否可见</summary>
        public bool BackgroundGridVisible
        {
            get => (bool)GetValue(BackgroundGridVisibleProperty);
            set => SetValue(BackgroundGridVisibleProperty, value);
        }

        #endregion

        #region 属性

        /// <summary>视口变换管理器</summary>
        public ViewportTransform ViewportTransform { get; }

        /// <summary>图像图层</summary>
        public ImageLayer ImageLayer { get; }

        /// <summary>几何图形图层</summary>
        public GeometryLayer GeometryLayer { get; }

        /// <summary>选择图层</summary>
        public SelectionLayer SelectionLayer { get; }

        /// <summary>当前交互模式</summary>
        public InteractionMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (_currentMode != value)
                {
                    _currentMode = value;
                    UpdateCursor();
                }
            }
        }

        /// <summary>选中的几何图形列表</summary>
        public IReadOnlyList<IGeometry> SelectedGeometries => GeometryLayer.SelectedGeometries;

        /// <summary>网格图层</summary>
        public GridLayer GridLayer => _gridLayer;

        /// <summary>Y轴是否向上（默认false，向下为正Y；true时向上为正Y）</summary>
        public bool YAxisUp
        {
            get => ViewportTransform.YAxisUp;
            set
            {
                ViewportTransform.YAxisUp = value;
                // 同步全局状态
                Geometry.Shapes.RotatedRectangleGeometry.GlobalYAxisUp = value;
            }
        }

        /// <summary>是否有可见内容</summary>
        public bool HasVisibleContent
        {
            get
            {
                // 检查是否有可见的几何图形
                if (GeometryLayer.Geometries.Any(g => g.IsVisible))
                    return true;
                
                // 检查是否有可见的图像
                if (ImageLayer.Images.Any(img => img.IsVisible))
                    return true;
                
                return false;
            }
        }

        #endregion

        #region 事件

        /// <summary>几何图形选择改变时触发</summary>
        public event EventHandler<GeometrySelectionChangedEventArgs>? SelectionChanged;

        /// <summary>几何图形被添加时触发</summary>
        public event EventHandler<GeometryEventArgs>? GeometryAdded;

        /// <summary>几何图形被移除时触发</summary>
        public event EventHandler<GeometryEventArgs>? GeometryRemoved;

        /// <summary>视口变换改变时触发</summary>
        public event EventHandler? ViewportTransformChanged;

        /// <summary>内容改变时触发（图像或几何图形添加/移除/可见性改变）</summary>
        public event EventHandler? ContentChanged;

        /// <summary>鼠标坐标改变时触发</summary>
        public event EventHandler<MouseCoordinateChangedEventArgs>? MouseCoordinateChanged;

        #endregion

        #region 构造函数

        public XDisplayControl()
        {
            // 初始化视口变换
            ViewportTransform = new ViewportTransform();
            ViewportTransform.TransformChanged += OnViewportTransformChanged;

            // 初始化图层
            ImageLayer = new ImageLayer();
            GeometryLayer = new GeometryLayer();
            SelectionLayer = new SelectionLayer();
            _drawingPreviewLayer = new DrawingPreviewLayer(); // 初始化绘制预览图层

            // 设置图层变换
            ImageLayer.Transform = ViewportTransform;
            GeometryLayer.Transform = ViewportTransform;
            SelectionLayer.Transform = ViewportTransform;
            _drawingPreviewLayer.Transform = ViewportTransform;

            // 初始化显示画布
            _displayCanvas = new DisplayCanvas();

            // 初始化背景网格图层
            _gridLayer = new GridLayer();
            _gridLayer.Transform = ViewportTransform;

            // 添加图层到画布（按Z序添加）
            _displayCanvas.AddVisualElement(_gridLayer.DrawingVisual);
            _displayCanvas.AddVisualElement(ImageLayer.DrawingVisual);
            _displayCanvas.AddVisualElement(GeometryLayer.DrawingVisual);
            _displayCanvas.AddVisualElement(SelectionLayer.DrawingVisual);
            _displayCanvas.AddVisualElement(_drawingPreviewLayer.DrawingVisual); // 添加预览图层到最顶层

            // 设置用户控件内容
            Content = _displayCanvas;

            // 绑定事件
            GeometryLayer.SelectionChanged += OnGeometrySelectionChanged;
            ImageLayer.ImageContentChanged += (sender, args) => OnContentChanged();
            
            // 初始化交互
            _currentMode = InteractionMode.Select;
            InitializeInteraction();

            // 设置默认属性
            Focusable = true;
            ClipToBounds = true;
            
            // 设置布局属性以适应容器
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }

        #endregion

        #region 公开方法 - 图像操作

        /// <summary>
        /// 添加图像
        /// </summary>
        /// <param name="image">图像源</param>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="worldSize">世界坐标尺寸</param>
        /// <returns>图像ID</returns>
        public Guid AddImage(ImageSource image, Point worldPosition, Size? worldSize = null)
        {
            var result = ImageLayer.AddImage(image, worldPosition, worldSize);
            OnContentChanged();
            return result;
        }

        /// <summary>
        /// 移除图像
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveImage(Guid imageId)
        {
            bool result = ImageLayer.RemoveImage(imageId);
            if (result)
            {
                OnContentChanged();
            }
            return result;
        }

        /// <summary>
        /// 清空所有图像
        /// </summary>
        public void ClearImages()
        {
            bool hadImages = ImageLayer.Images.Count > 0;
            ImageLayer.ClearImages();
            if (hadImages)
            {
                OnContentChanged();
            }
        }

        #endregion

        #region 公开方法 - 几何图形操作

        /// <summary>
        /// 添加几何图形
        /// </summary>
        /// <param name="geometry">几何图形</param>
        public void AddGeometry(IGeometry geometry)
        {
            GeometryLayer.AddGeometry(geometry);
            GeometryAdded?.Invoke(this, new GeometryEventArgs(geometry));
            OnContentChanged();
        }

        /// <summary>
        /// 移除几何图形
        /// </summary>
        /// <param name="geometry">几何图形</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGeometry(IGeometry geometry)
        {
            bool removed = GeometryLayer.RemoveGeometry(geometry);
            if (removed)
            {
                GeometryRemoved?.Invoke(this, new GeometryEventArgs(geometry));
                OnContentChanged();
            }
            return removed;
        }

        /// <summary>
        /// 清空所有几何图形
        /// </summary>
        public void ClearGeometries()
        {
            bool hadGeometries = GeometryLayer.Geometries.Count > 0;
            GeometryLayer.ClearGeometries();
            if (hadGeometries)
            {
                OnContentChanged();
            }
        }

        /// <summary>
        /// 创建矩形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>创建的矩形几何图形</returns>
        public Geometry.Shapes.RectangleGeometry CreateRectangle(Rect worldRect)
        {
            var rectangle = new Geometry.Shapes.RectangleGeometry(worldRect);
            AddGeometry(rectangle);
            return rectangle;
        }

        /// <summary>
        /// 创建圆形
        /// </summary>
        /// <param name="worldCenter">世界坐标圆心</param>
        /// <param name="radius">半径</param>
        /// <returns>创建的圆形几何图形</returns>
        public CircleGeometry CreateCircle(Point worldCenter, double radius)
        {
            var circle = new CircleGeometry(worldCenter, radius);
            AddGeometry(circle);
            return circle;
        }

        /// <summary>
        /// 创建线段
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <returns>创建的线段几何图形</returns>
        public Geometry.Shapes.LineGeometry CreateLine(Point startPoint, Point endPoint)
        {
            var line = new Geometry.Shapes.LineGeometry(startPoint, endPoint);
            AddGeometry(line);
            return line;
        }

        /// <summary>
        /// 创建箭头线段
        /// </summary>
        /// <param name="startPoint">起点</param>
        /// <param name="endPoint">终点</param>
        /// <returns>创建的箭头线段几何图形</returns>
        public ArrowLineGeometry CreateArrowLine(Point startPoint, Point endPoint)
        {
            var arrowLine = new ArrowLineGeometry(startPoint, endPoint);
            AddGeometry(arrowLine);
            return arrowLine;
        }

        /// <summary>
        /// 创建旋转矩形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <param name="rotationAngle">旋转角度（度）</param>
        /// <returns>创建的旋转矩形几何图形</returns>
        public RotatedRectangleGeometry CreateRotatedRectangle(Rect worldRect, double rotationAngle = 0)
        {
            var rotatedRectangle = new RotatedRectangleGeometry(worldRect, rotationAngle);
            AddGeometry(rotatedRectangle);
            return rotatedRectangle;
        }

        #endregion

        #region 公开方法 - 选择操作

        /// <summary>
        /// 选择几何图形
        /// </summary>
        /// <param name="geometry">要选择的几何图形</param>
        /// <param name="multiSelect">是否多选</param>
        public void SelectGeometry(IGeometry geometry, bool multiSelect = false)
        {
            GeometryLayer.SelectGeometry(geometry, multiSelect);
        }

        /// <summary>
        /// 清空选择
        /// </summary>
        public void ClearSelection()
        {
            GeometryLayer.ClearSelection();
            SelectionLayer.EditingGeometry = null;
            // 清除控制点状态
            ClearControlPointStates();
            // 确保SelectionLayer也清除控制点状态
            SelectionLayer.ClearControlPointStates();
        }

        /// <summary>
        /// 删除选中的几何图形
        /// </summary>
        public void DeleteSelected()
        {
            GeometryLayer.DeleteSelected();
            SelectionLayer.EditingGeometry = null;
            // 清除控制点状态
            ClearControlPointStates();
            // 确保SelectionLayer也清除控制点状态
            SelectionLayer.ClearControlPointStates();
        }

        #endregion

        #region 公开方法 - 视口操作

        /// <summary>
        /// 设置坐标系方向
        /// </summary>
        /// <param name="yAxisUp">Y轴是否向上（true:向右为正X、向上为正Y；false:向右为正X、向下为正Y）</param>
        public void SetCoordinateSystem(bool yAxisUp)
        {
            ViewportTransform.SetCoordinateSystem(yAxisUp);
        }

        /// <summary>
        /// 重置视口到默认状态
        /// </summary>
        public void ResetViewport()
        {
            ViewportTransform.Reset();
        }

        /// <summary>
        /// 适应所有几何图形到视口
        /// </summary>
        public void FitToGeometries()
        {
            if (GeometryLayer.Geometries.Count == 0) return;

            Rect bounds = Rect.Empty;
            foreach (var geometry in GeometryLayer.Geometries)
            {
                if (geometry.IsVisible)
                {
                    bounds.Union(geometry.Bounds);
                }
            }

            if (!bounds.IsEmpty)
            {
                ViewportTransform.FitToBounds(bounds, 50);
            }
        }

        /// <summary>
        /// 自动全局显示 - 计算所有可见内容的最小外接矩形并调整视口以最大化显示
        /// </summary>
        /// <param name="margin">边距（像素），默认为50</param>
        public void AutoFitToAllContent(double margin = 50)
        {
            Rect contentBounds = GetAllContentBounds();
            
            if (contentBounds.IsEmpty)
            {
                // 如果没有内容，重置视口到默认状态
                ViewportTransform.Reset();
                return;
            }

            // 使用视口变换的FitToBounds方法来自动调整
            ViewportTransform.FitToBounds(contentBounds, margin);
        }

        /// <summary>
        /// 获取所有可见内容的最小外接轴对齐矩形
        /// </summary>
        /// <returns>所有可见内容的边界矩形（世界坐标），如果没有内容则返回空矩形</returns>
        public Rect GetAllContentBounds()
        {
            Rect bounds = Rect.Empty;

            // 添加所有可见几何图形的边界
            foreach (var geometry in GeometryLayer.Geometries)
            {
                if (geometry.IsVisible)
                {
                    bounds.Union(geometry.Bounds);
                }
            }

            // 添加所有可见图像的边界
            foreach (var imageItem in ImageLayer.Images)
            {
                if (imageItem.IsVisible)
                {
                    Rect imageBounds = new Rect(imageItem.WorldPosition, imageItem.WorldSize);
                    bounds.Union(imageBounds);
                }
            }

            return bounds;
        }

        #endregion

        #region 受保护方法 - 控件生命周期

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            ViewportTransform.ViewportSize = sizeInfo.NewSize;
            
            // 设置所有图层的尺寸（使用自定义属性）
            _gridLayer.ViewportWidth = sizeInfo.NewSize.Width;
            _gridLayer.ViewportHeight = sizeInfo.NewSize.Height;
            ImageLayer.ViewportWidth = sizeInfo.NewSize.Width;
            ImageLayer.ViewportHeight = sizeInfo.NewSize.Height;
            GeometryLayer.ViewportWidth = sizeInfo.NewSize.Width;
            GeometryLayer.ViewportHeight = sizeInfo.NewSize.Height;
            SelectionLayer.ViewportWidth = sizeInfo.NewSize.Width;
            SelectionLayer.ViewportHeight = sizeInfo.NewSize.Height;
            _drawingPreviewLayer.ViewportWidth = sizeInfo.NewSize.Width;
            _drawingPreviewLayer.ViewportHeight = sizeInfo.NewSize.Height;
            
            UpdateAllLayers();
        }

        #endregion

        #region 私有方法 - 初始化

        /// <summary>
        /// 初始化交互处理
        /// </summary>
        private void InitializeInteraction()
        {
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;
            MouseLeave += OnMouseLeave;
        }

        /// <summary>
        /// 更新光标
        /// </summary>
        private void UpdateCursor()
        {
            Cursor = _currentMode switch
            {
                InteractionMode.Select => Cursors.Arrow,
                InteractionMode.Pan => Cursors.Hand,
                InteractionMode.DrawRectangle => Cursors.Cross,
                InteractionMode.DrawCircle => Cursors.Cross,
                InteractionMode.DrawLine => Cursors.Cross,
                InteractionMode.DrawArrowLine => Cursors.Cross,
                InteractionMode.DrawRotatedRectangle => Cursors.Cross,
                _ => Cursors.Arrow
            };
        }

        /// <summary>
        /// 更新所有图层
        /// </summary>
        private void UpdateAllLayers()
        {
            _gridLayer?.Update();
            ImageLayer?.Update();
            GeometryLayer?.Update();
            SelectionLayer?.Update();
            _drawingPreviewLayer?.Update();
        }

        #endregion

        #region 私有方法 - 事件处理

        private static void OnBackgroundGridVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XDisplayControl control && control._gridLayer != null)
            {
                control._gridLayer.IsVisible = (bool)e.NewValue;
            }
        }

        private void OnViewportTransformChanged(object? sender, EventArgs e)
        {
            UpdateAllLayers();
            ViewportTransformChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnGeometrySelectionChanged(object? sender, GeometrySelectionChangedEventArgs e)
        {
            // 更新选择图层的编辑几何图形
            SelectionLayer.EditingGeometry = e.SelectedGeometries.Count == 1 ? e.SelectedGeometries[0] : null;
            
            SelectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 触发选择改变事件
        /// </summary>
        private void OnSelectionChanged()
        {
            var selectedGeometries = GeometryLayer.SelectedGeometries;
            SelectionChanged?.Invoke(this, new GeometrySelectionChangedEventArgs(selectedGeometries));
        }

        /// <summary>
        /// 触发内容改变事件
        /// </summary>
        private void OnContentChanged()
        {
            ContentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 私有方法 - 鼠标事件处理

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _isMouseDown = true;
            _lastMousePosition = e.GetPosition(this);
            Point worldPoint = ViewportTransform.ScreenToWorld(_lastMousePosition);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                HandleLeftMouseDown(worldPoint, e);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                HandleRightMouseDown(worldPoint, e);
            }

            CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(this);
            Point worldPoint = ViewportTransform.ScreenToWorld(currentPosition);

            // 触发鼠标坐标改变事件
            MouseCoordinateChanged?.Invoke(this, new MouseCoordinateChangedEventArgs(currentPosition, worldPoint));

            if (_isMouseDown)
            {
                HandleMouseDrag(currentPosition, worldPoint, e);
            }
            else
            {
                HandleMouseHover(worldPoint, e);
            }

            _lastMousePosition = currentPosition;
            e.Handled = true;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isMouseDown)
            {
                Point currentPosition = e.GetPosition(this);
                Point worldPoint = ViewportTransform.ScreenToWorld(currentPosition);

                HandleMouseUp(worldPoint, e);
            }

            _isMouseDown = false;
            _isDragging = false;
            _draggedGeometry = null;
            _draggedControlPointIndex = -1;
            SelectionLayer.EndSelectionRect();
            
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mousePosition = e.GetPosition(this);
            double scaleFactor = e.Delta > 0 ? 1.1 : 0.9;
            
            ViewportTransform.ScaleAt(mousePosition, scaleFactor);
            e.Handled = true;
        }
        
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            // 鼠标离开控件时，触发坐标清空事件
            MouseCoordinateChanged?.Invoke(this, new MouseCoordinateChangedEventArgs(new Point(-1, -1), new Point(double.NaN, double.NaN)));
        }

        #endregion

        #region 私有方法 - 具体交互处理

        private void HandleLeftMouseDown(Point worldPoint, MouseButtonEventArgs e)
        {
            switch (_currentMode)
            {
                case InteractionMode.Select:
                    HandleSelectModeLeftDown(worldPoint, e);
                    break;
                case InteractionMode.Pan:
                    // 平移模式在拖拽时处理
                    break;
                case InteractionMode.DrawRectangle:
                    StartDrawingRectangle(worldPoint);
                    break;
                case InteractionMode.DrawCircle:
                    StartDrawingCircle(worldPoint);
                    break;
                case InteractionMode.DrawLine:
                    StartDrawingLine(worldPoint);
                    break;
                case InteractionMode.DrawArrowLine:
                    StartDrawingArrowLine(worldPoint);
                    break;
                case InteractionMode.DrawRotatedRectangle:
                    StartDrawingRotatedRectangle(worldPoint);
                    break;
            }
        }

        private void HandleRightMouseDown(Point worldPoint, MouseButtonEventArgs e)
        {
            // 右键通常用于上下文菜单或取消当前操作
            if (_currentMode != InteractionMode.Select)
            {
                // 取消当前绘制操作
                if (_isDrawing)
                {
                    _isDrawing = false;
                    _drawingStartPoint = null;
                    _drawingPreviewLayer.ClearPreview();
                }
                
                _currentMode = InteractionMode.Select;
                UpdateCursor();
            }
        }

        private void HandleMouseDrag(Point screenPoint, Point worldPoint, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                // 开始拖拽
                double dragThreshold = 1.0; // 像素 - 进一步降低阈值使拖拽更容易触发
                double distance = Math.Sqrt(
                    Math.Pow(screenPoint.X - _lastMousePosition.X, 2) +
                    Math.Pow(screenPoint.Y - _lastMousePosition.Y, 2));
                
                if (distance >= dragThreshold)
                {
                    _isDragging = true;
                    StartDragOperation(worldPoint);
                }
            }
            else
            {
                // 继续拖拽
                ContinueDragOperation(screenPoint, worldPoint);
            }
        }

        private void HandleMouseHover(Point worldPoint, MouseEventArgs e)
        {
            // 悬停处理 - 更新光标和控制点悬停状态
            if (_currentMode == InteractionMode.Select)
            {
                var hitGeometry = GeometryLayer.HitTest(worldPoint);
                Cursor = hitGeometry != null ? Cursors.Hand : Cursors.Arrow;
                
                // 检查是否悬停在控制点上（仅在没有选中控制点时）
                int hoveredControlPointIndex = -1;
                if (_selectedControlPointIndex == -1 && SelectionLayer.EditingGeometry != null)
                {
                    hoveredControlPointIndex = SelectionLayer.HitTestControlPoint(worldPoint);
                }
                
                // 如果悬停的控制点发生变化，更新选择图层
                if (_hoveredControlPointIndex != hoveredControlPointIndex)
                {
                    _hoveredControlPointIndex = hoveredControlPointIndex;
                    SelectionLayer.SetHoveredControlPoint(_hoveredControlPointIndex);
                    
                    // 更新光标
                    if (_hoveredControlPointIndex >= 0)
                    {
                        Cursor = Cursors.Hand;
                    }
                }
            }
        }

        private void HandleMouseUp(Point worldPoint, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                EndDragOperation(worldPoint);
            }
            else
            {
                // 单击处理
                HandleSingleClick(worldPoint, e);
            }
        }

        #endregion

        #region 私有方法 - 选择模式处理

        private void HandleSelectModeLeftDown(Point worldPoint, MouseButtonEventArgs e)
        {
            bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            
            // 首先检查是否点击了控制点
            int controlPointIndex = SelectionLayer.HitTestControlPoint(worldPoint);
            if (controlPointIndex >= 0 && SelectionLayer.EditingGeometry != null)
            {
                // 确保任何时候只有一个控制点处于选中状态
                if (_selectedControlPointIndex != controlPointIndex)
                {
                    // 在设置新的选中状态之前，先清除所有可能的选中和拖拽状态
                    ClearControlPointStates();
                    
                    // 设置新的选中的控制点
                    _selectedControlPointIndex = controlPointIndex;
                    SelectionLayer.SetSelectedControlPoint(_selectedControlPointIndex);
                }
                
                // 注意：不立即设置为拖拽状态，等待鼠标移动超过阈值后再设置
                return;
            }

            // 检查是否为循环选择（在相同位置快速连续点击）
            Point screenPoint = e.GetPosition(this);
            bool isCyclicClick = IsCyclicClick(screenPoint);
            
            // 然后检查是否点击了几何图形
            IGeometry? hitGeometry;
            if (isCyclicClick && SelectedGeometries.Count == 1)
            {
                // 循环选择模式：在重叠的图形之间循环
                hitGeometry = GeometryLayer.CyclicHitTest(worldPoint, SelectedGeometries[0]);
            }
            else
            {
                // 正常选择模式：选择最高层的图形
                hitGeometry = GeometryLayer.HitTest(worldPoint);
            }
            
            if (hitGeometry != null)
            {
                if (hitGeometry.IsSelected && isCtrlPressed)
                {
                    // Ctrl+点击已选中的对象 = 取消选择
                    GeometryLayer.DeselectGeometry(hitGeometry);
                }
                else if (!hitGeometry.IsSelected)
                {
                    // 选择新对象
                    GeometryLayer.SelectGeometry(hitGeometry, isCtrlPressed);
                }
                // 注意：移除了设置 _draggedGeometry 的逻辑，不再允许直接拖拽几何图形
            }
            else
            {
                // 点击空白区域
                if (!isCtrlPressed)
                {
                    GeometryLayer.ClearSelection();
                    // 清除控制点状态
                    ClearControlPointStates();
                }
                // 开始框选
                SelectionLayer.StartSelectionRect(worldPoint);
            }
            
            // 更新循环选择状态
            UpdateCyclicClickState(screenPoint);
        }

        private void HandleSingleClick(Point worldPoint, MouseButtonEventArgs e)
        {
            // 处理单击事件（非拖拽）
            // 如果有选中的控制点，则清除选中状态
            if (_selectedControlPointIndex >= 0)
            {
                _selectedControlPointIndex = -1;
                SelectionLayer.SetSelectedControlPoint(-1);
            }
        }

        /// <summary>
        /// 清除所有控制点相关状态
        /// </summary>
        private void ClearControlPointStates()
        {
            // 清除选中状态
            if (_selectedControlPointIndex >= 0)
            {
                _selectedControlPointIndex = -1;
                SelectionLayer.SetSelectedControlPoint(-1);
            }
            
            // 清除拖拽状态
            if (_draggedControlPointIndex >= 0)
            {
                _draggedControlPointIndex = -1;
                _draggedGeometry = null;
                SelectionLayer.SetDraggedControlPoint(-1);
            }
            
            // 清除悬停状态
            if (_hoveredControlPointIndex >= 0)
            {
                _hoveredControlPointIndex = -1;
                SelectionLayer.SetHoveredControlPoint(-1);
            }
        }
        
        #endregion

        #region 私有方法 - 循环选择支持

        /// <summary>
        /// 检查是否为循环点击（在相同位置的快速连续点击）
        /// </summary>
        /// <param name="currentClickPosition">当前点击位置（屏幕坐标）</param>
        /// <returns>是否为循环点击</returns>
        private bool IsCyclicClick(Point currentClickPosition)
        {
            if (!_lastClickPosition.HasValue)
                return false;

            // 检查时间间隔
            var timeDiff = (DateTime.Now - _lastClickTime).TotalMilliseconds;
            if (timeDiff > CyclicClickTimeWindow)
                return false;

            // 检查位置距离
            var distance = Math.Sqrt(
                Math.Pow(currentClickPosition.X - _lastClickPosition.Value.X, 2) +
                Math.Pow(currentClickPosition.Y - _lastClickPosition.Value.Y, 2));
            
            return distance <= CyclicClickTolerance;
        }

        /// <summary>
        /// 更新循环点击状态
        /// </summary>
        /// <param name="currentClickPosition">当前点击位置（屏幕坐标）</param>
        private void UpdateCyclicClickState(Point currentClickPosition)
        {
            _lastClickPosition = currentClickPosition;
            _lastClickTime = DateTime.Now;
        }

        #endregion

        #region 私有方法 - 绘制模式处理

        private void StartDrawingRectangle(Point worldPoint)
        {
            _drawingStartPoint = worldPoint;
            _isDrawing = true;
        }

        private void StartDrawingCircle(Point worldPoint)
        {
            _drawingStartPoint = worldPoint;
            _isDrawing = true;
        }

        private void StartDrawingLine(Point worldPoint)
        {
            _drawingStartPoint = worldPoint;
            _isDrawing = true;
        }

        private void StartDrawingArrowLine(Point worldPoint)
        {
            _drawingStartPoint = worldPoint;
            _isDrawing = true;
        }

        private void StartDrawingRotatedRectangle(Point worldPoint)
        {
            _drawingStartPoint = worldPoint;
            _isDrawing = true;
        }

        #endregion

        #region 私有方法 - 拖拽操作

        private void StartDragOperation(Point worldPoint)
        {
            if (_currentMode == InteractionMode.Select)
            {
                if (_selectedControlPointIndex >= 0 && SelectionLayer.EditingGeometry != null)
                {
                    // 开始拖拽控制点
                    _draggedControlPointIndex = _selectedControlPointIndex;
                    _draggedGeometry = SelectionLayer.EditingGeometry;
                    SelectionLayer.SetDraggedControlPoint(_draggedControlPointIndex);
                }
                else if (SelectionLayer.SelectionRect.HasValue)
                {
                    // 框选拖拽
                    SelectionLayer.UpdateSelectionRect(worldPoint);
                }
            }
            else if (_currentMode == InteractionMode.Pan)
            {
                // 平移拖拽 - 无需特殊初始化
            }
        }

        private void ContinueDragOperation(Point screenPoint, Point worldPoint)
        {
            if (_currentMode == InteractionMode.Select)
            {
                if (_draggedControlPointIndex >= 0 && _draggedGeometry != null)
                {
                    // 拖拽控制点
                    var controlPointType = _draggedGeometry.GetControlPointType(_draggedControlPointIndex);
                    if (controlPointType == ControlPointType.Move)
                    {
                        // 移动控制点：移动整个几何图形
                        Point lastWorldPoint = ViewportTransform.ScreenToWorld(_lastMousePosition);
                        double deltaX = worldPoint.X - lastWorldPoint.X;
                        double deltaY = worldPoint.Y - lastWorldPoint.Y;
                        
                        if (_draggedGeometry.IsSelected)
                        {
                            // 移动所有选中的对象
                            GeometryLayer.MoveSelected(deltaX, deltaY);
                        }
                        else
                        {
                            // 只移动当前对象
                            _draggedGeometry.Move(deltaX, deltaY);
                        }
                    }
                    else
                    {
                        // 其他控制点（调整大小、旋转等）
                        _draggedGeometry.UpdateControlPoint(_draggedControlPointIndex, worldPoint);
                    }
                }
                else if (SelectionLayer.SelectionRect.HasValue)
                {
                    // 更新框选矩形
                    SelectionLayer.UpdateSelectionRect(worldPoint);
                }
            }
            else if (_currentMode == InteractionMode.Pan)
            {
                // 平移视口
                double deltaX = screenPoint.X - _lastMousePosition.X;
                double deltaY = screenPoint.Y - _lastMousePosition.Y;
                ViewportTransform.Translate(deltaX, deltaY);
            }
            else if (_isDrawing && _drawingStartPoint.HasValue)
            {
                // 更新正在绘制的图形预览
                UpdateDrawingPreview(worldPoint);
            }
        }

        private void EndDragOperation(Point worldPoint)
        {
            if (_currentMode == InteractionMode.Select)
            {
                if (SelectionLayer.SelectionRect.HasValue)
                {
                    // 完成框选
                    bool isCtrlPressed = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                    GeometryLayer.SelectGeometriesInRect(SelectionLayer.SelectionRect.Value, isCtrlPressed);
                    SelectionLayer.ClearSelectionRect();
                }
                else if (_draggedControlPointIndex >= 0)
                {
                    // 完成控制点拖拽
                    _draggedControlPointIndex = -1;
                    _draggedGeometry = null;
                    SelectionLayer.SetDraggedControlPoint(-1);
                }
                
                // 清除选中状态（重要：确保句柄不会以选中状态出现）
                _selectedControlPointIndex = -1;
                SelectionLayer.SetSelectedControlPoint(-1);
            }
            else if (_isDrawing && _drawingStartPoint.HasValue)
            {
                // 完成绘制
                FinishDrawing(worldPoint);
            }
        }

        private void UpdateDrawingPreview(Point currentPoint)
        {
            if (!_drawingStartPoint.HasValue) return;

            Point startPoint = _drawingStartPoint.Value;

            switch (_currentMode)
            {
                case InteractionMode.DrawRectangle:
                    _drawingPreviewLayer.SetRectanglePreview(startPoint, currentPoint);
                    break;
                case InteractionMode.DrawCircle:
                    double radius = Math.Sqrt(
                        Math.Pow(currentPoint.X - startPoint.X, 2) +
                        Math.Pow(currentPoint.Y - startPoint.Y, 2));
                    _drawingPreviewLayer.SetCirclePreview(startPoint, radius);
                    break;
                case InteractionMode.DrawLine:
                    _drawingPreviewLayer.SetLinePreview(startPoint, currentPoint);
                    break;
                case InteractionMode.DrawArrowLine:
                    _drawingPreviewLayer.SetArrowLinePreview(startPoint, currentPoint);
                    break;
                case InteractionMode.DrawRotatedRectangle:
                    _drawingPreviewLayer.SetRotatedRectanglePreview(startPoint, currentPoint, 0); // 初始角度设为0度
                    break;
            }
        }

        private void FinishDrawing(Point endPoint)
        {
            if (!_drawingStartPoint.HasValue) return;

            Point startPoint = _drawingStartPoint.Value;

            // 清除预览
            _drawingPreviewLayer.ClearPreview();

            switch (_currentMode)
            {
                case InteractionMode.DrawRectangle:
                    CreateRectangle(new Rect(startPoint, endPoint));
                    break;
                case InteractionMode.DrawCircle:
                    double radius = Math.Sqrt(
                        Math.Pow(endPoint.X - startPoint.X, 2) +
                        Math.Pow(endPoint.Y - startPoint.Y, 2));
                    CreateCircle(startPoint, radius);
                    break;
                case InteractionMode.DrawLine:
                    CreateLine(startPoint, endPoint);
                    break;
                case InteractionMode.DrawArrowLine:
                    CreateArrowLine(startPoint, endPoint);
                    break;
                case InteractionMode.DrawRotatedRectangle:
                    // 创建初始角度为0度的旋转矩形
                    CreateRotatedRectangle(new Rect(startPoint, endPoint), 0);
                    break;
            }

            _isDrawing = false;
            _drawingStartPoint = null;
        }

        #endregion

        #region 私有方法 - 键盘事件处理

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteSelected();
                    e.Handled = true;
                    break;
                case Key.A when Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl):
                    // Ctrl+A 全选
                    foreach (var geometry in GeometryLayer.Geometries)
                    {
                        geometry.IsSelected = true;
                    }
                    GeometryLayer.Update();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    // ESC键取消操作
                    if (_isDrawing)
                    {
                        _isDrawing = false;
                        _drawingStartPoint = null;
                        _drawingPreviewLayer.ClearPreview();
                    }
                    ClearSelection();
                    _currentMode = InteractionMode.Select;
                    UpdateCursor();
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region 字段

        private readonly DisplayCanvas _displayCanvas;
        private readonly GridLayer _gridLayer;
        private readonly DrawingPreviewLayer _drawingPreviewLayer; // 新增绘制预览图层
        private InteractionMode _currentMode;

        // 交互状态
        private bool _isMouseDown;
        private Point _lastMousePosition;
        private bool _isDragging;
        private IGeometry? _draggedGeometry;
        private int _draggedControlPointIndex = -1;
        private int _hoveredControlPointIndex = -1; // 悬停的控制点索引
        private int _selectedControlPointIndex = -1; // 选中的控制点索引

        // 循环选择状态
        private Point? _lastClickPosition;
        private DateTime _lastClickTime;
        private const double CyclicClickTolerance = 5.0; // 像素容差
        private const double CyclicClickTimeWindow = 500.0; // 毫秒

        // 绘制状态
        private bool _isDrawing;
        private Point? _drawingStartPoint;

        #endregion
    }

    /// <summary>
    /// 交互模式枚举
    /// </summary>
    public enum InteractionMode
    {
        /// <summary>选择模式</summary>
        Select,
        /// <summary>平移模式</summary>
        Pan,
        /// <summary>绘制矩形模式</summary>
        DrawRectangle,
        /// <summary>绘制圆形模式</summary>
        DrawCircle,
        /// <summary>绘制线段模式</summary>
        DrawLine,
        /// <summary>绘制箭头线段模式</summary>
        DrawArrowLine,
        /// <summary>绘制旋转矩形模式</summary>
        DrawRotatedRectangle
    }

    /// <summary>
    /// 几何图形事件参数
    /// </summary>
    public class GeometryEventArgs : EventArgs
    {
        public IGeometry Geometry { get; }

        public GeometryEventArgs(IGeometry geometry)
        {
            Geometry = geometry;
        }
    }
    
    /// <summary>
    /// 鼠标坐标改变事件参数
    /// </summary>
    public class MouseCoordinateChangedEventArgs : EventArgs
    {
        /// <summary>屏幕坐标</summary>
        public Point ScreenCoordinate { get; }
        
        /// <summary>世界坐标</summary>
        public Point WorldCoordinate { get; }

        public MouseCoordinateChangedEventArgs(Point screenCoordinate, Point worldCoordinate)
        {
            ScreenCoordinate = screenCoordinate;
            WorldCoordinate = worldCoordinate;
        }
    }
}