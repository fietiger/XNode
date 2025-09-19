using System;
using System.Windows;

namespace XDisplay.Core
{
    /// <summary>
    /// 视口变换管理器 - 处理缩放、平移等坐标变换
    /// 提供屏幕坐标与世界坐标之间的高效转换
    /// </summary>
    public class ViewportTransform
    {
        #region 属性

        /// <summary>缩放比例</summary>
        public double Scale
        {
            get => _scale;
            set
            {
                _scale = Math.Max(MinScale, Math.Min(MaxScale, value));
                OnTransformChanged();
            }
        }

        /// <summary>平移偏移</summary>
        public Point Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                OnTransformChanged();
            }
        }

        /// <summary>视口中心点（世界坐标系）</summary>
        public Point ViewportCenter
        {
            get => new Point(_viewportSize.Width / 2, _viewportSize.Height / 2);
        }

        /// <summary>最小缩放比例</summary>
        public double MinScale { get; set; } = 0.1;

        /// <summary>最大缩放比例</summary>
        public double MaxScale { get; set; } = 10.0;

        /// <summary>视口尺寸</summary>
        public Size ViewportSize
        {
            get => _viewportSize;
            set
            {
                _viewportSize = value;
                OnTransformChanged();
            }
        }

        /// <summary>Y轴是否向上（默认false，向下为正Y；true时向上为正Y）</summary>
        public bool YAxisUp
        {
            get => _yAxisUp;
            set
            {
                if (_yAxisUp != value)
                {
                    _yAxisUp = value;
                    
                    // 坐标系切换时，需要调整偏移量以保持视觉稳定性
                    // Y轴翻转后，原来的偏移量也需要相应翻转
                    _offset.Y = -_offset.Y;
                    
                    OnTransformChanged();
                }
            }
        }

        #endregion

        #region 事件

        /// <summary>变换发生改变时触发</summary>
        public event EventHandler? TransformChanged;

        #endregion

        #region 构造函数

        public ViewportTransform()
        {
            _scale = 1.0;
            _offset = new Point(0, 0);
            _viewportSize = new Size(800, 600);
            _yAxisUp = false;
        }

        #endregion

        #region 坐标转换方法

        /// <summary>
        /// 屏幕坐标转世界坐标
        /// </summary>
        /// <param name="screenPoint">屏幕坐标点</param>
        /// <returns>世界坐标点</returns>
        public Point ScreenToWorld(Point screenPoint)
        {
            Point center = ViewportCenter;
            double worldX = (screenPoint.X - center.X - _offset.X) / _scale;
            double worldY = (screenPoint.Y - center.Y - _offset.Y) / _scale;
            
            // 应用Y轴翻转（如果需要）
            if (_yAxisUp)
            {
                worldY = -worldY;
            }
            
            return new Point(worldX, worldY);
        }

        /// <summary>
        /// 世界坐标转屏幕坐标
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <returns>屏幕坐标点</returns>
        public Point WorldToScreen(Point worldPoint)
        {
            Point center = ViewportCenter;
            
            // 先应用Y轴翻转（如果需要）
            double adjustedWorldY = _yAxisUp ? -worldPoint.Y : worldPoint.Y;
            
            double screenX = worldPoint.X * _scale + center.X + _offset.X;
            double screenY = adjustedWorldY * _scale + center.Y + _offset.Y;
            
            return new Point(screenX, screenY);
        }

        /// <summary>
        /// 世界坐标尺寸转屏幕坐标尺寸
        /// </summary>
        /// <param name="worldSize">世界坐标尺寸</param>
        /// <returns>屏幕坐标尺寸</returns>
        public Size WorldToScreen(Size worldSize)
        {
            return new Size(worldSize.Width * _scale, worldSize.Height * _scale);
        }

        /// <summary>
        /// 屏幕坐标尺寸转世界坐标尺寸
        /// </summary>
        /// <param name="screenSize">屏幕坐标尺寸</param>
        /// <returns>世界坐标尺寸</returns>
        public Size ScreenToWorld(Size screenSize)
        {
            return new Size(screenSize.Width / _scale, screenSize.Height / _scale);
        }

        /// <summary>
        /// 世界坐标矩形转屏幕坐标矩形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>屏幕坐标矩形</returns>
        public Rect WorldToScreen(Rect worldRect)
        {
            // 对于矩形，需要正确处理Y轴翻转时的上下的变化
            if (_yAxisUp)
            {
                // Y轴向上时，需要翻转矩形的Y坐标
                Point topLeft = WorldToScreen(new Point(worldRect.Left, worldRect.Top));
                Point bottomRight = WorldToScreen(new Point(worldRect.Right, worldRect.Bottom));
                
                // 确保TopLeft在左上角，BottomRight在右下角
                double left = Math.Min(topLeft.X, bottomRight.X);
                double top = Math.Min(topLeft.Y, bottomRight.Y);
                double right = Math.Max(topLeft.X, bottomRight.X);
                double bottom = Math.Max(topLeft.Y, bottomRight.Y);
                
                return new Rect(left, top, right - left, bottom - top);
            }
            else
            {
                Point topLeft = WorldToScreen(worldRect.TopLeft);
                Size size = WorldToScreen(worldRect.Size);
                return new Rect(topLeft, size);
            }
        }

        /// <summary>
        /// 屏幕坐标矩形转世界坐标矩形
        /// </summary>
        /// <param name="screenRect">屏幕坐标矩形</param>
        /// <returns>世界坐标矩形</returns>
        public Rect ScreenToWorld(Rect screenRect)
        {
            // 对于矩形，需要正确处理Y轴翻转时的上下的变化
            if (_yAxisUp)
            {
                Point topLeft = ScreenToWorld(screenRect.TopLeft);
                Point bottomRight = ScreenToWorld(screenRect.BottomRight);
                
                // Y轴翻转后，需要重新排列上下坐标
                double left = Math.Min(topLeft.X, bottomRight.X);
                double top = Math.Max(topLeft.Y, bottomRight.Y); // Y轴向上时，top是更大的Y值
                double right = Math.Max(topLeft.X, bottomRight.X);
                double bottom = Math.Min(topLeft.Y, bottomRight.Y); // bottom是更小的Y值
                
                return new Rect(left, bottom, right - left, top - bottom);
            }
            else
            {
                Point topLeft = ScreenToWorld(screenRect.TopLeft);
                Size size = ScreenToWorld(screenRect.Size);
                return new Rect(topLeft, size);
            }
        }

        #endregion

        #region 变换操作方法

        /// <summary>
        /// 在指定点进行缩放
        /// </summary>
        /// <param name="scaleCenter">缩放中心点（屏幕坐标）</param>
        /// <param name="scaleFactor">缩放因子</param>
        public void ScaleAt(Point scaleCenter, double scaleFactor)
        {
            // 计算缩放前的世界坐标点
            Point worldPoint = ScreenToWorld(scaleCenter);
            
            // 应用缩放
            Scale *= scaleFactor;
            
            // 计算缩放后该点在屏幕上的新位置
            Point newScreenPoint = WorldToScreen(worldPoint);
            
            // 调整偏移，使缩放中心点保持不变
            _offset.X += scaleCenter.X - newScreenPoint.X;
            _offset.Y += scaleCenter.Y - newScreenPoint.Y;
            
            OnTransformChanged();
        }

        /// <summary>
        /// 平移视口
        /// </summary>
        /// <param name="deltaX">X轴平移量</param>
        /// <param name="deltaY">Y轴平移量</param>
        public void Translate(double deltaX, double deltaY)
        {
            _offset.X += deltaX;
            _offset.Y += deltaY;
            OnTransformChanged();
        }

        /// <summary>
        /// 重置变换到默认状态
        /// </summary>
        public void Reset()
        {
            _scale = 1.0;
            _offset = new Point(0, 0);
            OnTransformChanged();
        }

        /// <summary>
        /// 设置坐标系方向
        /// </summary>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public void SetCoordinateSystem(bool yAxisUp)
        {
            YAxisUp = yAxisUp;
        }

        /// <summary>
        /// 适应视口大小（缩放以适应指定的世界坐标矩形）
        /// </summary>
        /// <param name="worldBounds">要适应的世界坐标矩形</param>
        /// <param name="margin">边距（像素）</param>
        public void FitToBounds(Rect worldBounds, double margin = 20)
        {
            if (worldBounds.IsEmpty || _viewportSize.IsEmpty)
                return;

            // 计算缩放比例
            double scaleX = (_viewportSize.Width - 2 * margin) / worldBounds.Width;
            double scaleY = (_viewportSize.Height - 2 * margin) / worldBounds.Height;
            
            // 使用较小的缩放比例以确保完全可见
            Scale = Math.Min(scaleX, scaleY);
            
            // 计算偏移以居中显示
            Point worldCenter = new Point(
                worldBounds.X + worldBounds.Width / 2,
                worldBounds.Y + worldBounds.Height / 2);
            Point screenCenter = ViewportCenter;
            Point targetScreenPoint = WorldToScreen(worldCenter);
            
            _offset.X += screenCenter.X - targetScreenPoint.X;
            _offset.Y += screenCenter.Y - targetScreenPoint.Y;
            
            OnTransformChanged();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 触发变换改变事件
        /// </summary>
        private void OnTransformChanged()
        {
            TransformChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 字段

        /// <summary>缩放比例</summary>
        private double _scale;

        /// <summary>平移偏移</summary>
        private Point _offset;

        /// <summary>视口尺寸</summary>
        private Size _viewportSize;

        /// <summary>Y轴是否向上</summary>
        private bool _yAxisUp;

        #endregion
    }
}