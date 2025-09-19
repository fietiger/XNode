using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using XDisplay.Core;

namespace XDisplay.Layers
{
    /// <summary>
    /// 图层基类 - 所有图层的抽象基础
    /// 基于XNode的SingleBoard架构，提供高效的单一DrawingVisual绘制
    /// </summary>
    public abstract class LayerBase : FrameworkElement
    {
        #region 属性

        /// <summary>图层是否可见</summary>
        public new bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    if (_isVisible)
                        Update();
                    else
                        Clear();
                }
            }
        }

        /// <summary>视口变换管理器</summary>
        public ViewportTransform? Transform { get; set; }

        /// <summary>图层Z序（用于排序）</summary>
        public int ZOrder { get; set; } = 0;

        /// <summary>获取内部的DrawingVisual</summary>
        public DrawingVisual DrawingVisual => _drawingVisual;

        /// <summary>视口宽度</summary>
        public double ViewportWidth { get; set; }

        /// <summary>视口高度</summary>
        public double ViewportHeight { get; set; }

        #endregion

        #region FrameworkElement 重写

        protected override int VisualChildrenCount => 0; // LayerBase本身不包含子元素，DrawingVisual由DisplayCanvas管理

        protected override Visual GetVisualChild(int index) => throw new ArgumentOutOfRangeException(nameof(index));

        #endregion

        #region 构造函数

        protected LayerBase()
        {
            _drawingVisual = new DrawingVisual();
            // 注意：DrawingVisual将由DisplayCanvas管理，不在此处添加为子元素
            // 这样避免了同一个DrawingVisual有多个父级的冲突
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 初始化图层（子类可重写进行资源初始化，如冻结画笔）
        /// </summary>
        public virtual void Initialize()
        {
            // 子类重写进行初始化
        }

        /// <summary>
        /// 更新图层绘制
        /// </summary>
        public void Update()
        {
            if (!_isVisible) return;

            using (var context = _drawingVisual.RenderOpen())
            {
                OnRender(context);
            }
        }

        /// <summary>
        /// 清空图层内容
        /// </summary>
        public void Clear()
        {
            using (var context = _drawingVisual.RenderOpen())
            {
                // 不绘制任何内容，相当于清空
            }
        }

        /// <summary>
        /// 刷新图层（强制重绘）
        /// </summary>
        public void Refresh()
        {
            Update();
        }

        /// <summary>
        /// 获取图层的绘制区域
        /// </summary>
        /// <returns>绘制区域矩形</returns>
        public virtual Rect GetBounds()
        {
            return new Rect(0, 0, ViewportWidth, ViewportHeight);
        }

        #endregion

        #region 保护方法

        /// <summary>
        /// 执行具体的绘制逻辑（子类必须实现）
        /// </summary>
        /// <param name="context">绘制上下文</param>
        protected new abstract void OnRender(DrawingContext context);

        /// <summary>
        /// 屏幕坐标转世界坐标
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>世界坐标点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Point ScreenToWorld(Point screenPoint)
        {
            return Transform?.ScreenToWorld(screenPoint) ?? screenPoint;
        }

        /// <summary>
        /// 世界坐标转屏幕坐标
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <returns>屏幕坐标点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Point WorldToScreen(Point worldPoint)
        {
            return Transform?.WorldToScreen(worldPoint) ?? worldPoint;
        }

        /// <summary>
        /// 世界坐标尺寸转屏幕坐标尺寸
        /// </summary>
        /// <param name="worldSize">世界坐标尺寸</param>
        /// <returns>屏幕坐标尺寸</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Size WorldToScreen(Size worldSize)
        {
            return Transform?.WorldToScreen(worldSize) ?? worldSize;
        }

        /// <summary>
        /// 世界坐标矩形转屏幕坐标矩形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>屏幕坐标矩形</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Rect WorldToScreen(Rect worldRect)
        {
            return Transform?.WorldToScreen(worldRect) ?? worldRect;
        }

        /// <summary>
        /// 检查矩形是否在视口范围内（用于视口剪裁优化）
        /// </summary>
        /// <param name="screenRect">屏幕坐标矩形</param>
        /// <returns>是否可见</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsRectVisible(Rect screenRect)
        {
            Rect viewport = new Rect(0, 0, ViewportWidth, ViewportHeight);
            return viewport.IntersectsWith(screenRect);
        }

        /// <summary>
        /// 绘制带边界检查的线段（性能优化）
        /// </summary>
        /// <param name="context">绘制上下文</param>
        /// <param name="pen">画笔</param>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void DrawLineWithBoundsCheck(DrawingContext context, Pen pen, Point start, Point end)
        {
            // 防御性编程：检查pen是否为null
            if (pen == null) return;
            
            // 简单的边界检查
            Rect viewport = new Rect(0, 0, ViewportWidth, ViewportHeight);
            Rect lineBounds = new Rect(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y));

            if (viewport.IntersectsWith(lineBounds))
            {
                context.DrawLine(pen, start, end);
            }
        }

        #endregion

        #region 字段

        /// <summary>绘制可视对象</summary>
        private readonly DrawingVisual _drawingVisual;

        /// <summary>是否可见</summary>
        private bool _isVisible = true;

        #endregion
    }
}