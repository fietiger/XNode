using System;
using System.Windows;
using System.Windows.Media;
using XDisplay.Geometry;

namespace XDisplay.Layers
{
    /// <summary>
    /// 绘制预览图层 - 显示正在绘制的图形预览
    /// </summary>
    public class DrawingPreviewLayer : LayerBase
    {
        #region 属性

        /// <summary>预览几何图形</summary>
        public IGeometry? PreviewGeometry { get; set; }

        /// <summary>是否显示预览</summary>
        public bool ShowPreview => PreviewGeometry != null;

        #endregion

        #region 构造函数

        public DrawingPreviewLayer()
        {
            // 初始化预览画笔 - 绿色虚线
            InitializePreviewPen();
        }

        #endregion

        #region LayerBase 实现

        protected override void OnRender(DrawingContext context)
        {
            if (!ShowPreview || PreviewGeometry == null)
                return;

            // 创建预览版本的几何图形，使用绿色虚线样式
            var previewGeometry = PreviewGeometry.Clone();
            previewGeometry.Stroke = _previewPen;
            previewGeometry.Fill = _previewFill;

            // 渲染预览图形
            previewGeometry.Render(context, Transform);
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 设置矩形预览
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        public void SetRectanglePreview(Point startPoint, Point endPoint)
        {
            PreviewGeometry = new Geometry.Shapes.RectangleGeometry(new Rect(startPoint, endPoint));
            Update();
        }

        /// <summary>
        /// 设置圆形预览
        /// </summary>
        /// <param name="center">圆心</param>
        /// <param name="radius">半径</param>
        public void SetCirclePreview(Point center, double radius)
        {
            PreviewGeometry = new Geometry.Shapes.CircleGeometry(center, radius);
            Update();
        }

        /// <summary>
        /// 设置线段预览
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        public void SetLinePreview(Point startPoint, Point endPoint)
        {
            PreviewGeometry = new Geometry.Shapes.LineGeometry(startPoint, endPoint);
            Update();
        }

        /// <summary>
        /// 设置箭头线段预览
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        public void SetArrowLinePreview(Point startPoint, Point endPoint)
        {
            PreviewGeometry = new Geometry.Shapes.ArrowLineGeometry(startPoint, endPoint);
            Update();
        }

        /// <summary>
        /// 设置旋转矩形预览
        /// </summary>
        /// <param name="startPoint">起始点</param>
        /// <param name="endPoint">结束点</param>
        /// <param name="rotationAngle">旋转角度</param>
        public void SetRotatedRectanglePreview(Point startPoint, Point endPoint, double rotationAngle = 0)
        {
            PreviewGeometry = new Geometry.Shapes.RotatedRectangleGeometry(new Rect(startPoint, endPoint), rotationAngle);
            Update();
        }

        /// <summary>
        /// 清除预览
        /// </summary>
        public void ClearPreview()
        {
            PreviewGeometry = null;
            Update();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化预览画笔
        /// </summary>
        private void InitializePreviewPen()
        {
            // 创建绿色虚线画笔
            _previewPen = new Pen(
                new SolidColorBrush(Color.FromArgb(200, 0, 255, 0)), // 半透明绿色
                2.0)
            {
                DashStyle = DashStyles.Dash
            };
            _previewPen.Freeze();

            // 创建半透明绿色填充
            _previewFill = new SolidColorBrush(Color.FromArgb(30, 0, 255, 0));
            _previewFill.Freeze();
        }

        #endregion

        #region 字段

        /// <summary>预览画笔</summary>
        private Pen _previewPen = null!;

        /// <summary>预览填充</summary>
        private Brush _previewFill = null!;

        #endregion
    }
}