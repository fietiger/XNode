using System;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 局部坐标系 - 定义容器的坐标变换和物理映射
    /// 支持平移、缩放、旋转以及物理单位转换
    /// </summary>
    public class LocalCoordinateSystem
    {
        #region 属性

        /// <summary>原点位置（相对于父坐标系）</summary>
        public Point Origin
        {
            get => _origin;
            set
            {
                _origin = value;
                UpdateTransformMatrix();
            }
        }

        /// <summary>缩放比例</summary>
        public double Scale
        {
            get => _scale;
            set
            {
                _scale = Math.Max(0.001, value); // 防止零或负值
                UpdateTransformMatrix();
            }
        }

        /// <summary>旋转角度（度）</summary>
        public double Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                UpdateTransformMatrix();
            }
        }

        /// <summary>Y轴是否向上（默认false，向下）</summary>
        public bool YAxisUp
        {
            get => _yAxisUp;
            set
            {
                _yAxisUp = value;
                UpdateTransformMatrix();
            }
        }

        /// <summary>物理单位到逻辑单位的比例（例如：1mm = 多少逻辑单位）</summary>
        public double PhysicalToLogicalRatio
        {
            get => _physicalToLogicalRatio;
            set
            {
                _physicalToLogicalRatio = Math.Max(0.001, value);
                UpdateTransformMatrix();
            }
        }

        /// <summary>物理原点在图像中的位置（像素坐标，用于图像映射）</summary>
        public Point PhysicalOriginInImage
        {
            get => _physicalOriginInImage;
            set => _physicalOriginInImage = value;
        }

        /// <summary>像素到物理单位的比例（例如：1像素 = 0.04mm）</summary>
        public double PixelToPhysicalRatio
        {
            get => _pixelToPhysicalRatio;
            set => _pixelToPhysicalRatio = Math.Max(0.001, value);
        }

        /// <summary>正向变换矩阵（本地到父坐标系）</summary>
        public Matrix TransformMatrix => _transformMatrix;

        /// <summary>逆向变换矩阵（父坐标系到本地）</summary>
        public Matrix InverseTransformMatrix => _inverseTransformMatrix;

        #endregion

        #region 构造函数

        public LocalCoordinateSystem()
        {
            _origin = new Point(0, 0);
            _scale = 1.0;
            _rotation = 0.0;
            _yAxisUp = false;
            _physicalToLogicalRatio = 1.0;
            _physicalOriginInImage = new Point(0, 0);
            _pixelToPhysicalRatio = 1.0;
            
            UpdateTransformMatrix();
        }

        /// <summary>
        /// 创建带物理映射的坐标系
        /// </summary>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public LocalCoordinateSystem(double pixelToPhysicalRatio, Point physicalOriginInImage, bool yAxisUp = true)
        {
            _origin = new Point(0, 0);
            _scale = 1.0;
            _rotation = 0.0;
            _yAxisUp = yAxisUp;
            _physicalToLogicalRatio = 1.0;
            _physicalOriginInImage = physicalOriginInImage;
            _pixelToPhysicalRatio = pixelToPhysicalRatio;
            
            UpdateTransformMatrix();
        }

        #endregion

        #region 坐标转换方法

        /// <summary>
        /// 本地坐标转父坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <returns>父坐标系点</returns>
        public Point LocalToParent(Point localPoint)
        {
            return _transformMatrix.Transform(localPoint);
        }

        /// <summary>
        /// 父坐标系转本地坐标
        /// </summary>
        /// <param name="parentPoint">父坐标系点</param>
        /// <returns>本地坐标点</returns>
        public Point ParentToLocal(Point parentPoint)
        {
            return _inverseTransformMatrix.Transform(parentPoint);
        }

        /// <summary>
        /// 本地矩形转父坐标系矩形
        /// </summary>
        /// <param name="localRect">本地坐标矩形</param>
        /// <returns>父坐标系矩形</returns>
        public Rect LocalToParent(Rect localRect)
        {
            Point topLeft = LocalToParent(localRect.TopLeft);
            Point bottomRight = LocalToParent(localRect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        /// <summary>
        /// 父坐标系矩形转本地矩形
        /// </summary>
        /// <param name="parentRect">父坐标系矩形</param>
        /// <returns>本地坐标矩形</returns>
        public Rect ParentToLocal(Rect parentRect)
        {
            Point topLeft = ParentToLocal(parentRect.TopLeft);
            Point bottomRight = ParentToLocal(parentRect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        /// <summary>
        /// 物理坐标转图像像素坐标
        /// </summary>
        /// <param name="physicalPoint">物理坐标点（如毫米）</param>
        /// <returns>图像像素坐标点</returns>
        public Point PhysicalToImagePixel(Point physicalPoint)
        {
            double imageX = _physicalOriginInImage.X + physicalPoint.X / _pixelToPhysicalRatio;
            double imageY = _physicalOriginInImage.Y + (_yAxisUp ? -physicalPoint.Y : physicalPoint.Y) / _pixelToPhysicalRatio;
            return new Point(imageX, imageY);
        }

        /// <summary>
        /// 图像像素坐标转物理坐标
        /// </summary>
        /// <param name="imagePixelPoint">图像像素坐标点</param>
        /// <returns>物理坐标点（如毫米）</returns>
        public Point ImagePixelToPhysical(Point imagePixelPoint)
        {
            double physicalX = (imagePixelPoint.X - _physicalOriginInImage.X) * _pixelToPhysicalRatio;
            double physicalY = (_yAxisUp ? -1 : 1) * (imagePixelPoint.Y - _physicalOriginInImage.Y) * _pixelToPhysicalRatio;
            return new Point(physicalX, physicalY);
        }

        /// <summary>
        /// 物理坐标转本地逻辑坐标
        /// </summary>
        /// <param name="physicalPoint">物理坐标点</param>
        /// <returns>本地逻辑坐标点</returns>
        public Point PhysicalToLocal(Point physicalPoint)
        {
            return new Point(
                physicalPoint.X * _physicalToLogicalRatio,
                physicalPoint.Y * _physicalToLogicalRatio);
        }

        /// <summary>
        /// 本地逻辑坐标转物理坐标
        /// </summary>
        /// <param name="localPoint">本地逻辑坐标点</param>
        /// <returns>物理坐标点</returns>
        public Point LocalToPhysical(Point localPoint)
        {
            return new Point(
                localPoint.X / _physicalToLogicalRatio,
                localPoint.Y / _physicalToLogicalRatio);
        }

        #endregion

        #region 变换操作

        /// <summary>
        /// 设置变换参数
        /// </summary>
        /// <param name="origin">原点</param>
        /// <param name="scale">缩放</param>
        /// <param name="rotation">旋转角度</param>
        public void SetTransform(Point origin, double scale, double rotation)
        {
            _origin = origin;
            _scale = Math.Max(0.001, scale);
            _rotation = rotation;
            UpdateTransformMatrix();
        }

        /// <summary>
        /// 应用平移
        /// </summary>
        /// <param name="deltaX">X轴平移量</param>
        /// <param name="deltaY">Y轴平移量</param>
        public void Translate(double deltaX, double deltaY)
        {
            _origin = new Point(_origin.X + deltaX, _origin.Y + deltaY);
            UpdateTransformMatrix();
        }

        /// <summary>
        /// 应用缩放
        /// </summary>
        /// <param name="scaleFactor">缩放因子</param>
        /// <param name="scaleCenter">缩放中心点</param>
        public void ScaleAt(double scaleFactor, Point scaleCenter)
        {
            _scale *= scaleFactor;
            _scale = Math.Max(0.001, _scale);
            
            // 调整原点，使缩放中心点保持不变
            Vector offset = (Vector)(scaleCenter - _origin);
            offset *= (scaleFactor - 1.0);
            _origin = Point.Add(_origin, -offset);
            
            UpdateTransformMatrix();
        }

        /// <summary>
        /// 应用旋转
        /// </summary>
        /// <param name="angle">旋转角度（度）</param>
        /// <param name="rotationCenter">旋转中心点</param>
        public void RotateAt(double angle, Point rotationCenter)
        {
            _rotation += angle;
            
            // 调整原点，使旋转中心点保持不变
            Matrix rotMatrix = Matrix.Identity;
            rotMatrix.RotateAt(angle, rotationCenter.X, rotationCenter.Y);
            _origin = rotMatrix.Transform(_origin);
            
            UpdateTransformMatrix();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 更新变换矩阵
        /// </summary>
        private void UpdateTransformMatrix()
        {
            _transformMatrix = Matrix.Identity;
            
            // 应用Y轴翻转
            if (_yAxisUp)
            {
                _transformMatrix.Scale(1, -1);
            }
            
            // 应用缩放
            _transformMatrix.Scale(_scale, _scale);
            
            // 应用旋转
            if (Math.Abs(_rotation) > 0.001)
            {
                _transformMatrix.Rotate(_rotation);
            }
            
            // 应用平移
            _transformMatrix.Translate(_origin.X, _origin.Y);
            
            // 计算逆矩阵
            _inverseTransformMatrix = _transformMatrix;
            _inverseTransformMatrix.Invert();
        }

        #endregion

        #region 字段

        private Point _origin;
        private double _scale;
        private double _rotation;
        private bool _yAxisUp;
        private double _physicalToLogicalRatio;
        private Point _physicalOriginInImage;
        private double _pixelToPhysicalRatio;
        private Matrix _transformMatrix;
        private Matrix _inverseTransformMatrix;

        #endregion
    }
}