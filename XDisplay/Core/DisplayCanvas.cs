using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Core
{
    /// <summary>
    /// 显示画布 - 高效的多图层绘制容器
    /// 基于XNode的DrawingBoard架构设计，支持多个可视元素的高效管理
    /// </summary>
    public class DisplayCanvas : FrameworkElement
    {
        #region 构造函数
        
        public DisplayCanvas()
        {
            // 为了确保能接收鼠标事件，继续使用原有的方式
            // FrameworkElement 默认就能接收事件，不需要特殊设置
        }
        
        #endregion
        
        #region FrameworkElement 属性、方法

        protected override int VisualChildrenCount => _visualElements.Count;

        protected override Visual GetVisualChild(int index) => _visualElements[index];

        #endregion

        #region 公开方法

        /// <summary>
        /// 添加可视元素
        /// </summary>
        public void AddVisualElement(DrawingVisual element)
        {
            _visualElements.Add(element);
            AddVisualChild(element);
            AddLogicalChild(element);
        }

        /// <summary>
        /// 移除可视元素
        /// </summary>
        public void RemoveVisualElement(DrawingVisual element)
        {
            if (_hitedElement != null && _hitedElement == element) 
                _hitedElement = null;

            _visualElements.Remove(element);
            RemoveVisualChild(element);
            RemoveLogicalChild(element);
        }

        /// <summary>
        /// 清空所有可视元素
        /// </summary>
        public void ClearVisualElements()
        {
            foreach (var element in _visualElements)
            {
                RemoveVisualChild(element);
                RemoveLogicalChild(element);
            }

            _hitedElement = null;
            _visualElements.Clear();
        }

        /// <summary>
        /// 获取命中的可视元素（高效的几何命中检测）
        /// </summary>
        /// <param name="point">检测点</param>
        /// <param name="tolerance">容差半径（像素）</param>
        /// <returns>命中的可视元素，未命中返回null</returns>
        public DrawingVisual? GetHitVisualElement(Point point, double tolerance = 8.0)
        {
            // 置空命中元素
            _hitedElement = null;
            
            // 创建圆形命中检测区域
            var hitGeometry = new EllipseGeometry(point, tolerance, tolerance);
            var parameters = new GeometryHitTestParameters(hitGeometry);
            
            // 执行命中检测
            VisualTreeHelper.HitTest(this, null, HitTestCallback, parameters);
            
            return _hitedElement;
        }

        /// <summary>
        /// 获取指定矩形区域内的所有可视元素
        /// </summary>
        /// <param name="rect">检测矩形区域</param>
        /// <returns>命中的可视元素列表</returns>
        public List<DrawingVisual> GetHitVisualElements(Rect rect)
        {
            _hitElements.Clear();
            
            var hitGeometry = new RectangleGeometry(rect);
            var parameters = new GeometryHitTestParameters(hitGeometry);
            
            VisualTreeHelper.HitTest(this, null, HitTestMultipleCallback, parameters);
            
            var result = new List<DrawingVisual>(_hitElements);
            _hitElements.Clear();
            return result;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 单个命中测试回调
        /// </summary>
        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            if (result.VisualHit is DrawingVisual visual)
            {
                _hitedElement = visual;
                return HitTestResultBehavior.Stop;
            }
            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// 多个命中测试回调
        /// </summary>
        private HitTestResultBehavior HitTestMultipleCallback(HitTestResult result)
        {
            if (result.VisualHit is DrawingVisual visual)
            {
                _hitElements.Add(visual);
            }
            return HitTestResultBehavior.Continue;
        }

        #endregion

        #region 字段

        /// <summary>可视元素列表</summary>
        private readonly List<DrawingVisual> _visualElements = new List<DrawingVisual>();
        
        /// <summary>单次命中的元素</summary>
        private DrawingVisual? _hitedElement = null;
        
        /// <summary>多次命中的元素列表</summary>
        private readonly List<DrawingVisual> _hitElements = new List<DrawingVisual>();

        #endregion
    }
}