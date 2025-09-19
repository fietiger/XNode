using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Shapes
{
    /// <summary>
    /// 有向线段几何图形（箭头）
    /// </summary>
    public class ArrowLineGeometry : LineGeometry
    {
        #region 属性

        public override string TypeName => "ArrowLine";

        /// <summary>箭头头部长度</summary>
        public double ArrowHeadLength
        {
            get => _arrowHeadLength;
            set
            {
                double newLength = Math.Max(1, value);
                if (Math.Abs(_arrowHeadLength - newLength) > 0.001)
                {
                    _arrowHeadLength = newLength;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        /// <summary>箭头头部角度（度）</summary>
        public double ArrowHeadAngle
        {
            get => _arrowHeadAngle;
            set
            {
                double newAngle = Math.Max(10, Math.Min(90, value));
                if (Math.Abs(_arrowHeadAngle - newAngle) > 0.001)
                {
                    _arrowHeadAngle = newAngle;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        /// <summary>是否显示箭头头部</summary>
        public bool ShowArrowHead
        {
            get => _showArrowHead;
            set
            {
                if (_showArrowHead != value)
                {
                    _showArrowHead = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        /// <summary>是否显示箭头尾部</summary>
        public bool ShowArrowTail
        {
            get => _showArrowTail;
            set
            {
                if (_showArrowTail != value)
                {
                    _showArrowTail = value;
                    OnGeometryChanged(GeometryChangeType.Appearance);
                }
            }
        }

        #endregion

        #region 构造函数

        public ArrowLineGeometry() : this(new Point(0, 0), new Point(100, 100))
        {
        }

        public ArrowLineGeometry(Point startPoint, Point endPoint) : base(startPoint, endPoint)
        {
            _arrowHeadLength = 15.0;
            _arrowHeadAngle = 30.0;
            _showArrowHead = true;
            _showArrowTail = false;
        }

        public ArrowLineGeometry(double x1, double y1, double x2, double y2) : base(x1, y1, x2, y2)
        {
            _arrowHeadLength = 15.0;
            _arrowHeadAngle = 30.0;
            _showArrowHead = true;
            _showArrowTail = false;
        }

        #endregion

        #region GeometryBase 重写

        public override void Render(DrawingContext context, Core.ViewportTransform? transform)
        {
            if (!IsVisible) return;

            ApplyRenderTransform(context, ctx =>
            {
                Point screenStart = transform?.WorldToScreen(StartPoint) ?? StartPoint;
                Point screenEnd = transform?.WorldToScreen(EndPoint) ?? EndPoint;

                // 绘制主线段
                Pen renderPen = Stroke ?? CreateDefaultPen(transform);
                ctx.DrawLine(renderPen, screenStart, screenEnd);

                // 计算箭头参数
                double screenArrowLength = _arrowHeadLength;
                if (transform != null)
                {
                    screenArrowLength *= transform.Scale;
                }

                // 绘制箭头头部
                if (_showArrowHead)
                {
                    DrawArrowHead(ctx, screenStart, screenEnd, screenArrowLength, renderPen);
                }

                // 绘制箭头尾部
                if (_showArrowTail)
                {
                    DrawArrowTail(ctx, screenEnd, screenStart, screenArrowLength, renderPen);
                }
            });
        }

        public override IGeometry Clone()
        {
            var clone = new ArrowLineGeometry(StartPoint, EndPoint)
            {
                Thickness = Thickness,
                ArrowHeadLength = _arrowHeadLength,
                ArrowHeadAngle = _arrowHeadAngle,
                ShowArrowHead = _showArrowHead,
                ShowArrowTail = _showArrowTail,
                Fill = Fill,
                Stroke = Stroke,
                Opacity = Opacity,
                IsVisible = IsVisible,
                ZOrder = ZOrder
            };
            return clone;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 绘制箭头头部
        /// </summary>
        private void DrawArrowHead(DrawingContext context, Point lineStart, Point lineEnd, double arrowLength, Pen pen)
        {
            if (arrowLength <= 0) return;

            // 计算线段方向向量
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lineLength = Math.Sqrt(dx * dx + dy * dy);
            
            if (lineLength < 0.001) return;

            // 单位方向向量
            double unitX = dx / lineLength;
            double unitY = dy / lineLength;

            // 箭头角度（弧度）
            double arrowAngleRad = _arrowHeadAngle * Math.PI / 180;
            double cosAngle = Math.Cos(arrowAngleRad);
            double sinAngle = Math.Sin(arrowAngleRad);

            // 计算箭头的两个边点
            Point arrowPoint1 = new Point(
                lineEnd.X - arrowLength * (unitX * cosAngle - unitY * sinAngle),
                lineEnd.Y - arrowLength * (unitY * cosAngle + unitX * sinAngle)
            );

            Point arrowPoint2 = new Point(
                lineEnd.X - arrowLength * (unitX * cosAngle + unitY * sinAngle),
                lineEnd.Y - arrowLength * (unitY * cosAngle - unitX * sinAngle)
            );

            // 绘制箭头线段
            context.DrawLine(pen, lineEnd, arrowPoint1);
            context.DrawLine(pen, lineEnd, arrowPoint2);

            // 可选：绘制填充的箭头头部
            if (Fill != null)
            {
                var geometry = new StreamGeometry();
                using (var geometryContext = geometry.Open())
                {
                    geometryContext.BeginFigure(lineEnd, true, true);
                    geometryContext.LineTo(arrowPoint1, true, false);
                    geometryContext.LineTo(arrowPoint2, true, false);
                }
                geometry.Freeze();
                context.DrawGeometry(Fill, null, geometry);
            }
        }

        /// <summary>
        /// 绘制箭头尾部
        /// </summary>
        private void DrawArrowTail(DrawingContext context, Point lineStart, Point lineEnd, double arrowLength, Pen pen)
        {
            if (arrowLength <= 0) return;

            // 计算线段方向向量（相反方向）
            double dx = lineStart.X - lineEnd.X;
            double dy = lineStart.Y - lineEnd.Y;
            double lineLength = Math.Sqrt(dx * dx + dy * dy);
            
            if (lineLength < 0.001) return;

            // 单位方向向量
            double unitX = dx / lineLength;
            double unitY = dy / lineLength;

            // 箭头角度（弧度）
            double arrowAngleRad = _arrowHeadAngle * Math.PI / 180;
            double cosAngle = Math.Cos(arrowAngleRad);
            double sinAngle = Math.Sin(arrowAngleRad);

            // 计算箭头尾部的两个边点
            Point arrowPoint1 = new Point(
                lineStart.X - arrowLength * (unitX * cosAngle - unitY * sinAngle),
                lineStart.Y - arrowLength * (unitY * cosAngle + unitX * sinAngle)
            );

            Point arrowPoint2 = new Point(
                lineStart.X - arrowLength * (unitX * cosAngle + unitY * sinAngle),
                lineStart.Y - arrowLength * (unitY * cosAngle - unitX * sinAngle)
            );

            // 绘制箭头尾部线段
            context.DrawLine(pen, lineStart, arrowPoint1);
            context.DrawLine(pen, lineStart, arrowPoint2);
        }

        /// <summary>
        /// 创建默认画笔
        /// </summary>
        private Pen CreateDefaultPen(Core.ViewportTransform? transform)
        {
            double screenThickness = Thickness;
            if (transform != null)
            {
                screenThickness *= transform.Scale;
            }
            
            var pen = new Pen(Brushes.Black, screenThickness);
            pen.Freeze();
            return pen;
        }

        #endregion

        #region 字段

        private double _arrowHeadLength;
        private double _arrowHeadAngle;
        private bool _showArrowHead;
        private bool _showArrowTail;

        #endregion
    }
}