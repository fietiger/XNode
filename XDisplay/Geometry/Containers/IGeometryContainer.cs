using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 几何容器接口 - 支持包含子几何图形的容器
    /// 提供层次化管理和局部坐标系支持
    /// </summary>
    public interface IGeometryContainer : IGeometry
    {
        #region 容器属性

        /// <summary>子几何图形集合</summary>
        IList<IGeometry> Children { get; }

        /// <summary>局部坐标系</summary>
        LocalCoordinateSystem LocalCoordinateSystem { get; }

        /// <summary>是否裁剪子元素到容器边界</summary>
        bool ClipChildren { get; set; }

        #endregion

        #region 子元素管理

        /// <summary>
        /// 添加子几何图形
        /// </summary>
        /// <param name="child">子几何图形</param>
        /// <param name="localPosition">在容器本地坐标系中的位置</param>
        void AddChild(IGeometry child, Point localPosition);

        /// <summary>
        /// 移除子几何图形
        /// </summary>
        /// <param name="child">要移除的子几何图形</param>
        /// <returns>是否成功移除</returns>
        bool RemoveChild(IGeometry child);

        /// <summary>
        /// 清空所有子几何图形
        /// </summary>
        void ClearChildren();

        /// <summary>
        /// 根据ID查找子几何图形
        /// </summary>
        /// <param name="id">几何图形ID</param>
        /// <returns>找到的几何图形，未找到返回null</returns>
        IGeometry? FindChild(Guid id);

        #endregion

        #region 坐标转换

        /// <summary>
        /// 本地坐标转世界坐标
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <returns>世界坐标点</returns>
        Point LocalToWorld(Point localPoint);

        /// <summary>
        /// 世界坐标转本地坐标
        /// </summary>
        /// <param name="worldPoint">世界坐标点</param>
        /// <returns>本地坐标点</returns>
        Point WorldToLocal(Point worldPoint);

        /// <summary>
        /// 本地矩形转世界矩形
        /// </summary>
        /// <param name="localRect">本地坐标矩形</param>
        /// <returns>世界坐标矩形</returns>
        Rect LocalToWorld(Rect localRect);

        /// <summary>
        /// 世界矩形转本地矩形
        /// </summary>
        /// <param name="worldRect">世界坐标矩形</param>
        /// <returns>本地坐标矩形</returns>
        Rect WorldToLocal(Rect worldRect);

        #endregion

        #region 事件

        /// <summary>子元素添加时触发</summary>
        event EventHandler<ChildGeometryEventArgs>? ChildAdded;

        /// <summary>子元素移除时触发</summary>
        event EventHandler<ChildGeometryEventArgs>? ChildRemoved;

        #endregion
    }

    /// <summary>
    /// 子几何图形事件参数
    /// </summary>
    public class ChildGeometryEventArgs : EventArgs
    {
        /// <summary>容器</summary>
        public IGeometryContainer Container { get; }

        /// <summary>子几何图形</summary>
        public IGeometry Child { get; }

        /// <summary>构造函数</summary>
        public ChildGeometryEventArgs(IGeometryContainer container, IGeometry child)
        {
            Container = container;
            Child = child;
        }
    }
}