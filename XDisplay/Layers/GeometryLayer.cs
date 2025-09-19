using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XDisplay.Geometry;

namespace XDisplay.Layers
{
    /// <summary>
    /// 几何图形图层 - 负责高效渲染和管理几何图形
    /// </summary>
    public class GeometryLayer : LayerBase
    {
        #region 属性

        /// <summary>几何图形列表</summary>
        public List<IGeometry> Geometries { get; } = new List<IGeometry>();

        /// <summary>选中的几何图形列表</summary>
        public List<IGeometry> SelectedGeometries => Geometries.Where(g => g.IsSelected).ToList();

        #endregion

        #region 事件

        /// <summary>几何图形被选中时触发</summary>
        public event EventHandler<GeometrySelectionChangedEventArgs>? SelectionChanged;

        #endregion

        #region 公开方法

        /// <summary>
        /// 添加几何图形
        /// </summary>
        /// <param name="geometry">几何图形</param>
        public void AddGeometry(IGeometry geometry)
        {
            if (geometry == null) return;

            Geometries.Add(geometry);
            geometry.GeometryChanged += OnGeometryChanged;
            Update();
        }

        /// <summary>
        /// 移除几何图形
        /// </summary>
        /// <param name="geometry">几何图形</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGeometry(IGeometry geometry)
        {
            if (geometry == null) return false;

            bool removed = Geometries.Remove(geometry);
            if (removed)
            {
                geometry.GeometryChanged -= OnGeometryChanged;
                Update();
            }
            return removed;
        }

        /// <summary>
        /// 根据ID移除几何图形
        /// </summary>
        /// <param name="geometryId">几何图形ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveGeometry(Guid geometryId)
        {
            var geometry = Geometries.FirstOrDefault(g => g.Id == geometryId);
            return geometry != null && RemoveGeometry(geometry);
        }

        /// <summary>
        /// 清空所有几何图形
        /// </summary>
        public void ClearGeometries()
        {
            foreach (var geometry in Geometries)
            {
                geometry.GeometryChanged -= OnGeometryChanged;
            }

            Geometries.Clear();
            Clear();
            OnSelectionChanged();
        }

        /// <summary>
        /// 根据ID查找几何图形
        /// </summary>
        /// <param name="geometryId">几何图形ID</param>
        /// <returns>找到的几何图形，未找到返回null</returns>
        public IGeometry? FindGeometry(Guid geometryId)
        {
            return Geometries.FirstOrDefault(g => g.Id == geometryId);
        }

        /// <summary>
        /// 命中测试 - 查找指定点命中的几何图形
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <param name="tolerance">容差</param>
        /// <returns>命中的几何图形（按Z序排序，最上层优先）</returns>
        public IGeometry? HitTest(Point worldPoint, double tolerance = 1.0)
        {
            // 按Z序倒序遍历（最上层优先）
            var sortedGeometries = Geometries
                .Where(g => g.IsVisible)
                .OrderByDescending(g => g.ZOrder);

            foreach (var geometry in sortedGeometries)
            {
                if (geometry.HitTest(worldPoint, tolerance))
                {
                    return geometry;
                }
            }

            return null;
        }

        /// <summary>
        /// 命中测试 - 查找指定点命中的所有几何图形
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <param name="tolerance">容差</param>
        /// <returns>命中的所有几何图形列表（按Z序排序，最上层优先）</returns>
        public List<IGeometry> HitTestAll(Point worldPoint, double tolerance = 1.0)
        {
            // 按Z序倒序遍历（最上层优先）
            var sortedGeometries = Geometries
                .Where(g => g.IsVisible)
                .OrderByDescending(g => g.ZOrder);

            var hitGeometries = new List<IGeometry>();
            foreach (var geometry in sortedGeometries)
            {
                if (geometry.HitTest(worldPoint, tolerance))
                {
                    hitGeometries.Add(geometry);
                }
            }

            return hitGeometries;
        }

        /// <summary>
        /// 循环选择 - 在指定点的重叠图形之间循环选择
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <param name="currentSelected">当前选中的几何图形（如果有）</param>
        /// <param name="tolerance">容差</param>
        /// <returns>下一个要选择的几何图形，如果没有则返回null</returns>
        public IGeometry? CyclicHitTest(Point worldPoint, IGeometry? currentSelected = null, double tolerance = 1.0)
        {
            var hitGeometries = HitTestAll(worldPoint, tolerance);
            if (hitGeometries.Count == 0) return null;
            if (hitGeometries.Count == 1) return hitGeometries[0];

            // 如果当前没有选中的图形，或者选中的图形不在命中列表中，选择第一个
            if (currentSelected == null || !hitGeometries.Contains(currentSelected))
            {
                return hitGeometries[0];
            }

            // 找到当前选中图形在列表中的索引，选择下一个
            int currentIndex = hitGeometries.IndexOf(currentSelected);
            int nextIndex = (currentIndex + 1) % hitGeometries.Count;
            return hitGeometries[nextIndex];
        }

        /// <summary>
        /// 矩形区域命中测试 - 查找与指定矩形相交的几何图形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>相交的几何图形列表</returns>
        public List<IGeometry> HitTest(Rect worldRect)
        {
            return Geometries
                .Where(g => g.IsVisible && g.HitTest(worldRect))
                .OrderBy(g => g.ZOrder)
                .ToList();
        }

        /// <summary>
        /// 选择几何图形
        /// </summary>
        /// <param name="geometry">要选择的几何图形</param>
        /// <param name="multiSelect">是否多选模式</param>
        public void SelectGeometry(IGeometry geometry, bool multiSelect = false)
        {
            if (geometry == null) return;

            if (!multiSelect)
            {
                // 单选模式：取消其他选择
                foreach (var g in Geometries.Where(g => g.IsSelected && g != geometry))
                {
                    g.IsSelected = false;
                }
            }

            geometry.IsSelected = true;
            Update();
            OnSelectionChanged();
        }

        /// <summary>
        /// 取消选择几何图形
        /// </summary>
        /// <param name="geometry">要取消选择的几何图形</param>
        public void DeselectGeometry(IGeometry geometry)
        {
            if (geometry == null) return;

            geometry.IsSelected = false;
            Update();
            OnSelectionChanged();
        }

        /// <summary>
        /// 清空所有选择
        /// </summary>
        public void ClearSelection()
        {
            bool hasSelection = false;
            foreach (var geometry in Geometries.Where(g => g.IsSelected))
            {
                geometry.IsSelected = false;
                hasSelection = true;
            }

            if (hasSelection)
            {
                Update();
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// 选择矩形区域内的所有几何图形
        /// </summary>
        /// <param name="worldRect">选择矩形</param>
        /// <param name="multiSelect">是否多选模式</param>
        public void SelectGeometriesInRect(Rect worldRect, bool multiSelect = false)
        {
            var hitGeometries = HitTest(worldRect);

            if (!multiSelect)
            {
                ClearSelection();
            }

            foreach (var geometry in hitGeometries)
            {
                geometry.IsSelected = true;
            }

            if (hitGeometries.Count > 0)
            {
                Update();
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// 删除选中的几何图形
        /// </summary>
        public void DeleteSelected()
        {
            var selectedGeometries = SelectedGeometries.ToList();
            foreach (var geometry in selectedGeometries)
            {
                RemoveGeometry(geometry);
            }
        }

        /// <summary>
        /// 移动选中的几何图形
        /// </summary>
        /// <param name="deltaX">X轴移动量</param>
        /// <param name="deltaY">Y轴移动量</param>
        public void MoveSelected(double deltaX, double deltaY)
        {
            foreach (var geometry in SelectedGeometries)
            {
                geometry.Move(deltaX, deltaY);
            }
            Update();
        }

        #endregion

        #region 保护方法

        protected override void OnRender(DrawingContext context)
        {
            if (Geometries.Count == 0) return;

            // 按Z序排序绘制
            var sortedGeometries = Geometries
                .Where(g => g.IsVisible)
                .OrderBy(g => g.ZOrder);

            foreach (var geometry in sortedGeometries)
            {
                // 视口剪裁优化：只绘制可见区域内的几何图形
                Rect geometryScreenBounds = WorldToScreen(geometry.Bounds);
                if (IsRectVisible(geometryScreenBounds))
                {
                    geometry.Render(context, Transform);
                }
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 几何图形属性改变时的处理
        /// </summary>
        private void OnGeometryChanged(object? sender, GeometryChangedEventArgs e)
        {
            Update();

            if (e.ChangeType == GeometryChangeType.Selection)
            {
                OnSelectionChanged();
            }
        }

        /// <summary>
        /// 触发选择改变事件
        /// </summary>
        private void OnSelectionChanged()
        {
            var selectedGeometries = SelectedGeometries;
            SelectionChanged?.Invoke(this, new GeometrySelectionChangedEventArgs(selectedGeometries));
        }

        #endregion
    }

    /// <summary>
    /// 几何图形选择改变事件参数
    /// </summary>
    public class GeometrySelectionChangedEventArgs : EventArgs
    {
        /// <summary>当前选中的几何图形列表</summary>
        public List<IGeometry> SelectedGeometries { get; }

        /// <summary>构造函数</summary>
        public GeometrySelectionChangedEventArgs(List<IGeometry> selectedGeometries)
        {
            SelectedGeometries = selectedGeometries ?? new List<IGeometry>();
        }
    }
}