using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XDisplay.Geometry;
using XDisplay.Geometry.Shapes;

namespace XDisplay.Layers
{
    /// <summary>
    /// 选择图层 - 负责绘制选择矩形和编辑手柄
    /// </summary>
    public class SelectionLayer : LayerBase
    {
        #region 属性

        /// <summary>选择矩形（世界坐标）</summary>
        public Rect? SelectionRect
        {
            get => _selectionRect;
            set
            {
                if (_selectionRect != value)
                {
                    _selectionRect = value;
                    Update();
                }
            }
        }

        /// <summary>当前编辑的几何图形</summary>
        public IGeometry? EditingGeometry
        {
            get => _editingGeometry;
            set
            {
                if (_editingGeometry != value)
                {
                    // 取消对旧几何图形的事件监听
                    if (_editingGeometry != null)
                    {
                        _editingGeometry.GeometryChanged -= OnEditingGeometryChanged;
                    }
                    
                    _editingGeometry = value;
                    
                    // 监听新几何图形的变化事件
                    if (_editingGeometry != null)
                    {
                        _editingGeometry.GeometryChanged += OnEditingGeometryChanged;
                    }
                    
                    Update();
                }
            }
        }

        /// <summary>控制点大小（屏幕坐标像素）</summary>
        public double ControlPointSize { get; set; } = 8.0;

        /// <summary>选择框颜色</summary>
        public Brush SelectionBrush { get; set; } = Brushes.Blue;

        /// <summary>控制点颜色</summary>
        public Brush ControlPointBrush { get; set; } = Brushes.White;

        /// <summary>控制点边框颜色</summary>
        public Brush ControlPointBorderBrush { get; set; } = Brushes.Blue;

        /// <summary>移动句柄颜色</summary>
        public Brush MoveHandleBrush { get; set; } = Brushes.LightGreen;

        /// <summary>旋转句柄颜色</summary>
        public Brush RotationHandleBrush { get; set; } = Brushes.Orange;

        /// <summary>悬停时的控制点放大倍数</summary>
        public double HoverScaleFactor { get; set; } = 1.5;

        /// <summary>悬停时的控制点颜色</summary>
        public Brush HoverControlPointBrush { get; set; } = Brushes.Yellow;

        /// <summary>悬停时的控制点边框颜色</summary>
        public Brush HoverControlPointBorderBrush { get; set; } = Brushes.Red;

        /// <summary>选中时的控制点颜色</summary>
        public Brush SelectedControlPointBrush { get; set; } = Brushes.Red;

        /// <summary>选中时的控制点边框颜色</summary>
        public Brush SelectedControlPointBorderBrush { get; set; } = Brushes.DarkRed;

        /// <summary>选中时的控制点放大倍数</summary>
        public double SelectedScaleFactor { get; set; } = 1.3;

        #endregion

        #region 构造函数

        public SelectionLayer()
        {
            InitializePens();
        }

        /// <summary>
        /// 析构函数 - 确保事件监听器被正确清理
        /// </summary>
        ~SelectionLayer()
        {
            if (_editingGeometry != null)
            {
                _editingGeometry.GeometryChanged -= OnEditingGeometryChanged;
            }
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 开始选择矩形操作
        /// </summary>
        /// <param name="startPoint">起始点（世界坐标）</param>
        public void StartSelectionRect(Point startPoint)
        {
            _selectionStartPoint = startPoint;
            SelectionRect = new Rect(startPoint, new Size(0, 0));
        }

        /// <summary>
        /// 更新选择矩形
        /// </summary>
        /// <param name="currentPoint">当前点（世界坐标）</param>
        public void UpdateSelectionRect(Point currentPoint)
        {
            if (_selectionStartPoint.HasValue)
            {
                Point start = _selectionStartPoint.Value;
                SelectionRect = new Rect(
                    Math.Min(start.X, currentPoint.X),
                    Math.Min(start.Y, currentPoint.Y),
                    Math.Abs(currentPoint.X - start.X),
                    Math.Abs(currentPoint.Y - start.Y));
            }
        }

        /// <summary>
        /// 结束选择矩形操作
        /// </summary>
        public void EndSelectionRect()
        {
            _selectionStartPoint = null;
        }

        /// <summary>
        /// 清除选择矩形
        /// </summary>
        public void ClearSelectionRect()
        {
            SelectionRect = null;
            _selectionStartPoint = null;
        }
        
        /// <summary>
        /// 清除所有控制点状态
        /// </summary>
        public void ClearControlPointStates()
        {
            _hoveredControlPointIndex = -1;
            _selectedControlPointIndex = -1;
            _draggedControlPointIndex = -1;
            Update();
        }

        /// <summary>
        /// 命中测试控制点
        /// </summary>
        /// <param name="worldPoint">测试点（世界坐标）</param>
        /// <returns>命中的控制点索引，未命中返回-1</returns>
        public int HitTestControlPoint(Point worldPoint)
        {
            if (_editingGeometry == null) return -1;

            var controlPoints = _editingGeometry.GetControlPoints();
            if (controlPoints == null || controlPoints.Length == 0) return -1;

            double tolerance = ControlPointSize / 2;
            if (Transform != null)
            {
                tolerance /= Transform.Scale; // 转换为世界坐标单位
            }

            for (int i = 0; i < controlPoints.Length; i++)
            {
                Point controlPoint = controlPoints[i];
                double distance = Math.Sqrt(
                    Math.Pow(worldPoint.X - controlPoint.X, 2) + 
                    Math.Pow(worldPoint.Y - controlPoint.Y, 2));

                // 如果是移动控制点，使用1.5倍的命中区域
                var controlPointType = _editingGeometry.GetControlPointType(i);
                double currentTolerance = controlPointType == ControlPointType.Move ? tolerance * 1.5 : tolerance;

                if (distance <= currentTolerance)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 设置悬停的控制点索引
        /// </summary>
        /// <param name="hoveredIndex">悬停的控制点索引，-1表示没有悬停</param>
        public void SetHoveredControlPoint(int hoveredIndex)
        {
            if (_hoveredControlPointIndex != hoveredIndex)
            {
                _hoveredControlPointIndex = hoveredIndex;
                Update();
            }
        }

        /// <summary>
        /// 设置当前选中的控制点索引
        /// </summary>
        /// <param name="selectedIndex">选中的控制点索引，-1表示没有选中</param>
        public void SetSelectedControlPoint(int selectedIndex)
        {
            if (_selectedControlPointIndex != selectedIndex)
            {
                _selectedControlPointIndex = selectedIndex;
                Update();
            }
        }

        /// <summary>
        /// 设置当前拖拽的控制点索引
        /// </summary>
        /// <param name="draggedIndex">拖拽的控制点索引，-1表示没有拖拽</param>
        public void SetDraggedControlPoint(int draggedIndex)
        {
            if (_draggedControlPointIndex != draggedIndex)
            {
                _draggedControlPointIndex = draggedIndex;
                Update();
            }
        }

        #endregion

        #region 保护方法

        protected override void OnRender(DrawingContext context)
        {
            // 绘制选择矩形
            if (_selectionRect.HasValue && !_selectionRect.Value.IsEmpty)
            {
                DrawSelectionRect(context, _selectionRect.Value);
            }

            // 绘制编辑几何图形的边界框和控制点
            if (_editingGeometry != null)
            {
                if (_editingGeometry is RotatedRectangleGeometry rotatedRect)
                {
                    DrawRotatedRectangleBoundingBox(context, rotatedRect);
                    DrawRotatedRectangleControlPoints(context, rotatedRect);
                }
                else
                {
                    DrawGeometryBoundingBox(context, _editingGeometry);
                    DrawControlPoints(context, _editingGeometry);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 编辑几何图形变化时的处理
        /// </summary>
        private void OnEditingGeometryChanged(object? sender, GeometryChangedEventArgs e)
        {
            // 当几何图形的位置或尺寸变化时，重新绘制操作句柄
            if (e.ChangeType == GeometryChangeType.Position || 
                e.ChangeType == GeometryChangeType.Size || 
                e.ChangeType == GeometryChangeType.Rotation)
            {
                Update();
            }
            // 当几何图形的选择状态改变时，清除控制点状态
            else if (e.ChangeType == GeometryChangeType.Selection && _editingGeometry != null && !_editingGeometry.IsSelected)
            {
                ClearControlPointStates();
            }
        }

        /// <summary>
        /// 初始化画笔
        /// </summary>
        private void InitializePens()
        {
            _selectionPen = new Pen(SelectionBrush, 1.0)
            {
                DashStyle = DashStyles.Dash
            };
            _selectionPen.Freeze();

            _controlPointPen = new Pen(ControlPointBorderBrush, 1.0);
            _controlPointPen.Freeze();
            
            _rotationHandlePen = new Pen(RotationHandleBrush, 2.0);
            _rotationHandlePen.Freeze();
            
            // 初始化悬停状态的画笔
            _hoverControlPointPen = new Pen(HoverControlPointBorderBrush, 1.0);
            _hoverControlPointPen.Freeze();
            
            // 初始化选中状态的画笔
            _selectedControlPointPen = new Pen(SelectedControlPointBorderBrush, 1.0);
            _selectedControlPointPen.Freeze();
        }

        /// <summary>
        /// 绘制几何图形的边界框
        /// </summary>
        private void DrawGeometryBoundingBox(DrawingContext context, IGeometry geometry)
        {
            if (geometry is RotatedRectangleGeometry rotatedRect)
            {
                DrawRotatedRectangleBoundingBox(context, rotatedRect);
            }
            else
            {
                var bounds = geometry.Bounds;
                if (bounds.IsEmpty) return;

                Rect screenRect = WorldToScreen(bounds);
                
                // 当Y轴翻转时，需要保证矩形的宽高为正值
                if (Transform != null && Transform.YAxisUp)
                {
                    double left = Math.Min(screenRect.Left, screenRect.Right);
                    double top = Math.Min(screenRect.Top, screenRect.Bottom);
                    double width = Math.Abs(screenRect.Width);
                    double height = Math.Abs(screenRect.Height);
                    screenRect = new Rect(left, top, width, height);
                }
                
                // 使用虚线蓝色边框绘制边界矩形
                context.DrawRectangle(null, _selectionPen, screenRect);
            }
        }

        /// <summary>
        /// 绘制选择矩形
        /// </summary>
        private void DrawSelectionRect(DrawingContext context, Rect worldRect)
        {
            Rect screenRect = WorldToScreen(worldRect);
            
            // 当Y轴翻转时，需要保证矩形的宽高为正值
            if (Transform != null && Transform.YAxisUp)
            {
                double left = Math.Min(screenRect.Left, screenRect.Right);
                double top = Math.Min(screenRect.Top, screenRect.Bottom);
                double width = Math.Abs(screenRect.Width);
                double height = Math.Abs(screenRect.Height);
                screenRect = new Rect(left, top, width, height);
            }
            
            // 绘制选择框
            Brush fillBrush = new SolidColorBrush(((SolidColorBrush)SelectionBrush).Color);
            fillBrush.Opacity = 0.1;
            fillBrush.Freeze();

            context.DrawRectangle(fillBrush, _selectionPen, screenRect);
        }

        /// <summary>
        /// 绘制控制点
        /// </summary>
        private void DrawControlPoints(DrawingContext context, IGeometry geometry)
        {
            if (geometry is RotatedRectangleGeometry rotatedRect)
            {
                DrawRotatedRectangleControlPoints(context, rotatedRect);
            }
            else
            {
                var controlPoints = geometry.GetControlPoints();
                if (controlPoints == null || controlPoints.Length == 0) return;

                double halfSize = ControlPointSize / 2;

                for (int i = 0; i < controlPoints.Length; i++)
                {
                    Point worldPoint = controlPoints[i];
                    Point screenPoint = WorldToScreen(worldPoint);
                    
                    var controlPointType = geometry.GetControlPointType(i);
                    
                    // 确定当前控制点的绘制参数
                    Brush fillBrush = ControlPointBrush;
                    Pen borderPen = _controlPointPen;
                    double sizeFactor = 1.0;
                    
                    // 检查是否是当前拖拽的控制点
                    if (i == _draggedControlPointIndex)
                    {
                        fillBrush = SelectedControlPointBrush;
                        borderPen = _selectedControlPointPen;
                        sizeFactor = SelectedScaleFactor;
                    }
                    // 检查是否是当前选中的控制点
                    else if (i == _selectedControlPointIndex)
                    {
                        fillBrush = SelectedControlPointBrush;
                        borderPen = _selectedControlPointPen;
                        sizeFactor = SelectedScaleFactor;
                    }
                    // 检查是否是悬停的控制点
                    else if (i == _hoveredControlPointIndex)
                    {
                        fillBrush = HoverControlPointBrush;
                        borderPen = _hoverControlPointPen;
                        sizeFactor = HoverScaleFactor;
                    }
                    // 检查是否是移动控制点
                    else if (controlPointType == ControlPointType.Move)
                    {
                        fillBrush = MoveHandleBrush;
                    }
                    
                    if (controlPointType == ControlPointType.Move)
                    {
                        // 移动句柄绘制为圆形（1.5倍大小）
                        double moveHandleSize = halfSize * 1.5 * sizeFactor;
                        context.DrawEllipse(fillBrush, borderPen, screenPoint, moveHandleSize, moveHandleSize);
                    }
                    else
                    {
                        // 调整大小句柄绘制为正方形
                        double actualSize = ControlPointSize * sizeFactor;
                        double actualHalfSize = actualSize / 2;
                        Rect controlPointRect = new Rect(
                            screenPoint.X - actualHalfSize,
                            screenPoint.Y - actualHalfSize,
                            actualSize,
                            actualSize);

                        context.DrawRectangle(fillBrush, borderPen, controlPointRect);
                    }
                }
            }
        }

        /// <summary>
        /// 绘制旋转矩形的边界框
        /// </summary>
        private void DrawRotatedRectangleBoundingBox(DrawingContext context, RotatedRectangleGeometry rotatedRect)
        {
            // 获取旋转后的四个角点
            Point[] corners = GetRotatedCorners(rotatedRect);
            if (corners.Length < 4) return;

            // 转换为屏幕坐标
            Point[] screenCorners = new Point[4];
            for (int i = 0; i < 4; i++)
            {
                screenCorners[i] = WorldToScreen(corners[i]);
            }

            // 绘制旋转后的矩形边框（稍微放大一点）
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(screenCorners[0], false, true);
                for (int i = 1; i < 4; i++)
                {
                    ctx.LineTo(screenCorners[i], true, false);
                }
            }
            geometry.Freeze();

            context.DrawGeometry(null, _selectionPen, geometry);
        }

        /// <summary>
        /// 绘制旋转矩形的控制点
        /// </summary>
        private void DrawRotatedRectangleControlPoints(DrawingContext context, RotatedRectangleGeometry rotatedRect)
        {
            var controlPoints = rotatedRect.GetControlPoints();
            if (controlPoints == null || controlPoints.Length == 0) return;

            double halfSize = ControlPointSize / 2;
            
            // 绘制所有控制点
            for (int i = 0; i < controlPoints.Length; i++)
            {
                Point screenPoint = WorldToScreen(controlPoints[i]);
                var controlPointType = rotatedRect.GetControlPointType(i);
                
                // 确定当前控制点的绘制参数
                Brush fillBrush = ControlPointBrush;
                Pen borderPen = _controlPointPen;
                double sizeFactor = 1.0;
                
                // 检查是否是当前拖拽的控制点
                if (i == _draggedControlPointIndex)
                {
                    fillBrush = SelectedControlPointBrush;
                    borderPen = _selectedControlPointPen;
                    sizeFactor = SelectedScaleFactor;
                }
                // 检查是否是当前选中的控制点
                else if (i == _selectedControlPointIndex)
                {
                    fillBrush = SelectedControlPointBrush;
                    borderPen = _selectedControlPointPen;
                    sizeFactor = SelectedScaleFactor;
                }
                // 检查是否是悬停的控制点
                else if (i == _hoveredControlPointIndex)
                {
                    fillBrush = HoverControlPointBrush;
                    borderPen = _hoverControlPointPen;
                    sizeFactor = HoverScaleFactor;
                }
                // 检查是否是移动控制点
                else if (controlPointType == ControlPointType.Move)
                {
                    fillBrush = MoveHandleBrush;
                }
                // 检查是否是旋转控制点
                else if (controlPointType == ControlPointType.Rotate)
                {
                    fillBrush = RotationHandleBrush;
                }
                
                if (controlPointType == ControlPointType.Move)
                {
                    // 中心移动句柄（圆形，1.5倍大小）
                    double moveHandleSize = halfSize * 1.5 * sizeFactor;
                    context.DrawEllipse(fillBrush, borderPen, screenPoint, moveHandleSize, moveHandleSize);
                }
                else if (controlPointType == ControlPointType.Resize)
                {
                    // 调整大小句柄（正方形）
                    double actualSize = ControlPointSize * sizeFactor;
                    double actualHalfSize = actualSize / 2;
                    Rect controlPointRect = new Rect(
                        screenPoint.X - actualHalfSize,
                        screenPoint.Y - actualHalfSize,
                        actualSize,
                        actualSize);

                    context.DrawRectangle(fillBrush, borderPen, controlPointRect);
                }
                else if (controlPointType == ControlPointType.Rotate)
                {
                    // 旋转句柄（略大的圆形）
                    double rotationHandleSize = ControlPointSize * 0.75 * sizeFactor; // 1.5倍大小的半径
                    context.DrawEllipse(fillBrush, borderPen, screenPoint, rotationHandleSize, rotationHandleSize);
                }
            }

            // 绘制旋转句柄的方向箭头（如果有旋转控制点）
            if (controlPoints.Length > 4) // 有旋转控制点
            {
                DrawRotationHandle(context, rotatedRect);
            }
        }

        /// <summary>
        /// 绘制旋转句柄（箭头）
        /// </summary>
        private void DrawRotationHandle(DrawingContext context, RotatedRectangleGeometry rotatedRect)
        {
            // 计算旋转句柄位置：矩形右边中点
            Point center = rotatedRect.RotationCenter;
            Point handlePoint = new Point(rotatedRect.Rectangle.X + rotatedRect.Rectangle.Width, 
                                        rotatedRect.Rectangle.Y + rotatedRect.Rectangle.Height / 2);
            
            // 应用旋转变换（使用原始角度，不进行额外翻转）
            double rotationAngle = rotatedRect.RotationAngle;
            handlePoint = RotatePointAroundCenter(handlePoint, center, rotationAngle);
            
            Point screenCenter = WorldToScreen(center);
            Point screenHandlePoint = WorldToScreen(handlePoint);
            
            // 绘制从中心到句柄点的线
            context.DrawLine(_rotationHandlePen, screenCenter, screenHandlePoint);
            
            // 绘制箭头头部（适当放大）
            DrawArrowHead(context, screenCenter, screenHandlePoint, 12, 20);
        }

        /// <summary>
        /// 绘制箭头头部
        /// </summary>
        private void DrawArrowHead(DrawingContext context, Point start, Point end, double headLength, double headAngle)
        {
            Vector direction = end - start;
            direction.Normalize();
            
            double angleRad = headAngle * Math.PI / 180.0;
            
            // 计算箭头的两个边
            Vector headVector1 = new Vector(
                direction.X * Math.Cos(Math.PI - angleRad) - direction.Y * Math.Sin(Math.PI - angleRad),
                direction.X * Math.Sin(Math.PI - angleRad) + direction.Y * Math.Cos(Math.PI - angleRad));
            
            Vector headVector2 = new Vector(
                direction.X * Math.Cos(Math.PI + angleRad) - direction.Y * Math.Sin(Math.PI + angleRad),
                direction.X * Math.Sin(Math.PI + angleRad) + direction.Y * Math.Cos(Math.PI + angleRad));
            
            Point headPoint1 = end + headVector1 * headLength;
            Point headPoint2 = end + headVector2 * headLength;
            
            // 绘制箭头头部
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(end, true, true);
                ctx.LineTo(headPoint1, true, false);
                ctx.LineTo(headPoint2, true, false);
            }
            geometry.Freeze();
            
            context.DrawGeometry(RotationHandleBrush, _rotationHandlePen, geometry);
        }

        /// <summary>
        /// 获取旋转矩形的角点
        /// </summary>
        private Point[] GetRotatedCorners(RotatedRectangleGeometry rotatedRect)
        {
            var rect = rotatedRect.Rectangle;
            Point[] corners = {
                new Point(rect.Left, rect.Top),       // 左上
                new Point(rect.Right, rect.Top),      // 右上
                new Point(rect.Right, rect.Bottom),   // 右下
                new Point(rect.Left, rect.Bottom)     // 左下
            };

            // 如果有旋转角度，旋转所有角点（使用原始角度，不进行额外翻转）
            if (Math.Abs(rotatedRect.RotationAngle) > 0.001)
            {
                double rotationAngle = rotatedRect.RotationAngle;
                
                for (int i = 0; i < corners.Length; i++)
                {
                    corners[i] = RotatePointAroundCenter(corners[i], rotatedRect.RotationCenter, rotationAngle);
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

        #region 字段

        private Rect? _selectionRect;
        private Point? _selectionStartPoint;
        private IGeometry? _editingGeometry;
        private int _hoveredControlPointIndex = -1; // 悬停的控制点索引
        private int _selectedControlPointIndex = -1; // 选中的控制点索引
        private int _draggedControlPointIndex = -1; // 拖拽的控制点索引

        // 画笔缓存
        private Pen? _selectionPen;
        private Pen? _controlPointPen;
        private Pen? _rotationHandlePen;
        private Pen? _hoverControlPointPen; // 悬停状态的画笔
        private Pen? _selectedControlPointPen; // 选中状态的画笔

        #endregion
    }
}