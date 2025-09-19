using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using XDisplay.Core;
using XDisplay.Geometry;
using XDisplay.Geometry.Containers;
using XDisplay.Layers;
using XDisplayRect = XDisplay.Geometry.Shapes.RectangleGeometry;
using XDisplayLine = XDisplay.Geometry.Shapes.LineGeometry;
using XDisplay.Geometry.Shapes;

namespace XDisplay.Test
{
    public partial class MainWindow : Window
    {
        // 添加坐标系管理字段
        private List<HierarchicalImageContainer> _coordinateSystemContainers = new List<HierarchicalImageContainer>();
        private HierarchicalImageContainer? _currentActiveCoordinateSystem = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeDisplay();
            CreateSampleGeometries();
        }

        private void InitializeDisplay()
        {
            DisplayControl.CurrentMode = InteractionMode.Select;
            UpdateModeButtons();
            UpdateStatusBar();
            
            // 监听鼠标坐标改变事件
            DisplayControl.MouseCoordinateChanged += DisplayControl_MouseCoordinateChanged;
            
            // 默认使用屏幕坐标系
            DisplayControl.SetCoordinateSystem(yAxisUp: false);
            UpdateCoordinateSystemButtons(false);
        }

        private void CreateSampleGeometries()
        {
            var rect1 = new XDisplayRect(new Rect(100, 100, 100, 80))
            {
                Fill = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Blue, 2),
                ZOrder = 1
            };
            DisplayControl.AddGeometry(rect1);

            var circle1 = new CircleGeometry(new Point(350, 150), 40)
            {
                Fill = new SolidColorBrush(Colors.LightYellow) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Orange, 2),
                ZOrder = 1
            };
            DisplayControl.AddGeometry(circle1);

            CreateSampleImageContainer();
            UpdateStatusBar();
        }

        private void CreateSampleImageContainer()
        {
            var pcbContainer = ImageContainer.CreateEmpty(new Point(500, 100), new Size(200, 150));
            pcbContainer.Fill = new SolidColorBrush(Colors.DarkGreen) { Opacity = 0.3 };
            pcbContainer.Stroke = new Pen(Brushes.DarkGreen, 2);
            pcbContainer.ClipChildren = true;

            var component1 = new XDisplayRect(new Rect(0, 0, 20, 10));
            component1.Fill = new SolidColorBrush(Colors.Gold);
            component1.Stroke = new Pen(Brushes.DarkGoldenrod, 1);
            pcbContainer.AddChild(component1, new Point(-50, 30));

            DisplayControl.AddGeometry(pcbContainer);
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.Select;
            UpdateModeButtons();
        }

        private void BtnPan_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.Pan;
            UpdateModeButtons();
        }

        private void BtnDrawRect_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.DrawRectangle;
            UpdateModeButtons();
        }

        private void BtnDrawCircle_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.DrawCircle;
            UpdateModeButtons();
        }

        private void BtnDrawLine_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.DrawLine;
            UpdateModeButtons();
        }

        private void BtnDrawArrow_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.DrawArrowLine;
            UpdateModeButtons();
        }

        private void BtnDrawRotatedRect_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.CurrentMode = InteractionMode.DrawRotatedRectangle;
            UpdateModeButtons();
        }

        private void BtnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择图像文件",
                Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                    Point position = new Point(0, 0);
                    
                    if (DisplayControl.SelectedGeometries.Count > 0)
                    {
                        var selectedBounds = DisplayControl.SelectedGeometries[0].Bounds;
                        position = new Point(selectedBounds.Right + 20, selectedBounds.Top);
                    }

                    DisplayControl.AddImage(bitmap, position);
                    DisplayControl.FitToGeometries();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("加载图像失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCreateImageContainer_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择背景图像文件",
                Filter = "图像文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*",
                Multiselect = false
            };

            ImageSource? backgroundImage = null;
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    backgroundImage = new BitmapImage(new Uri(openFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("加载图像失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            Point position = new Point(100, 100);
            
            if (DisplayControl.SelectedGeometries.Count > 0)
            {
                var selectedBounds = DisplayControl.SelectedGeometries[0].Bounds;
                position = new Point(selectedBounds.Right + 20, selectedBounds.Top);
            }

            ImageContainer container;
            if (backgroundImage != null)
            {
                container = ImageContainer.CreateFromImage(position, backgroundImage,
                    0.05, new Point(backgroundImage.Width / 2, backgroundImage.Height / 2), true);
                container.Fill = new SolidColorBrush(Colors.Transparent);
                container.Stroke = new Pen(Brushes.Blue, 2);
                container.ClipChildren = true;
                
                string message = "图像容器已创建！\n\n" +
                    "图像尺寸：" + backgroundImage.Width + " x " + backgroundImage.Height + " 像素\n" +
                    "容器尺寸：" + container.Size.Width + " x " + container.Size.Height + "\n\n" +
                    "容器尺寸自动匹配了图像尺寸！";
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                container = ImageContainer.CreateEmpty(position, new Size(300, 200));
                container.Fill = new SolidColorBrush(Colors.LightGray) { Opacity = 0.3 };
                container.Stroke = new Pen(Brushes.Gray, 2);
                container.ClipChildren = false;
                
                MessageBox.Show("空容器已创建！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DisplayControl.AddGeometry(container);
            DisplayControl.FitToGeometries();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("确定要清空所有内容吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                DisplayControl.ClearGeometries();
                DisplayControl.ClearImages();
                UpdateStatusBar();
            }
        }

        /// <summary>
        /// 演示自动全局显示功能的复杂场景
        /// </summary>
        private void CreateComplexSceneForAutoFitDemo()
        {
            // 清空现有内容
            DisplayControl.ClearGeometries();
            DisplayControl.ClearImages();

            // 创建分散在不同位置的内容，测试边界计算
            
            // 左上角区域
            var rect1 = new XDisplayRect(new Rect(-300, -200, 120, 80))
            {
                Fill = new SolidColorBrush(Colors.LightCoral) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Red, 2)
            };
            DisplayControl.AddGeometry(rect1);
            
            var circle1 = new CircleGeometry(new Point(-200, -100), 30)
            {
                Fill = new SolidColorBrush(Colors.LightBlue) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Blue, 2)
            };
            DisplayControl.AddGeometry(circle1);
            
            // 右下角区域
            var rect2 = new XDisplayRect(new Rect(400, 300, 150, 100))
            {
                Fill = new SolidColorBrush(Colors.LightYellow) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Orange, 2)
            };
            DisplayControl.AddGeometry(rect2);
            
            var line1 = new XDisplayLine(new Point(450, 350), new Point(600, 450))
            {
                Stroke = new Pen(Brushes.Purple, 3)
            };
            DisplayControl.AddGeometry(line1);
            
            // 中心区域
            var rotatedRect = new RotatedRectangleGeometry(new Rect(100, 150, 120, 180), 45)
            {
                Fill = new SolidColorBrush(Colors.LightGreen) { Opacity = 0.7 },
                Stroke = new Pen(Brushes.Green, 2)
            };
            DisplayControl.AddGeometry(rotatedRect);
            
            var arrowLine = new ArrowLineGeometry(new Point(50, 50), new Point(250, 200))
            {
                Stroke = new Pen(Brushes.DarkBlue, 2)
            };
            DisplayControl.AddGeometry(arrowLine);
            
            // 边缘位置的小元素
            var circle2 = new CircleGeometry(new Point(-400, 250), 20)
            {
                Fill = new SolidColorBrush(Colors.Pink),
                Stroke = new Pen(Brushes.HotPink, 1)
            };
            DisplayControl.AddGeometry(circle2);
            
            var circle3 = new CircleGeometry(new Point(650, -150), 25)
            {
                Fill = new SolidColorBrush(Colors.Violet),
                Stroke = new Pen(Brushes.Purple, 1)
            };
            DisplayControl.AddGeometry(circle3);

            MessageBox.Show("已创建复杂场景！\n\n" +
                           "内容分散在不同位置，包括：\n" +
                           "- 左上角的矩形和圆形\n" +
                           "- 右下角的矩形和线条\n" +
                           "- 中心的旋转矩形和箭头\n" +
                           "- 边缘位置的小圆形\n\n" +
                           "现在点击‘自动全局显示’按钮来看效果！",
                           "复杂场景演示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnFitToView_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.FitToGeometries();
        }

        private void BtnAutoFitAll_Click(object sender, RoutedEventArgs e)
        {
            // 使用新的自动全局显示功能
            DisplayControl.AutoFitToAllContent();
        }

        private void BtnResetView_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.ResetViewport();
        }

        #region 坐标系切换事件处理

        private void BtnMathCoordinates_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.SetCoordinateSystem(yAxisUp: true);
            UpdateCoordinateSystemButtons(true);
            
            MessageBox.Show("已切换到数学坐标系：向右为正X、向上为正Y", "坐标系切换", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnScreenCoordinates_Click(object sender, RoutedEventArgs e)
        {
            DisplayControl.SetCoordinateSystem(yAxisUp: false);
            UpdateCoordinateSystemButtons(false);
            
            MessageBox.Show("已切换到屏幕坐标系：向右为正X、向下为正Y", "坐标系切换", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuMathCoordinates_Click(object sender, RoutedEventArgs e)
        {
            BtnMathCoordinates_Click(sender, e);
        }

        private void MenuScreenCoordinates_Click(object sender, RoutedEventArgs e)
        {
            BtnScreenCoordinates_Click(sender, e);
        }

        private void MenuCoordinateDemo_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("是否使用数学坐标系（Y轴向上）来展示坐标系演示？\n\n选择“是”使用数学坐标系，选择“否”使用屏幕坐标系。", 
                "坐标系演示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Cancel)
                return;
                
            bool useMathCoordinates = result == MessageBoxResult.Yes;
            
            // 先清空现有内容
            DisplayControl.ClearGeometries();
            
            // 设置演示场景
            // CoordinateSystemExample.SetupCoordinateSystemDemo(DisplayControl, useMathCoordinates);
            
            // 更新按钮状态
            UpdateCoordinateSystemButtons(useMathCoordinates);
            
            // 显示详细说明
            string coordinateSystemName = useMathCoordinates ? "数学坐标系" : "屏幕坐标系";
            string yDirection = useMathCoordinates ? "向上" : "向下";
            
            MessageBox.Show($"演示场景已创建！\n\n" +
                $"当前坐标系：{coordinateSystemName}\n" +
                $"X轴：向右为正\n" +
                $"Y轴：{yDirection}为正\n\n" +
                "请观察绿色圆点（Y轴正方向点）在不同坐标系下的位置差异！", 
                "演示说明", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region 多层坐标系管理

        private void BtnGenerateCoordinateSystems_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 清空现有的坐标系容器
                foreach (var container in _coordinateSystemContainers)
                {
                    DisplayControl.RemoveGeometry(container);
                }
                _coordinateSystemContainers.Clear();
                _currentActiveCoordinateSystem = null;
                
                // 生成新的多层坐标系结构
                var worldContainer = HierarchicalCoordinateSystemDemo.CreateCompleteHierarchy();
                
                // 添加到显示控件
                DisplayControl.AddGeometry(worldContainer);
                _coordinateSystemContainers.Add(worldContainer);
                
                // 更新ComboBox
                UpdateCoordinateSystemsComboBox();
                
                // 设置第一个坐标系为当前激活的坐标系
                if (CmbCoordinateSystems.Items.Count > 0)
                {
                    CmbCoordinateSystems.SelectedIndex = 0;
                }
                
                MessageBox.Show("多层坐标系已成功生成！\n\n" +
                               "结构包含：\n" +
                               "- 1个世界坐标系\n" +
                               "- 2个轨道坐标系\n" +
                               "- 3个载具坐标系\n" +
                               "- 4个电路板坐标系\n" +
                               "- 7个子拼板坐标系\n\n" +
                               "可以通过ComboBox选择不同的坐标系进行查看。",
                               "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成多层坐标系时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 更新坐标系ComboBox
        /// </summary>
        private void UpdateCoordinateSystemsComboBox()
        {
            CmbCoordinateSystems.Items.Clear();
            
            foreach (var container in _coordinateSystemContainers)
            {
                AddCoordinateSystemToComboBox(container);
            }
        }
        
        /// <summary>
        /// 递归添加坐标系到ComboBox
        /// </summary>
        private void AddCoordinateSystemToComboBox(HierarchicalImageContainer container)
        {
            // 添加当前容器
            string displayName = $"{new string(' ', container.ContainerLevel * 2)}{container.HierarchicalCoordinateSystem.Name}";
            CmbCoordinateSystems.Items.Add(new ComboBoxItem
            {
                Content = displayName,
                Tag = container
            });
            
            // 递归添加子容器
            foreach (var childContainer in container.ChildContainers)
            {
                if (childContainer is HierarchicalImageContainer hierarchicalChild)
                {
                    AddCoordinateSystemToComboBox(hierarchicalChild);
                }
            }
        }
        
        /// <summary>
        /// ComboBox选择变化事件处理
        /// </summary>
        private void CmbCoordinateSystems_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CmbCoordinateSystems.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem && 
                selectedItem.Tag is HierarchicalImageContainer selectedContainer)
            {
                _currentActiveCoordinateSystem = selectedContainer;
                
                // 在状态栏显示当前坐标系信息
                UpdateCoordinateSystemStatusBar(selectedContainer);
                
                // 可以在这里添加高亮显示当前坐标系的逻辑
                HighlightCoordinateSystem(selectedContainer);
            }
        }
        
        /// <summary>
        /// 更新状态栏坐标系信息
        /// </summary>
        private void UpdateCoordinateSystemStatusBar(HierarchicalImageContainer container)
        {
            if (container != null)
            {
                TxtCoordinateSystemInfo.Text = $"坐标系信息: {container.HierarchicalCoordinateSystem.Name} " +
                                             $"(层级: {container.ContainerLevel}, " +
                                             $"路径: {container.FullContainerPath})";
            }
            else
            {
                TxtCoordinateSystemInfo.Text = "坐标系信息: 无";
            }
        }
        
        /// <summary>
        /// 高亮显示当前坐标系
        /// </summary>
        private void HighlightCoordinateSystem(HierarchicalImageContainer container)
        {
            // 重置所有容器的边框颜色
            ResetAllContainerStrokes();
            
            // 设置当前容器的高亮边框
            var originalStroke = container.Stroke;
            container.Stroke = new Pen(Brushes.Red, 3);
            
            // 5秒后恢复原样式
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (sender, e) =>
            {
                container.Stroke = originalStroke;
                timer.Stop();
            };
            timer.Start();
        }
        
        /// <summary>
        /// 重置所有容器的边框样式
        /// </summary>
        private void ResetAllContainerStrokes()
        {
            foreach (var container in _coordinateSystemContainers)
            {
                ResetContainerStrokeRecursive(container);
            }
        }
        
        /// <summary>
        /// 递归重置容器边框样式
        /// </summary>
        private void ResetContainerStrokeRecursive(HierarchicalImageContainer container)
        {
            // 恢复默认边框样式（这里可以根据需要调整）
            if (container.Stroke?.Thickness == 3 && container.Stroke.Brush is SolidColorBrush brush && brush.Color == Colors.Red)
            {
                // 恢复默认边框样式，这里简化处理
                container.Stroke = new Pen(Brushes.Blue, 2);
            }
            
            // 递归处理子容器
            foreach (var childContainer in container.ChildContainers)
            {
                if (childContainer is HierarchicalImageContainer hierarchicalChild)
                {
                    ResetContainerStrokeRecursive(hierarchicalChild);
                }
            }
        }

        #endregion

        #region 菜单事件处理

        private void MenuCreateComplexScene_Click(object sender, RoutedEventArgs e)
        {
            CreateComplexSceneForAutoFitDemo();
        }

        private void MenuAutoFitDemo_Click(object sender, RoutedEventArgs e)
        {
            if (!DisplayControl.HasVisibleContent)
            {
                var result = MessageBox.Show("当前没有可见内容。\n\n是否先创建复杂场景用于演示？",
                    "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    CreateComplexSceneForAutoFitDemo();
                }
                else
                {
                    return;
                }
            }

            // 获取当前内容边界
            Rect contentBounds = DisplayControl.GetAllContentBounds();
            
            string message = $"当前内容边界信息：\n\n" +
                           $"位置: ({contentBounds.X:F1}, {contentBounds.Y:F1})\n" +
                           $"尺寸: {contentBounds.Width:F1} x {contentBounds.Height:F1}\n" +
                           $"总面积: {contentBounds.Width * contentBounds.Height:F0}\n\n" +
                           $"即将执行自动全局显示...";
            
            MessageBox.Show(message, "自动适应演示", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // 执行自动全局显示
            DisplayControl.AutoFitToAllContent();
            
            MessageBox.Show("自动全局显示完成！\n\n所有可见内容现在都在视口中最大化显示。",
                "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuContentBoundsInfo_Click(object sender, RoutedEventArgs e)
        {
            Rect contentBounds = DisplayControl.GetAllContentBounds();
            
            string message;
            if (contentBounds.IsEmpty)
            {
                message = "当前没有可见内容。";
            }
            else
            {
                message = $"内容边界信息：\n\n" +
                         $"位置: ({contentBounds.X:F2}, {contentBounds.Y:F2})\n" +
                         $"尺寸: {contentBounds.Width:F2} x {contentBounds.Height:F2}\n" +
                         $"总面积: {contentBounds.Width * contentBounds.Height:F0}\n\n" +
                         $"有可见内容: {DisplayControl.HasVisibleContent}\n" +
                         $"几何图形数量: {DisplayControl.GeometryLayer.Geometries.Count}\n" +
                         $"图像数量: {DisplayControl.ImageLayer.Images.Count}";
            }
            
            MessageBox.Show(message, "内容边界信息", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuEnableAutoFit_Click(object sender, RoutedEventArgs e)
        {
            bool isEnabled = MenuEnableAutoFit.IsChecked;
            
            if (isEnabled)
            {
                // 启用实时自动适应
                DisplayControl.ContentChanged += OnContentChangedAutoFit;
                MessageBox.Show("实时自动适应已启用！\n\n当内容发生变化时，视口将自动调整以显示所有内容。",
                    "实时自加适应", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 禁用实时自动适应
                DisplayControl.ContentChanged -= OnContentChangedAutoFit;
                MessageBox.Show("实时自动适应已禁用。",
                    "实时自动适应", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("关于 XDisplay 测试程序\n\n" +
                           "这是 XDisplay 控件的测试和演示程序。\n\n" +
                           "新增功能：\n" +
                           "- 自动全局显示：自动计算所有可见内容的最小外接矩形\n" +
                           "- 智能缩放：选择最优缩放比例以最大化显示内容\n" +
                           "- 不变形显示：保持内容的宽高比不变\n" +
                           "- 实时更新：支持内容变化时的自动适应\n\n" +
                           "\u4f7f用方法：\n" +
                           "1. 创建或加载一些内容\n" +
                           "2. 点击‘自动全局显示’按钮\n" +
                           "3. 或者使用菜单中的演示功能",
                           "关于", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 实时自动适应事件处理
        /// </summary>
        private void OnContentChangedAutoFit(object? sender, EventArgs e)
        {
            if (DisplayControl.HasVisibleContent)
            {
                DisplayControl.AutoFitToAllContent();
            }
        }

        #endregion

        private void BtnPerformanceTest_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("将生成2000个小矩形分布在圆形区域中进行性能测试。\n\n这可能会需要一些时间，是否继续？",
                "性能测试", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // 清空现有内容
                DisplayControl.ClearGeometries();
                DisplayControl.ClearImages();
                
                CreatePerformanceTestRectangles();
                
                MessageBox.Show("性能测试完成！\n\n已生成2000个小矩形。\n现在可以测试缩放、平移等操作的性能。",
                    "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 自动适应视图以显示所有矩形
                DisplayControl.AutoFitToAllContent();
            }
        }
        
        /// <summary>
        /// 创建2000个小矩形分布在圆形区域中
        /// </summary>
        private void CreatePerformanceTestRectangles()
        {
            const int totalRects = 30000;
            const double circleRadius = 500; // 圆形区域半径
            const double rectSize = 8; // 小矩形尺寸
            
            var random = new Random(42); // 使用固定种子以获得可重现的结果
            var center = new Point(0, 0); // 圆心位置
            
            // 生成多种颜色
            var colors = new Color[]
            {
                Colors.Red, Colors.Green, Colors.Blue, Colors.Orange, Colors.Purple,
                Colors.Yellow, Colors.Pink, Colors.Cyan, Colors.Magenta, Colors.Lime,
                Colors.Brown, Colors.Navy, Colors.Teal, Colors.Olive, Colors.Maroon
            };
            
            var startTime = DateTime.Now;
            
            for (int i = 0; i < totalRects; i++)
            {
                // 在圆形区域内随机生成位置
                double angle = random.NextDouble() * 2 * Math.PI;
                double distance = Math.Sqrt(random.NextDouble()) * circleRadius; // 使用平方根保证均匀分布
                
                double x = center.X + distance * Math.Cos(angle);
                double y = center.Y + distance * Math.Sin(angle);
                
                // 创建小矩形
                var rect = new XDisplayRect(new Rect(x - rectSize/2, y - rectSize/2, rectSize, rectSize))
                {
                    Fill = new SolidColorBrush(colors[i % colors.Length]) { Opacity = 0.7 },
                    Stroke = new Pen(new SolidColorBrush(colors[i % colors.Length]), 0.5),
                    ZOrder = 1
                };
                
                DisplayControl.AddGeometry(rect);
                
                // 每100个矩形更新一次状态栏，让用户看到进度
                if ((i + 1) % 100 == 0)
                {
                    UpdateStatusBar();
                    // 让UI有机会更新
                    Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            
            var endTime = DateTime.Now;
            var duration = endTime - startTime;
            
            // 最终更新状态栏
            UpdateStatusBar();
            
            // 显示创建时间统计
            var timeMessage = $"创建{totalRects}个矩形的时间：{duration.TotalMilliseconds:F0} 毫秒";
            Console.WriteLine(timeMessage);
        }

        private void ChkShowGrid_Checked(object sender, RoutedEventArgs e)
        {
            if (DisplayControl != null)
            {
                DisplayControl.BackgroundGridVisible = true;
            }
        }

        private void ChkShowGrid_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DisplayControl != null)
            {
                DisplayControl.BackgroundGridVisible = false;
            }
        }

        private void ChkShowCheckerboard_Checked(object sender, RoutedEventArgs e)
        {
            if (DisplayControl?.GridLayer != null)
            {
                DisplayControl.GridLayer.ShowCheckerboard = true;
            }
        }

        private void ChkShowCheckerboard_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DisplayControl?.GridLayer != null)
            {
                DisplayControl.GridLayer.ShowCheckerboard = false;
            }
        }

        private void DisplayControl_MouseCoordinateChanged(object? sender, MouseCoordinateChangedEventArgs e)
        {
            // 检查是否是鼠标离开事件
            if (double.IsNaN(e.WorldCoordinate.X) || double.IsNaN(e.WorldCoordinate.Y))
            {
                TxtMouseCoordinates.Text = "坐标: -";
            }
            else
            {
                // 更新鼠标坐标显示
                TxtMouseCoordinates.Text = $"坐标: 屏幕({e.ScreenCoordinate.X:F0}, {e.ScreenCoordinate.Y:F0}) 世界({e.WorldCoordinate.X:F2}, {e.WorldCoordinate.Y:F2})";
            }
        }

        private void DisplayControl_GeometryAdded(object sender, GeometryEventArgs e)
        {
            UpdateStatusBar();
        }

        private void DisplayControl_GeometryRemoved(object sender, GeometryEventArgs e)
        {
            UpdateStatusBar();
        }

        private void DisplayControl_SelectionChanged(object sender, GeometrySelectionChangedEventArgs e)
        {
            UpdateStatusBar();
        }

        private void DisplayControl_ViewportTransformChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
        }

        private void UpdateModeButtons()
        {
            BtnSelect.IsEnabled = DisplayControl.CurrentMode != InteractionMode.Select;
            BtnPan.IsEnabled = DisplayControl.CurrentMode != InteractionMode.Pan;
            BtnDrawRect.IsEnabled = DisplayControl.CurrentMode != InteractionMode.DrawRectangle;
            BtnDrawCircle.IsEnabled = DisplayControl.CurrentMode != InteractionMode.DrawCircle;
            BtnDrawLine.IsEnabled = DisplayControl.CurrentMode != InteractionMode.DrawLine;
            BtnDrawArrow.IsEnabled = DisplayControl.CurrentMode != InteractionMode.DrawArrowLine;
            BtnDrawRotatedRect.IsEnabled = DisplayControl.CurrentMode != InteractionMode.DrawRotatedRectangle;

            string modeText = DisplayControl.CurrentMode switch
            {
                InteractionMode.Select => "选择",
                InteractionMode.Pan => "平移",
                InteractionMode.DrawRectangle => "绘制矩形",
                InteractionMode.DrawCircle => "绘制圆形",
                InteractionMode.DrawLine => "绘制线段",
                InteractionMode.DrawArrowLine => "绘制箭头",
                InteractionMode.DrawRotatedRectangle => "绘制旋转矩形",
                _ => "未知"
            };
            TxtMode.Text = "模式: " + modeText;
        }

        /// <summary>
        /// 更新坐标系按钮状态
        /// </summary>
        /// <param name="isYAxisUp">是否Y轴向上</param>
        private void UpdateCoordinateSystemButtons(bool isYAxisUp)
        {
            BtnMathCoordinates.IsEnabled = !isYAxisUp;
            BtnScreenCoordinates.IsEnabled = isYAxisUp;
            
            MenuMathCoordinates.IsChecked = isYAxisUp;
            MenuScreenCoordinates.IsChecked = !isYAxisUp;
        }

        private void UpdateStatusBar()
        {
            var selectedCount = DisplayControl.SelectedGeometries.Count;
            if (selectedCount == 0)
            {
                TxtSelection.Text = "选中: 无";
            }
            else if (selectedCount == 1)
            {
                var selected = DisplayControl.SelectedGeometries[0];
                TxtSelection.Text = "选中: " + selected.TypeName;
            }
            else
            {
                TxtSelection.Text = "选中: " + selectedCount + " 个对象";
            }

            double scale = DisplayControl.ViewportTransform.Scale;
            TxtScale.Text = "缩放: " + scale.ToString("P0");

            int geometryCount = DisplayControl.GeometryLayer.Geometries.Count;
            int imageCount = DisplayControl.ImageLayer.Images.Count;
            TxtGeometryCount.Text = $"图形数量: {geometryCount}, 图像数量: {imageCount}";
        }
    }
}