using System;
using System.Collections.Generic;
using System.Windows;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 分层几何容器接口 - 支持多层嵌套坐标系的容器
    /// 扩展了基础容器功能，添加分层坐标系管理
    /// </summary>
    public interface IHierarchicalContainer : IGeometryContainer
    {
        #region 分层坐标系属性

        /// <summary>分层坐标系</summary>
        HierarchicalCoordinateSystem HierarchicalCoordinateSystem { get; }

        /// <summary>父容器</summary>
        IHierarchicalContainer? ParentContainer { get; set; }

        /// <summary>子容器集合</summary>
        IReadOnlyList<IHierarchicalContainer> ChildContainers { get; }

        /// <summary>容器名称（用于坐标系标识）</summary>
        string ContainerName { get; set; }

        /// <summary>容器描述</summary>
        string ContainerDescription { get; set; }

        /// <summary>是否为根容器</summary>
        bool IsRootContainer { get; }

        /// <summary>容器层级深度</summary>
        int ContainerLevel { get; }

        /// <summary>完整容器路径</summary>
        string FullContainerPath { get; }

        #endregion

        #region 分层容器管理

        /// <summary>
        /// 添加子容器
        /// </summary>
        /// <param name="childContainer">子容器</param>
        /// <param name="localPosition">在本地坐标系中的位置</param>
        void AddChildContainer(IHierarchicalContainer childContainer, Point localPosition);

        /// <summary>
        /// 移除子容器
        /// </summary>
        /// <param name="childContainer">要移除的子容器</param>
        /// <returns>是否成功移除</returns>
        bool RemoveChildContainer(IHierarchicalContainer childContainer);

        /// <summary>
        /// 按名称查找直接子容器
        /// </summary>
        /// <param name="containerName">容器名称</param>
        /// <returns>找到的子容器，未找到返回null</returns>
        IHierarchicalContainer? FindChildContainer(string containerName);

        /// <summary>
        /// 递归查找子容器
        /// </summary>
        /// <param name="containerName">容器名称</param>
        /// <returns>找到的容器，未找到返回null</returns>
        IHierarchicalContainer? FindDescendantContainer(string containerName);

        /// <summary>
        /// 通过路径查找容器
        /// </summary>
        /// <param name="path">容器路径，如"世界/轨道1/载具A"</param>
        /// <returns>找到的容器，未找到返回null</returns>
        IHierarchicalContainer? FindContainerByPath(string path);

        /// <summary>
        /// 获取根容器
        /// </summary>
        /// <returns>根容器</returns>
        IHierarchicalContainer GetRootContainer();

        #endregion

        #region 分层坐标转换

        /// <summary>
        /// 转换到根坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <returns>根坐标系中的点</returns>
        Point TransformToRoot(Point localPoint);

        /// <summary>
        /// 从根坐标系转换
        /// </summary>
        /// <param name="rootPoint">根坐标系中的点</param>
        /// <returns>本地坐标点</returns>
        Point TransformFromRoot(Point rootPoint);

        /// <summary>
        /// 转换到指定容器的坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <param name="targetContainer">目标容器</param>
        /// <returns>目标容器坐标系中的点</returns>
        Point TransformToContainer(Point localPoint, IHierarchicalContainer targetContainer);

        /// <summary>
        /// 从指定容器的坐标系转换
        /// </summary>
        /// <param name="sourcePoint">源容器坐标系中的点</param>
        /// <param name="sourceContainer">源容器</param>
        /// <returns>本地坐标点</returns>
        Point TransformFromContainer(Point sourcePoint, IHierarchicalContainer sourceContainer);

        /// <summary>
        /// 转换矩形到根坐标系
        /// </summary>
        /// <param name="localRect">本地坐标矩形</param>
        /// <returns>根坐标系中的矩形</returns>
        Rect TransformRectToRoot(Rect localRect);

        /// <summary>
        /// 从根坐标系转换矩形
        /// </summary>
        /// <param name="rootRect">根坐标系中的矩形</param>
        /// <returns>本地坐标矩形</returns>
        Rect TransformRectFromRoot(Rect rootRect);

        #endregion

        #region 分层容器变换

        /// <summary>
        /// 设置相对于父容器的变换
        /// </summary>
        /// <param name="origin">原点位置</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="rotation">旋转角度（度）</param>
        void SetContainerTransform(Point origin, double scale = 1.0, double rotation = 0.0);

        /// <summary>
        /// 设置物理坐标映射
        /// </summary>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        void SetPhysicalCoordinateMapping(double pixelToPhysicalRatio, Point physicalOriginInImage, bool yAxisUp = true);

        #endregion

        #region 分层容器事件

        /// <summary>子容器添加时触发</summary>
        event EventHandler<ChildContainerEventArgs>? ChildContainerAdded;

        /// <summary>子容器移除时触发</summary>
        event EventHandler<ChildContainerEventArgs>? ChildContainerRemoved;

        /// <summary>容器变换改变时触发</summary>
        event EventHandler<ContainerTransformChangedEventArgs>? ContainerTransformChanged;

        #endregion
    }

    /// <summary>
    /// 子容器事件参数
    /// </summary>
    public class ChildContainerEventArgs : EventArgs
    {
        /// <summary>父容器</summary>
        public IHierarchicalContainer ParentContainer { get; }

        /// <summary>子容器</summary>
        public IHierarchicalContainer ChildContainer { get; }

        /// <summary>构造函数</summary>
        public ChildContainerEventArgs(IHierarchicalContainer parentContainer, IHierarchicalContainer childContainer)
        {
            ParentContainer = parentContainer;
            ChildContainer = childContainer;
        }
    }

    /// <summary>
    /// 容器变换改变事件参数
    /// </summary>
    public class ContainerTransformChangedEventArgs : EventArgs
    {
        /// <summary>容器</summary>
        public IHierarchicalContainer Container { get; }

        /// <summary>变换类型</summary>
        public ContainerTransformType TransformType { get; }

        /// <summary>构造函数</summary>
        public ContainerTransformChangedEventArgs(IHierarchicalContainer container, ContainerTransformType transformType)
        {
            Container = container;
            TransformType = transformType;
        }
    }

    /// <summary>
    /// 容器变换类型
    /// </summary>
    public enum ContainerTransformType
    {
        /// <summary>位置变换</summary>
        Position,
        /// <summary>缩放变换</summary>
        Scale,
        /// <summary>旋转变换</summary>
        Rotation,
        /// <summary>复合变换</summary>
        Combined
    }
}