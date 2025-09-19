using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace XDisplay.Geometry.Containers
{
    /// <summary>
    /// 分层坐标系管理器 - 支持多层嵌套的用户定义坐标系
    /// 实现从任意坐标系到任意坐标系的坐标转换
    /// </summary>
    public class HierarchicalCoordinateSystem
    {
        #region 属性

        /// <summary>坐标系名称</summary>
        public string Name { get; }

        /// <summary>坐标系描述</summary>
        public string Description { get; set; }

        /// <summary>父坐标系</summary>
        public HierarchicalCoordinateSystem? Parent
        {
            get => _parent;
            set
            {
                if (_parent != value)
                {
                    // 从旧父级移除
                    _parent?._children.Remove(this);
                    
                    _parent = value;
                    
                    // 添加到新父级
                    _parent?._children.Add(this);
                }
            }
        }

        /// <summary>子坐标系列表</summary>
        public IReadOnlyList<HierarchicalCoordinateSystem> Children => _children.AsReadOnly();

        /// <summary>局部坐标系变换</summary>
        public LocalCoordinateSystem LocalCoordinateSystem { get; }

        /// <summary>是否为根坐标系</summary>
        public bool IsRoot => _parent == null;

        /// <summary>坐标系层级（根为0）</summary>
        public int Level => IsRoot ? 0 : _parent!.Level + 1;

        /// <summary>坐标系完整路径（从根到当前）</summary>
        public string FullPath
        {
            get
            {
                if (IsRoot) return Name;
                return $"{_parent!.FullPath}/{Name}";
            }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 创建根坐标系
        /// </summary>
        /// <param name="name">坐标系名称</param>
        /// <param name="description">坐标系描述</param>
        public HierarchicalCoordinateSystem(string name, string description = "")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description;
            LocalCoordinateSystem = new LocalCoordinateSystem();
            _children = new List<HierarchicalCoordinateSystem>();
        }

        /// <summary>
        /// 创建子坐标系
        /// </summary>
        /// <param name="name">坐标系名称</param>
        /// <param name="parent">父坐标系</param>
        /// <param name="description">坐标系描述</param>
        public HierarchicalCoordinateSystem(string name, HierarchicalCoordinateSystem parent, string description = "")
            : this(name, description)
        {
            Parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        /// <summary>
        /// 创建带物理映射的子坐标系
        /// </summary>
        /// <param name="name">坐标系名称</param>
        /// <param name="parent">父坐标系</param>
        /// <param name="origin">原点位置（相对于父坐标系）</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="rotation">旋转角度（度）</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        /// <param name="description">坐标系描述</param>
        public HierarchicalCoordinateSystem(string name, HierarchicalCoordinateSystem parent,
            Point origin, double scale = 1.0, double rotation = 0.0, bool yAxisUp = false, string description = "")
            : this(name, parent, description)
        {
            LocalCoordinateSystem.Origin = origin;
            LocalCoordinateSystem.Scale = scale;
            LocalCoordinateSystem.Rotation = rotation;
            LocalCoordinateSystem.YAxisUp = yAxisUp;
        }

        #endregion

        #region 坐标系管理

        /// <summary>
        /// 添加子坐标系
        /// </summary>
        /// <param name="child">子坐标系</param>
        public void AddChild(HierarchicalCoordinateSystem child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            child.Parent = this;
        }

        /// <summary>
        /// 移除子坐标系
        /// </summary>
        /// <param name="child">子坐标系</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveChild(HierarchicalCoordinateSystem child)
        {
            if (child == null || child.Parent != this) return false;
            child.Parent = null;
            return true;
        }

        /// <summary>
        /// 按名称查找直接子坐标系
        /// </summary>
        /// <param name="name">子坐标系名称</param>
        /// <returns>找到的子坐标系，未找到返回null</returns>
        public HierarchicalCoordinateSystem? FindChild(string name)
        {
            return _children.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// 递归查找子坐标系（深度优先搜索）
        /// </summary>
        /// <param name="name">坐标系名称</param>
        /// <returns>找到的坐标系，未找到返回null</returns>
        public HierarchicalCoordinateSystem? FindDescendant(string name)
        {
            // 首先查找直接子级
            var directChild = FindChild(name);
            if (directChild != null) return directChild;

            // 递归查找间接子级
            foreach (var child in _children)
            {
                var descendant = child.FindDescendant(name);
                if (descendant != null) return descendant;
            }

            return null;
        }

        /// <summary>
        /// 通过完整路径查找坐标系
        /// </summary>
        /// <param name="path">完整路径，如"世界坐标系/轨道1/载具A/电路板1"</param>
        /// <returns>找到的坐标系，未找到返回null</returns>
        public HierarchicalCoordinateSystem? FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return null;

            // 从根节点开始查找
            var root = GetRoot();
            if (root.Name != parts[0]) return null;

            var current = root;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.FindChild(parts[i]);
                if (current == null) return null;
            }

            return current;
        }

        /// <summary>
        /// 获取根坐标系
        /// </summary>
        /// <returns>根坐标系</returns>
        public HierarchicalCoordinateSystem GetRoot()
        {
            var current = this;
            while (current.Parent != null)
            {
                current = current.Parent;
            }
            return current;
        }

        /// <summary>
        /// 获取从根到当前坐标系的路径
        /// </summary>
        /// <returns>坐标系路径列表</returns>
        public List<HierarchicalCoordinateSystem> GetPathFromRoot()
        {
            var path = new List<HierarchicalCoordinateSystem>();
            var current = this;
            
            while (current != null)
            {
                path.Insert(0, current);
                current = current.Parent;
            }
            
            return path;
        }

        #endregion

        #region 坐标转换

        /// <summary>
        /// 转换到父坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <returns>父坐标系中的点</returns>
        public Point ToParent(Point localPoint)
        {
            return LocalCoordinateSystem.LocalToParent(localPoint);
        }

        /// <summary>
        /// 从父坐标系转换
        /// </summary>
        /// <param name="parentPoint">父坐标系中的点</param>
        /// <returns>本地坐标点</returns>
        public Point FromParent(Point parentPoint)
        {
            return LocalCoordinateSystem.ParentToLocal(parentPoint);
        }

        /// <summary>
        /// 转换到根坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <returns>根坐标系中的点</returns>
        public Point ToRoot(Point localPoint)
        {
            Point currentPoint = localPoint;
            var current = this;

            while (current.Parent != null)
            {
                currentPoint = current.ToParent(currentPoint);
                current = current.Parent;
            }

            return currentPoint;
        }

        /// <summary>
        /// 从根坐标系转换
        /// </summary>
        /// <param name="rootPoint">根坐标系中的点</param>
        /// <returns>本地坐标点</returns>
        public Point FromRoot(Point rootPoint)
        {
            var path = GetPathFromRoot();
            Point currentPoint = rootPoint;

            // 从根节点开始，逐级转换到本地坐标系
            for (int i = 1; i < path.Count; i++)
            {
                currentPoint = path[i].FromParent(currentPoint);
            }

            return currentPoint;
        }

        /// <summary>
        /// 转换到指定坐标系
        /// </summary>
        /// <param name="localPoint">本地坐标点</param>
        /// <param name="targetCoordinateSystem">目标坐标系</param>
        /// <returns>目标坐标系中的点</returns>
        public Point TransformTo(Point localPoint, HierarchicalCoordinateSystem targetCoordinateSystem)
        {
            if (targetCoordinateSystem == null) throw new ArgumentNullException(nameof(targetCoordinateSystem));
            
            // 先转换到根坐标系
            Point rootPoint = ToRoot(localPoint);
            
            // 再从根坐标系转换到目标坐标系
            return targetCoordinateSystem.FromRoot(rootPoint);
        }

        /// <summary>
        /// 从指定坐标系转换
        /// </summary>
        /// <param name="sourcePoint">源坐标系中的点</param>
        /// <param name="sourceCoordinateSystem">源坐标系</param>
        /// <returns>本地坐标点</returns>
        public Point TransformFrom(Point sourcePoint, HierarchicalCoordinateSystem sourceCoordinateSystem)
        {
            if (sourceCoordinateSystem == null) throw new ArgumentNullException(nameof(sourceCoordinateSystem));
            
            return sourceCoordinateSystem.TransformTo(sourcePoint, this);
        }

        #endregion

        #region 坐标系变换操作

        /// <summary>
        /// 设置相对于父坐标系的变换
        /// </summary>
        /// <param name="origin">原点位置</param>
        /// <param name="scale">缩放比例</param>
        /// <param name="rotation">旋转角度（度）</param>
        public void SetTransform(Point origin, double scale = 1.0, double rotation = 0.0)
        {
            LocalCoordinateSystem.SetTransform(origin, scale, rotation);
        }

        /// <summary>
        /// 设置物理坐标映射参数
        /// </summary>
        /// <param name="pixelToPhysicalRatio">像素到物理单位比例</param>
        /// <param name="physicalOriginInImage">物理原点在图像中的位置</param>
        /// <param name="yAxisUp">Y轴是否向上</param>
        public void SetPhysicalMapping(double pixelToPhysicalRatio, Point physicalOriginInImage, bool yAxisUp = true)
        {
            LocalCoordinateSystem.PixelToPhysicalRatio = pixelToPhysicalRatio;
            LocalCoordinateSystem.PhysicalOriginInImage = physicalOriginInImage;
            LocalCoordinateSystem.YAxisUp = yAxisUp;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取所有子孙坐标系（深度优先遍历）
        /// </summary>
        /// <returns>所有子孙坐标系列表</returns>
        public List<HierarchicalCoordinateSystem> GetAllDescendants()
        {
            var descendants = new List<HierarchicalCoordinateSystem>();
            
            foreach (var child in _children)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetAllDescendants());
            }
            
            return descendants;
        }

        /// <summary>
        /// 打印坐标系树结构
        /// </summary>
        /// <param name="indent">缩进级别</param>
        /// <returns>树结构字符串</returns>
        public string PrintTree(int indent = 0)
        {
            var indentStr = new string(' ', indent * 2);
            var result = $"{indentStr}{Name}";
            if (!string.IsNullOrEmpty(Description))
            {
                result += $" ({Description})";
            }
            result += Environment.NewLine;

            foreach (var child in _children)
            {
                result += child.PrintTree(indent + 1);
            }

            return result;
        }

        #endregion

        #region 重写方法

        public override string ToString()
        {
            return $"{Name} [{FullPath}]";
        }

        #endregion

        #region 字段

        private HierarchicalCoordinateSystem? _parent;
        private readonly List<HierarchicalCoordinateSystem> _children;

        #endregion
    }
}