using System.Runtime.CompilerServices;
using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Layers
{
    /// <summary>
    /// 网格背景图层 - 显示背景网格，帮助用户对齐
    /// 基于XNode的GridLayer设计，提供高效的网格绘制
    /// </summary>
    public class GridLayer : LayerBase
    {
        #region 属性

        /// <summary>网格单元宽度（世界坐标单位）</summary>
        public double GridCellWidth
        {
            get => _gridCellWidth;
            set
            {
                if (value > 0 && Math.Abs(_gridCellWidth - value) > 0.001)
                {
                    _gridCellWidth = value;
                    Update();
                }
            }
        }

        /// <summary>网格单元高度（世界坐标单位）</summary>
        public double GridCellHeight
        {
            get => _gridCellHeight;
            set
            {
                if (value > 0 && Math.Abs(_gridCellHeight - value) > 0.001)
                {
                    _gridCellHeight = value;
                    Update();
                }
            }
        }

        /// <summary>主网格线颜色</summary>
        public Color MajorGridColor
        {
            get => _majorGridColor;
            set
            {
                if (_majorGridColor != value)
                {
                    _majorGridColor = value;
                    CreatePens();
                    Update();
                }
            }
        }

        /// <summary>次网格线颜色</summary>
        public Color MinorGridColor
        {
            get => _minorGridColor;
            set
            {
                if (_minorGridColor != value)
                {
                    _minorGridColor = value;
                    CreatePens();
                    Update();
                }
            }
        }

        /// <summary>主网格线粗细</summary>
        public double MajorGridThickness
        {
            get => _majorGridThickness;
            set
            {
                if (value > 0 && Math.Abs(_majorGridThickness - value) > 0.001)
                {
                    _majorGridThickness = value;
                    CreatePens();
                    Update();
                }
            }
        }

        /// <summary>次网格线粗细</summary>
        public double MinorGridThickness
        {
            get => _minorGridThickness;
            set
            {
                if (value > 0 && Math.Abs(_minorGridThickness - value) > 0.001)
                {
                    _minorGridThickness = value;
                    CreatePens();
                    Update();
                }
            }
        }

        /// <summary>次网格细分数</summary>
        public int MinorGridSubdivisions
        {
            get => _minorGridSubdivisions;
            set
            {
                if (value > 0 && _minorGridSubdivisions != value)
                {
                    _minorGridSubdivisions = value;
                    Update();
                }
            }
        }

        /// <summary>是否启用棋盘格背景</summary>
        public bool ShowCheckerboard
        {
            get => _showCheckerboard;
            set
            {
                if (_showCheckerboard != value)
                {
                    _showCheckerboard = value;
                    CreateBrushes();
                    Update();
                }
            }
        }

        /// <summary>棋盘格深色块颜色</summary>
        public Color CheckerboardDarkColor
        {
            get => _checkerboardDarkColor;
            set
            {
                if (_checkerboardDarkColor != value)
                {
                    _checkerboardDarkColor = value;
                    CreateBrushes();
                    Update();
                }
            }
        }

        /// <summary>棋盘格浅色块颜色</summary>
        public Color CheckerboardLightColor
        {
            get => _checkerboardLightColor;
            set
            {
                if (_checkerboardLightColor != value)
                {
                    _checkerboardLightColor = value;
                    CreateBrushes();
                    Update();
                }
            }
        }

        #endregion

        #region 构造函数

        public GridLayer()
        {
            _gridCellWidth = 50.0;
            _gridCellHeight = 50.0;
            _majorGridColor = Color.FromArgb(100, 128, 128, 128);
            _minorGridColor = Color.FromArgb(50, 128, 128, 128);
            _majorGridThickness = 1.0;
            _minorGridThickness = 0.5;
            _minorGridSubdivisions = 5;
            
            // 棋盘格默认设置
            _showCheckerboard = true;
            _checkerboardDarkColor = Color.FromArgb(20, 64, 64, 64);   // 深灰色，低透明度
            _checkerboardLightColor = Color.FromArgb(10, 192, 192, 192); // 浅灰色，更低透明度

            CreatePens();
            CreateBrushes();
        }

        #endregion

        #region LayerBase 实现

        public override void Initialize()
        {
            CreatePens();
            CreateBrushes();
        }

        protected override void OnRender(DrawingContext context)
        {
            if (!IsVisible || Transform == null) return;

            // 先绘制棋盘格背景，再绘制网格线
            if (_showCheckerboard)
            {
                DrawCheckerboard(context);
            }
            
            DrawGrid(context);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 创建画笔
        /// </summary>
        private void CreatePens()
        {
            _majorGridPen?.Brush?.Freeze(); // 防御性编程
            _minorGridPen?.Brush?.Freeze();
            
            _majorGridPen = new Pen(new SolidColorBrush(_majorGridColor), _majorGridThickness);
            _majorGridPen.Freeze();

            _minorGridPen = new Pen(new SolidColorBrush(_minorGridColor), _minorGridThickness);
            _minorGridPen.Freeze();
        }

        /// <summary>
        /// 创建画刷
        /// </summary>
        private void CreateBrushes()
        {
            _checkerboardDarkBrush = new SolidColorBrush(_checkerboardDarkColor);
            _checkerboardDarkBrush.Freeze();
            
            _checkerboardLightBrush = new SolidColorBrush(_checkerboardLightColor);
            _checkerboardLightBrush.Freeze();
        }

        /// <summary>
        /// 绘制棋盘格背景
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawCheckerboard(DrawingContext context)
        {
            if (Transform == null || _checkerboardDarkBrush == null || _checkerboardLightBrush == null) return;

            // 获取视口范围（屏幕坐标）
            Rect viewport = new Rect(0, 0, ViewportWidth, ViewportHeight);
            
            // 转换为世界坐标范围
            Rect worldViewport = Transform.ScreenToWorld(viewport);

            // 计算棋盘格绘制范围
            double startX = Math.Floor(worldViewport.Left / _gridCellWidth) * _gridCellWidth;
            double endX = Math.Ceiling(worldViewport.Right / _gridCellWidth) * _gridCellWidth;
            double startY = Math.Floor(worldViewport.Top / _gridCellHeight) * _gridCellHeight;
            double endY = Math.Ceiling(worldViewport.Bottom / _gridCellHeight) * _gridCellHeight;

            // 使用世界坐标计算稳定的棋盘格模式
            for (double x = startX; x < endX; x += _gridCellWidth)
            {
                for (double y = startY; y < endY; y += _gridCellHeight)
                {
                    // 基于世界坐标计算网格索引，确保缩放时颜色稳定
                    int gridX = (int)Math.Floor(x / _gridCellWidth);
                    int gridY = (int)Math.Floor(y / _gridCellHeight);
                    
                    // 计算是否为深色块（棋盘模式）
                    bool isDark = (gridX + gridY) % 2 == 0;
                    Brush brush = isDark ? _checkerboardDarkBrush : _checkerboardLightBrush;

                    // 转换为屏幕坐标
                    Point topLeft = WorldToScreen(new Point(x, y));
                    Point bottomRight = WorldToScreen(new Point(x + _gridCellWidth, y + _gridCellHeight));
                    
                    // 绘制矩形块
                    Rect screenRect = new Rect(topLeft, bottomRight);
                    context.DrawRectangle(brush, null, screenRect);
                }
            }
        }

        /// <summary>
        /// 绘制网格
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGrid(DrawingContext context)
        {
            if (Transform == null) return;

            // 获取视口范围（屏幕坐标）
            Rect viewport = new Rect(0, 0, ViewportWidth, ViewportHeight);
            
            // 转换为世界坐标范围
            Rect worldViewport = Transform.ScreenToWorld(viewport);

            // 计算网格绘制范围
            double startX = Math.Floor(worldViewport.Left / _gridCellWidth) * _gridCellWidth;
            double endX = Math.Ceiling(worldViewport.Right / _gridCellWidth) * _gridCellWidth;
            double startY = Math.Floor(worldViewport.Top / _gridCellHeight) * _gridCellHeight;
            double endY = Math.Ceiling(worldViewport.Bottom / _gridCellHeight) * _gridCellHeight;

            // 绘制次网格线
            if (_minorGridSubdivisions > 1 && _minorGridPen != null)
            {
                double minorCellWidth = _gridCellWidth / _minorGridSubdivisions;
                double minorCellHeight = _gridCellHeight / _minorGridSubdivisions;

                DrawGridLines(context, startX, endX, startY, endY, minorCellWidth, minorCellHeight, _minorGridPen);
            }

            // 绘制主网格线
            if (_majorGridPen != null)
            {
                DrawGridLines(context, startX, endX, startY, endY, _gridCellWidth, _gridCellHeight, _majorGridPen);
            }

            // 绘制坐标轴（如果经过原点）
            if (worldViewport.Contains(new Point(0, 0)))
            {
                DrawAxes(context);
            }
        }

        /// <summary>
        /// 绘制网格线
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawGridLines(DrawingContext context, double startX, double endX, double startY, double endY, 
            double cellWidth, double cellHeight, Pen pen)
        {
            // 绘制垂直线
            for (double x = startX; x <= endX; x += cellWidth)
            {
                Point worldStart = new Point(x, startY);
                Point worldEnd = new Point(x, endY);
                Point screenStart = WorldToScreen(worldStart);
                Point screenEnd = WorldToScreen(worldEnd);

                // 边界检查
                if (IsVerticalLineVisible(screenStart.X))
                {
                    context.DrawLine(pen, 
                        new Point(screenStart.X, 0), 
                        new Point(screenEnd.X, ViewportHeight));
                }
            }

            // 绘制水平线
            for (double y = startY; y <= endY; y += cellHeight)
            {
                Point worldStart = new Point(startX, y);
                Point worldEnd = new Point(endX, y);
                Point screenStart = WorldToScreen(worldStart);
                Point screenEnd = WorldToScreen(worldEnd);

                // 边界检查
                if (IsHorizontalLineVisible(screenStart.Y))
                {
                    context.DrawLine(pen, 
                        new Point(0, screenStart.Y), 
                        new Point(ViewportWidth, screenEnd.Y));
                }
            }
        }

        /// <summary>
        /// 绘制坐标轴
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DrawAxes(DrawingContext context)
        {
            if (_axesPen == null)
            {
                _axesPen = new Pen(new SolidColorBrush(Colors.Red), 2.0);
                _axesPen.Freeze();
            }

            Point origin = WorldToScreen(new Point(0, 0));

            // X轴
            if (IsHorizontalLineVisible(origin.Y))
            {
                context.DrawLine(_axesPen, new Point(0, origin.Y), new Point(ViewportWidth, origin.Y));
            }

            // Y轴
            if (IsVerticalLineVisible(origin.X))
            {
                context.DrawLine(_axesPen, new Point(origin.X, 0), new Point(origin.X, ViewportHeight));
            }
        }

        /// <summary>
        /// 检查垂直线是否可见
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVerticalLineVisible(double screenX)
        {
            return screenX >= 0 && screenX <= ViewportWidth;
        }

        /// <summary>
        /// 检查水平线是否可见
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsHorizontalLineVisible(double screenY)
        {
            return screenY >= 0 && screenY <= ViewportHeight;
        }

        #endregion

        #region 字段

        private double _gridCellWidth;
        private double _gridCellHeight;
        private Color _majorGridColor;
        private Color _minorGridColor;
        private double _majorGridThickness;
        private double _minorGridThickness;
        private int _minorGridSubdivisions;
        
        // 棋盘格相关
        private bool _showCheckerboard;
        private Color _checkerboardDarkColor;
        private Color _checkerboardLightColor;

        // 画笔缓存
        private Pen? _majorGridPen;
        private Pen? _minorGridPen;
        private Pen? _axesPen;
        
        // 画刷缓存
        private Brush? _checkerboardDarkBrush;
        private Brush? _checkerboardLightBrush;

        #endregion
    }
}