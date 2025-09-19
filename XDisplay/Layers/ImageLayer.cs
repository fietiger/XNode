using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XDisplay.Layers
{
    /// <summary>
    /// 图像显示图层 - 负责高效显示和管理图像
    /// </summary>
    public class ImageLayer : LayerBase
    {
        #region 属性

        /// <summary>图像列表</summary>
        public List<ImageItem> Images { get; } = new List<ImageItem>();

        #endregion

        #region 事件

        /// <summary>图像内容改变时触发</summary>
        public event EventHandler? ImageContentChanged;

        #endregion

        #region 公开方法

        /// <summary>
        /// 添加图像
        /// </summary>
        /// <param name="image">图像源</param>
        /// <param name="worldPosition">世界坐标位置</param>
        /// <param name="worldSize">世界坐标尺寸（可选，null表示使用原始尺寸）</param>
        /// <returns>图像项ID</returns>
        public Guid AddImage(ImageSource image, Point worldPosition, Size? worldSize = null)
        {
            var imageItem = new ImageItem
            {
                Id = Guid.NewGuid(),
                Image = image,
                WorldPosition = worldPosition,
                WorldSize = worldSize ?? new Size(image.Width, image.Height),
                IsVisible = true
            };

            Images.Add(imageItem);
            Update();
            OnImageContentChanged();
            return imageItem.Id;
        }

        /// <summary>
        /// 移除图像
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveImage(Guid imageId)
        {
            var imageItem = Images.Find(img => img.Id == imageId);
            if (imageItem != null)
            {
                Images.Remove(imageItem);
                Update();
                OnImageContentChanged();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有图像
        /// </summary>
        public void ClearImages()
        {
            bool hadImages = Images.Count > 0;
            Images.Clear();
            Clear();
            if (hadImages)
            {
                OnImageContentChanged();
            }
        }

        /// <summary>
        /// 设置图像可见性
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <param name="visible">是否可见</param>
        public void SetImageVisible(Guid imageId, bool visible)
        {
            var imageItem = Images.Find(img => img.Id == imageId);
            if (imageItem != null && imageItem.IsVisible != visible)
            {
                imageItem.IsVisible = visible;
                Update();
                OnImageContentChanged();
            }
        }

        /// <summary>
        /// 更新图像位置
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <param name="worldPosition">新的世界坐标位置</param>
        public void UpdateImagePosition(Guid imageId, Point worldPosition)
        {
            var imageItem = Images.Find(img => img.Id == imageId);
            if (imageItem != null)
            {
                imageItem.WorldPosition = worldPosition;
                Update();
                OnImageContentChanged();
            }
        }

        /// <summary>
        /// 更新图像尺寸
        /// </summary>
        /// <param name="imageId">图像ID</param>
        /// <param name="worldSize">新的世界坐标尺寸</param>
        public void UpdateImageSize(Guid imageId, Size worldSize)
        {
            var imageItem = Images.Find(img => img.Id == imageId);
            if (imageItem != null)
            {
                imageItem.WorldSize = worldSize;
                Update();
                OnImageContentChanged();
            }
        }

        #endregion

        #region 保护方法

        protected override void OnRender(DrawingContext context)
        {
            if (Images.Count == 0) return;

            // 获取视口边界以进行视口剪裁优化
            Rect viewportBounds = new Rect(0, 0, ViewportWidth, ViewportHeight);

            foreach (var imageItem in Images)
            {
                if (!imageItem.IsVisible || imageItem.Image == null) continue;

                // 将世界坐标转换为屏幕坐标
                Rect screenRect = WorldToScreen(new Rect(imageItem.WorldPosition, imageItem.WorldSize));

                // 视口剪裁：只绘制可见的图像
                if (!IsRectVisible(screenRect)) continue;

                // 绘制图像
                context.DrawImage(imageItem.Image, screenRect);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 触发图像内容改变事件
        /// </summary>
        private void OnImageContentChanged()
        {
            ImageContentChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }

    /// <summary>
    /// 图像项
    /// </summary>
    public class ImageItem
    {
        /// <summary>唯一标识</summary>
        public Guid Id { get; set; }

        /// <summary>图像源</summary>
        public ImageSource? Image { get; set; }

        /// <summary>世界坐标位置</summary>
        public Point WorldPosition { get; set; }

        /// <summary>世界坐标尺寸</summary>
        public Size WorldSize { get; set; }

        /// <summary>是否可见</summary>
        public bool IsVisible { get; set; }

        /// <summary>透明度（0.0-1.0）</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Z序（在同一图层内的排序）</summary>
        public int ZIndex { get; set; } = 0;
    }
}