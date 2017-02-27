﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Diagnostics;
using System.ComponentModel;
using System.Configuration;
using System.Net;
using System.Xml;
using System.Windows.Media.Imaging;
using System.Globalization;

namespace WallpaperManager
{
    #region DataGridItem
    public class DataGridItem : IEquatable<DataGridItem>
    {
        public bool IsEnabled { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Resolution { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }

        public DataGridItem()
        {
            IsEnabled = true;
            Name = string.Empty;
            Path = string.Empty;
            Resolution = string.Empty;
            Size = 0;
            Type = string.Empty;
        }

        public DataGridItem(bool isEnabled, string name, string path, string resolution, long size, string type)
        {
            IsEnabled = isEnabled;
            Name = name;
            Path = path;
            Resolution = resolution;
            Size = size;
            Type = type;
        }

        public bool Equals(DataGridItem other)
        {
            return (Name == other.Name) && (Path == other.Path) && (Resolution == other.Resolution) && (Size == other.Size) && (Type == other.Type);
        }
    }
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        #region DraggedItem
        /// <summary>
        /// DraggedItem Dependency Property
        /// </summary>
        public static readonly DependencyProperty DraggedItemProperty = DependencyProperty.Register("DraggedItem", typeof(DataGridItem), typeof(MainWindow));

        /// <summary>
        /// Gets or sets the DraggedItem property.
        /// </summary>
        public DataGridItem DraggedItem
        {
            get { return (DataGridItem)GetValue(DraggedItemProperty); }
            set { SetValue(DraggedItemProperty, value); }
        }
        #endregion

        #region BackgroundType
        public enum BackgroundType : int
        {
            SolidColor,
            Picture,
            Slideshow
        }
        #endregion

        private static readonly List<string> supportedExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };
        private static readonly string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WallpaperManager";
        private static readonly WallpaperHandler.Style fallbackWallpaperStyle = WallpaperHandler.Style.Stretch;
        
        private int wallpaperIndex = 0;
        private double interval = 10;
        //private WallpaperHandler.Style wallpaperStyle = WallpaperHandler.Style.Fill;
        private BackgroundType background = BackgroundType.SolidColor;
        private System.Drawing.Color backgroundColor;

        private DataGridItem lastItem = new DataGridItem();
        private WallpaperHandler.Style lastStyle;

        private DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);

        private ObservableCollection<DataGridItem> dataGridData = new ObservableCollection<DataGridItem>();
        private ObservableCollection<double> intervalComboBoxData = new ObservableCollection<double>(); /*new ObservableCollection<double>(Properties.Settings.Default.IntervalList);*/

        private bool datagridHasItems = true;

        private int dirCount = 0; private int fileCount = 0;
        
        //TODO: Write more userfriendly GUI

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            }
            catch (ConfigurationErrorsException ex)
            {
                if (MessageBox.Show("WallpaperManager has detected that your user settings file has become corrupted. " +
                                      "This may be due to a crash or improper exiting of the program. WallpaperManager must reset your user settings in order to continue." + 
                                      "\n\nClick Yes to reset your user settings and continue.\n\n" +
                                      "Click No if you wish to attempt manual repair or to rescue information before proceeding.",
                                      "Error! Corrupt user settings",
                                      MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                {
                    File.Delete(ex.Filename);
                    Properties.Settings.Default.Reload();
                    // you could optionally restart the app instead
                }
                else
                    Process.GetCurrentProcess().Kill();
                    // avoid the inevitable crash
            }

            //intervalComboBoxData = new ObservableCollection<double>(Properties.Settings.Default.IntervalList);
            intervalComboBoxData = new ObservableCollection<double>(new List<double> { 15, 30, 60, 300, 900, 3600, 86400 });

            //Bind DataGrid ItemSource and ComboBox DataContext
            dataGrid.ItemsSource = dataGridData;
            IntervalComboBox.DataContext = intervalComboBoxData;

            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }

            //Restore Window Pos, Window Size and Window State
            WindowState = Properties.Settings.Default.WindowState;
            Width = Properties.Settings.Default.WindowSize.X;
            Height = Properties.Settings.Default.WindowSize.Y;
            Left = Properties.Settings.Default.WindowPos.X;
            Top = Properties.Settings.Default.WindowPos.Y;
            
            //Restore Interval, 
            interval = intervalComboBoxData[Properties.Settings.Default.intervalIndex % intervalComboBoxData.Count];
            //wallpaperStyle = (WallpaperHandler.Style) Properties.Settings.Default.iWallpaperStyle;
            background = (BackgroundType) Properties.Settings.Default.backgroundType;
            backgroundColor = System.Drawing.ColorTranslator.FromHtml(Properties.Settings.Default.backgroundColorHex);

            if (WindowState != WindowState.Minimized) { TrayIcon_ButtonRestore.IsEnabled = false; TrayIcon_ButtonRestore.Opacity = .5; }

            if (!Directory.Exists(applicationDataPath + @"\bing")) Directory.CreateDirectory(applicationDataPath + @"\bing");
            if (!Directory.Exists(applicationDataPath + @"\bing\wallpaper")) Directory.CreateDirectory(applicationDataPath + @"\bing\wallpaper");
            if (!Directory.Exists(applicationDataPath + @"\bing\thumbnails")) Directory.CreateDirectory(applicationDataPath + @"\bing\thumbnails");

            InitGUI();

            Bing_UpdateGUI();
        }

        #region Dispatcher Timer
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Start();
            if (background == BackgroundType.Picture || background == BackgroundType.SolidColor) dispatcherTimer.Stop();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (dataGridData.Count > 0 && background != BackgroundType.Picture && background != BackgroundType.SolidColor)
            {
                //Test
                //if (trayIcon_ContextMenu.IsOpen) TrayIcon_TextBlockTimeLeft.Text = TimeSpan.FromSeconds((interval - Properties.Settings.Default.count)).ToString(@"hh\:mm\:ss");

                if ((Properties.Settings.Default.count % interval) == 0)
                {
                    Properties.Settings.Default.count = 0;
                    UpdateWallpaper();
                    DebugLog.Log("Wallpaper was updated!");
                }
                Properties.Settings.Default.count++;
            }
        }
        #endregion

        #region MainWindow ButtonEvents
        private void switchTheme_Click(object sender, RoutedEventArgs e)
        {
            Tuple<AppTheme, Accent> theme = ThemeManager.DetectAppStyle(Application.Current);
            AppTheme invertedAppTheme = ThemeManager.GetInverseAppTheme(theme.Item1);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, invertedAppTheme);
            switchTheme.Content = ((invertedAppTheme.Name == "BaseLight") ? "Dark" : "Light") + " Theme";
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                TrayIcon_ButtonRestore.IsEnabled = true;
                TrayIcon_ButtonRestore.Opacity = 1;
                Hide();
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.WindowSize = new Vector(Width, Height);
            Properties.Settings.Default.WindowPos = new Vector(Left, Top);
            Properties.Settings.Default.WindowState = WindowState;
            Properties.Settings.Default.Save();
        }
        #endregion     

        #region TrayIcon Button EventHandler
        private void TrayIcon_ButtonExitClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.WindowSize = new Vector(Width, Height);
            Properties.Settings.Default.WindowPos = new Vector(Left, Top);
            Properties.Settings.Default.WindowState = WindowState;
            Properties.Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private void TrayIcon_ButtonRestoreClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Show();
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
                TrayIcon_ButtonRestore.IsEnabled = false;
                TrayIcon_ButtonRestore.Opacity = .5;
            }
        }

        private void TrayIcon_ButtonChangeWPClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                Debug.WriteLine("LMB");
            else if (e.ChangedButton == MouseButton.Right)
                Debug.WriteLine("RMB");
        }
        #endregion

        #region CustomTab

        #region GUI Events
        private void ButtonOpenDir_Click(object sender, RoutedEventArgs e)
        {
            ResetCount();
            System.Windows.Forms.FolderBrowserDialog objDialog = new System.Windows.Forms.FolderBrowserDialog();
            objDialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(objDialog.SelectedPath)) {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += backgroundWorker_treescan;
                worker.RunWorkerCompleted += backgroundWorker_updateGUI;
                worker.RunWorkerAsync(objDialog.SelectedPath);
            } else {
                DebugLog.Warning("No directory selected!");
            }
        }

        private void ButtonAddFile_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.bmp;*.gif;*.png)|*.jpg;*.jpeg;*.bmp;*.gif;*.png;|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (openFileDialog.ShowDialog() == true)
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += backgroundWorker_treescan;
                worker.RunWorkerCompleted += backgroundWorker_updateGUI;
                worker.RunWorkerAsync(openFileDialog.FileNames);
            }
            else
                DebugLog.Log("No file(s) selected!");
            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void backgroundWorker_treescan(object sender, DoWorkEventArgs e)
        {
            ResetCount();
            DateTime begin = DateTime.UtcNow;
            Application.Current.Dispatcher.Invoke(delegate { label.Content = "Scanning. This may take a while..."; });

            string[] pathArray = (e.Argument.GetType() == typeof(string)) ? new string[] { (string)e.Argument } : (string[])e.Argument;
            foreach (string path in pathArray)
            {
                TreeScan(path);
            }
            e.Result = Math.Round((DateTime.UtcNow - begin).TotalSeconds, 2);
        }

        private void backgroundWorker_updateGUI(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateGUI();
            UpdateDataGridGUI();
            label.Content = "Scanned dir(s): " + dirCount + " | Scanned file(s): " + fileCount + " | File(s) in datagrid: " + dataGridData.Count + " | Elapsed time: " + e.Result + " second(s)";
        }

        private void ButtonClearList_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridData.Count <= 0) return;
            dataGridData.Clear();
            UpdateGUI();
            UpdateDataGridGUI();
            label.Content = string.Empty;
        }

        private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Properties.Settings.Default.iBackgroundType = BackgroundComboBox.SelectedIndex;

            background = (BackgroundType)BackgroundComboBox.SelectedIndex;
            if (dispatcherTimer.IsEnabled) { dispatcherTimer.Stop(); Properties.Settings.Default.count = 0; }
            if (background == BackgroundType.Slideshow) dispatcherTimer.Start();
            if (background == BackgroundType.SolidColor) lastItem = new DataGridItem();
            UpdateGUI();
        }

        private void ButtonShuffleList_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.UnselectAllCells();
            Utilities.ClearSort(dataGrid);
            dataGridData.Shuffle();
            wallpaperIndex = 0;
        }

        private void ButtonChangeWP_Click(object sender, RoutedEventArgs e)
        {
            if (background == BackgroundType.SolidColor)
            {
                var wmcolor = ColorPicker.SelectedColor.Value;
                WallpaperHandler.SetSolidColor(System.Drawing.Color.FromArgb(wmcolor.A, wmcolor.R, wmcolor.G, wmcolor.B));
            }
            else
                UpdateWallpaper((dataGrid.SelectedItem != null) ? (DataGridItem)dataGrid.SelectedItem : null);
        }

        private void WPStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            Properties.Settings.Default.wallpaperStyleIndex = (int)Enum.Parse(typeof(WallpaperHandler.Style), WPStyleComboBox.SelectedValue.ToString(), true);
            Properties.Settings.Default.Save();
        }

        private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            interval = (double)IntervalComboBox.SelectedItem;
            Properties.Settings.Default.intervalIndex = IntervalComboBox.SelectedIndex;
            Properties.Settings.Default.count = 0;
            Properties.Settings.Default.Save();
        }

        #region DataGrid Events
        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Ensure row was clicked and not empty space
            if (ItemsControl.ContainerFromElement((DataGrid)sender, (DependencyObject)e.OriginalSource) as DataGridRow == null) return;

            if (background == BackgroundType.SolidColor) background = BackgroundType.Picture;
            UpdateWallpaper((DataGridItem)dataGrid.SelectedItem);
        }

        private void dataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                BackgroundWorker worker = new BackgroundWorker();
                worker.DoWork += backgroundWorker_treescan;
                worker.RunWorkerCompleted += backgroundWorker_updateGUI;
                worker.RunWorkerAsync(e.Data.GetData(DataFormats.FileDrop, true));
            }
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            int index = (DataGridHelper.TryFindParent<DataGridRow>((Button)sender)).GetIndex();

            if (dataGridData.ElementAt(index) != null)
                Process.Start(dataGridData[index].Path);
            else
                DebugLog.Error("The Item which should be opened couldn't be found");
        }

        private void ButtonRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            int index = (DataGridHelper.TryFindParent<DataGridRow>((Button)sender)).GetIndex();

            if (dataGridData.ElementAt(index) != null)
            {
                dataGridData.RemoveAt(index);
                UpdateGUI();
            }
            else
                DebugLog.Error("The Item which should be removed couldn't be found");
        }
        #endregion

        #endregion

        private void ResetCount()
        {
            dirCount = 0; fileCount = 0;
        }

        private void TreeScan(string path)
        {
            if (Directory.Exists(path))
            {
                AddFilesToList(Directory.GetFiles(path));
                //Loop trough each directory
                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        dirCount++;
                        TreeScan(dir); //Recursive call to get subdirs
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        DebugLog.Error(e.Message);
                        continue;
                    }
                }
            }
            else if (File.Exists(path)) AddFilesToList(path);
        }

        private void AddFilesToList(object obj)
        {
            if (obj.GetType() != typeof(string) && obj.GetType() != typeof(string[])) return;

            string[] fileArray = (obj.GetType() == typeof(string)) ? new string[] { (string)obj } : (string[])obj;
            foreach (string file in fileArray)
            {
                if (string.IsNullOrWhiteSpace(file)) continue;
                fileCount++;
                //Application.Current.Dispatcher.Invoke(delegate { label.Content = file; });
                if (supportedExtensions.Contains(Path.GetExtension(file).ToUpperInvariant()))
                {
                    var img = System.Drawing.Image.FromFile(file);
                    DataGridItem item = new DataGridItem(true, Path.GetFileNameWithoutExtension(file), file, string.Format("{0}x{1}", img.Width, img.Height), Utilities.GetFileSizeOnDisk(file), Path.GetExtension(file).Substring(1).ToUpper());
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        if (!dataGridData.Contains(item))
                            dataGridData.Add(item);
                        else
                            DebugLog.Warning("File '" + file + "' is already in list!");
                    });
                }
                else
                    DebugLog.Warning("File '" + file + "' has an unsupported file extension!");
            }
        }

        private void UpdateWallpaper(DataGridItem item = null)
        {
            if (dataGridData.Count <= 0) return;
            if (item == null) item = dataGridData.ElementAt(wallpaperIndex % dataGridData.Count);
            if (item != null && !string.IsNullOrWhiteSpace(item.Path))
            {
                System.Drawing.Color lastBackgroundColor = backgroundColor;
                System.Windows.Media.Color colorPickerSelectedColor = ColorPicker.SelectedColor.Value;
                backgroundColor = System.Drawing.Color.FromArgb(colorPickerSelectedColor.A, colorPickerSelectedColor.R, colorPickerSelectedColor.G, colorPickerSelectedColor.B);
                if (!lastItem.Path.Equals(item.Path) || (lastItem.Path.Equals(item.Path) && !lastStyle.Equals((WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex)) || (lastItem.Path.Equals(item.Path) && !lastBackgroundColor.Equals(backgroundColor)) && item.IsEnabled)
                {
                    WallpaperHandler.Set(item.Path, (WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex, backgroundColor);
                    wallpaperIndex = ((dataGrid.SelectedItem != null) ? (dataGrid.SelectedIndex + 1) : (wallpaperIndex + 1)) % dataGrid.Items.Count;

                    //When double clicking on an item the timer will be reset to zero
                    Properties.Settings.Default.count = 0;

                    lastStyle = (WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex;
                    lastItem = item;
                    UpdateGUI();
                }
                else
                    DebugLog.Warning("Item '" + item.Path + "' is already set as wallpaper!");
            }
            else
                DebugLog.Error("Wallpaper couldn't be set, because item is null!");
        }

        private bool GetEnumStringEnumType<TEnum>(string _string) where TEnum : struct
        {
            TEnum _enum = default(TEnum);
            return (Enum.TryParse(_string, true, out _enum) && Enum.IsDefined(typeof(TEnum), _enum));
        }

        int lastItemIndex = 0; bool useLastItemIndex = false;

        #region GUI
        private void InitGUI()
        {
            IntervalComboBox.SelectedIndex = Properties.Settings.Default.intervalIndex;
            WPStyleComboBox.SelectedIndex = Properties.Settings.Default.wallpaperStyleIndex;

            ColorPicker.SelectedColor = System.Windows.Media.Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B);
            BackgroundComboBox.SelectedIndex = (int)background;

            if (!Utilities.IsWin7OrHigher())
            {
                ((ComboBoxItem)WPStyleComboBox.Items[0]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[0]).ToolTip = "This wallpaperstyle needs Win7 or higher";
                ((ComboBoxItem)WPStyleComboBox.Items[1]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[1]).ToolTip = "This wallpaperstyle needs Win7 or higher";
                ((ComboBoxItem)WPStyleComboBox.Items[5]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[5]).ToolTip = "This wallpaperstyle needs Win7 or higher";

                if ((WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex == WallpaperHandler.Style.Fill || (WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex == WallpaperHandler.Style.Fit || (WallpaperHandler.Style)Properties.Settings.Default.wallpaperStyleIndex == WallpaperHandler.Style.Span)
                    WPStyleComboBox.SelectedIndex = (int)fallbackWallpaperStyle;
            }

            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void UpdateGUI()
        {
            UpdateButton(ButtonClearList);
            //TODO: Special cases for ButtonChangeWP
            UpdateButton(ButtonChangeWP);
            UpdateButton(ButtonShuffleList);

            IntervalComboBox.IsEnabled = !(background == BackgroundType.Picture || background == BackgroundType.SolidColor);
            WPStyleComboBox.IsEnabled = !(background == BackgroundType.SolidColor);

            // Disable specific Items on specific datagriddata count
            ((ComboBoxItem)BackgroundComboBox.Items[1]).IsEnabled = (dataGridData.Count > 0) ? true : false;
            ((ComboBoxItem)BackgroundComboBox.Items[2]).IsEnabled = (dataGridData.Count > 1) ? true : false;

            if (!useLastItemIndex && dataGridData.Count < 2)
            {
                if ((BackgroundComboBox.SelectedIndex == 1 && dataGridData.Count < 1) || (BackgroundComboBox.SelectedIndex == 2 && dataGridData.Count <= 1))
                {
                    useLastItemIndex = true;
                    lastItemIndex = BackgroundComboBox.SelectedIndex;
                }
            }
            else if (useLastItemIndex && dataGridData.Count >= 2)
            {
                if ((lastItemIndex == 1) || (lastItemIndex == 2))
                {
                    useLastItemIndex = false;
                    BackgroundComboBox.SelectedIndex = lastItemIndex;
                }
            }

            if (dataGridData.Count == 1 && useLastItemIndex) BackgroundComboBox.SelectedIndex = 1;
            else if (dataGridData.Count < 1 && useLastItemIndex) BackgroundComboBox.SelectedIndex = 0;

            //Special Case
            if (background == BackgroundType.SolidColor && !ButtonChangeWP.IsEnabled) { ButtonChangeWP.IsEnabled = true; ButtonChangeWP.Opacity = 1; }
        }

        private void UpdateDataGridGUI()
        {
            //Console.WriteLine(((DataGridCell)dataGrid.SelectedItem).Column.DisplayIndex);

            //ButtonChangeWP.Content = (dataGrid.SelectedItem == null) ? "Change Wallpaper" : "Next Wallpaper"; 

            if (dataGrid.HasItems == datagridHasItems) return;
            datagridHasItems = dataGrid.HasItems;

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                dataGrid.Columns[i].Visibility = (dataGrid.HasItems) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void UpdateButton(Button button)
        {
            button.IsEnabled = (dataGridData.Count > 0);
            button.Opacity = (button.IsEnabled) ? 1 : .5;   
        }
        #endregion

        #region DataGrid Drag and Drop Rows
        private bool isDragging = false;
       
        private void dataGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender != null)
            {
                DataGrid grid = sender as DataGrid;
                if (grid != null && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    if (!dgr.IsMouseOver)
                    {
                        (dgr as DataGridRow).IsSelected = false;
                    }
                }
            }

            if (!isDragNDropEnabled) return; 

            //find the clicked row
            var row = DataGridHelper.TryFindFromPoint<DataGridRow>((UIElement)sender, e.GetPosition(dataGrid));
            if (row == null) return;

            //set flag that indicates we're capturing mouse movements
            isDragging = true;
            DraggedItem = (DataGridItem)row.Item;
        }

        /// <summary>
        /// Completes a drag/drop operation.
        /// </summary>
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isDragging) return;

            //get the target item
            DataGridItem targetItem = (DataGridItem)dataGrid.SelectedItem;

            if (targetItem == null || !ReferenceEquals(DraggedItem, targetItem))
            {
                //get target index
                var targetIndex = dataGridData.IndexOf(targetItem);
                //remove the source from the list
                dataGridData.Remove(DraggedItem);
                //move source at the target's location
                dataGridData.Insert(targetIndex, DraggedItem);
                //select the dropped item
                dataGrid.SelectedItem = DraggedItem;
            }
            //reset
            ResetDragDrop();
        }

        /// <summary>
        /// Closes the popup and resets the
        /// grid to read-enabled mode.
        /// </summary>
        private void ResetDragDrop()
        {
            isDragging = false;
            popup.IsOpen = false;
            dataGrid.IsReadOnly = false;
        }

        /// <summary>
        /// Updates the popup's position in case of a drag/drop operation.
        /// </summary>
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging || e.LeftButton != MouseButtonState.Pressed) return;

            //display the popup if it hasn't been opened yet
            if (!popup.IsOpen)
            {
                //switch to read-only mode
                dataGrid.IsReadOnly = true;
                //make sure the popup is visible
                popup.IsOpen = true;
            }

            Size popupSize = new Size(popup.ActualWidth, popup.ActualHeight);
            popup.PlacementRectangle = new Rect(e.GetPosition(this), popupSize);

            //make sure the row under the grid is being selected
            Point position = e.GetPosition(dataGrid);
            var row = DataGridHelper.TryFindFromPoint<DataGridRow>(dataGrid, position);
            if (row != null) dataGrid.SelectedItem = row.Item;
        }

        private bool isDragNDropEnabled = false;

        private void MenuItemDragNDrop_Click(object sender, RoutedEventArgs e)
        {
            isDragNDropEnabled = ((MenuItem)sender).IsChecked;
            dataGrid.UnselectAllCells();
        }
        #endregion

        #endregion

        #region BingItem
        public class BingItem
        {
            public DateTime Date { get; set; }
            public string Url { get; set; }
            public string Name { get; set; }
            public string Resolution { get; set; }
            public string FilePath { get; set; }
            public string Copyright { get; set; }
            public string CopyrightLink { get; set; }

            public BingItem()
            {
                Date = new DateTime();
                Url = string.Empty;
                Name = string.Empty;
                Resolution = string.Empty;
                FilePath = string.Empty;
                Copyright = string.Empty;
                CopyrightLink = string.Empty;
            }

            public BingItem(DateTime date, string url, string name, string resolution, string filepath, string copyright, string copyrightlink)
            {
                Date = date;
                Url = url;
                Name = name;
                Resolution = resolution;
                FilePath = filepath;
                Copyright = copyright;
                CopyrightLink = copyrightlink;
            }
        }
        #endregion

        /** Bing Image Handler **/

        private static readonly string bingMainDir = applicationDataPath + @"\bing\";
        private static readonly string bingXMLFile = bingMainDir + @"bing.xml";
        private static readonly string bingWallpaperDir = bingMainDir + @"wallpaper\";
        private static readonly string bingThumbnailDir = bingMainDir + @"thumbnails\";

        int bIndex = 0;
        List<BingItem> bingWallpapers = new List<BingItem>();
        BingItem currentBingImage = null;
        string bingXMLMD5Hash = string.Empty;
        //bool isFetching = false;
        List<BitmapImage> bingBMPImages = new List<BitmapImage>();

        /* Bing Properties */
        /* Saveable Properties */
        private static readonly string bingXMLUrl = "http://www.bing.com/hpimagearchive.aspx?format=xml&idx=-1&n={0}&mkt={1}";
        private static readonly string bingImageUrl = "http://www.bing.com{0}_{1}.jpg";
        private static string thumbnailResolution = "1280x720";
        private static string region = "de-DE";
        private static int n = 8; /// > 8 not supported

        private void Bing_FetchXML()
        {
            using (WebClient wc = new WebClient())
            {
                /* TODO: Async downloading */
                //wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFile(new Uri(string.Format(bingXMLUrl, n, region)), bingXMLFile);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            //if (isFetching) return;
            //isFetching = true; Debug.WriteLine(DateTime.Now.ToString() + " Click!");

            if (File.Exists(bingXMLFile))
            {
                DateTime modification = File.GetLastWriteTime(bingXMLFile);
                if (modification.Day != DateTime.Now.Day || modification.Month != DateTime.Now.Month || modification.Year != DateTime.Now.Year)
                {
                    Debug.WriteLine("File already exists and is not up to date!");
                    Bing_FetchXML();
                }
            }
            else
                Bing_FetchXML();

            string currentXMLFileMD5Hash = Utilities.GetMD5HashFromFile(bingXMLFile);
            if (bingXMLMD5Hash.Equals(currentXMLFileMD5Hash)) return;

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(bingXMLFile);
            }
            catch (XmlException)
            {
                Debug.WriteLine("Failed to open XML file.");
                Bing_FetchXML();
                return;
            }
            XmlNodeList nodes = doc.SelectNodes("/images/image");
            foreach (XmlNode node in nodes)
            {
                BingItem item = new BingItem();
                item.Date = DateTime.ParseExact(node.SelectSingleNode("enddate").InnerText, "yyyyMMdd", CultureInfo.InvariantCulture);
                item.Url = node.SelectSingleNode("urlBase").InnerText;
                item.Name = (item.Url.Split('/'))[4];
                item.Resolution = thumbnailResolution;
                item.FilePath = bingThumbnailDir + item.Name + "_" + thumbnailResolution + ".jpg";
                item.Copyright = node.SelectSingleNode("copyright").InnerText;
                item.CopyrightLink = node.SelectSingleNode("copyrightlink").InnerText;
                if (!File.Exists(item.FilePath))
                {
                    using (WebClient wc = new WebClient())
                    {
                        // TODO: Async downloading
                        //wc.DownloadFile(new Uri("http://www.bing.com" + item.Url + "_" + thumbnailResolution + ".jpg"), item.FilePath);
                        wc.DownloadFile(new Uri(string.Format(bingImageUrl, item.Url, thumbnailResolution)), item.FilePath);
                    }
                    ///Problem: When using Bing_DownloadImage() program does not wait until download is finished resulting in crashing it   
                    //Bing_DownloadImage(item, thumbnailResolution);
                }
                else
                    Debug.WriteLine("File '" + item.FilePath + "' already exists");
                bingBMPImages.Add(new BitmapImage(new Uri(item.FilePath)));
                bingWallpapers.Add(item);
            }
            BingItem fItem = bingWallpapers.First();
            //image.Source = new BitmapImage(new Uri(fItem.FilePath));
            image.Source = bingBMPImages.First();
            label1.Content = string.Format("{0}.{1}.{2}", fItem.Date.Day.ToString().PadLeft(2, '0'), fItem.Date.Month.ToString().PadLeft(2, '0'), fItem.Date.Year);
            bingXMLMD5Hash = currentXMLFileMD5Hash;
            Bing_UpdateGUI();
            Bing_UpdateImage();
            //isFetching = false;
        }

        private void Bing_UpdateGUI()
        {
            if (bingWallpapers.Count > 0)
            {
                Bing_ImageLeftButton.IsEnabled = true;
                Bing_ImageLeftButton.Opacity = 1;
                Bing_ImageRightButton.IsEnabled = true;
                Bing_ImageRightButton.Opacity = 1;
            }
            else
            {
                Bing_ImageLeftButton.IsEnabled = false;
                Bing_ImageLeftButton.Opacity = .5f;
                Bing_ImageRightButton.IsEnabled = false;
                Bing_ImageRightButton.Opacity = .5f;
            }
        }

        private void Bing_UpdateImage()
        {
            currentBingImage = bingWallpapers.ElementAt(bIndex);
            //image.Source = new BitmapImage(new Uri(item.FilePath));
            image.Source = bingBMPImages.ElementAt(bIndex);
            label1.Content = string.Format("{0}.{1}.{2}", currentBingImage.Date.Day.ToString().PadLeft(2, '0'), currentBingImage.Date.Month.ToString().PadLeft(2, '0'), currentBingImage.Date.Year);

            Bing_OpenBingImageCopyrightLink.ToolTip = currentBingImage.Copyright;
            if (Bing_BingImageInfoSwitchMenuItem.IsChecked) Console.WriteLine(currentBingImage.Copyright);
        }

        private void Bing_DownloadImage(BingItem item, string resolution)
        {
            using (WebClient wc = new WebClient())
            {
                /* TODO: Async downloading */
                //wc.DownloadFile(new Uri("http://www.bing.com" + item.Url + "_" + res + ".jpg"), bingWallpaperDir + item.Name + "_" + res + ".jpg");
                wc.DownloadFile(new Uri(string.Format(bingImageUrl, item.Url, resolution)), bingWallpaperDir + item.Name + "_" + resolution + ".jpg");
            }
        }

        private void bing_imageLeftButtonClick(object sender, RoutedEventArgs e)
        {
            bIndex = (bIndex + 1 > bingWallpapers.Count - 1 ? 0 : bIndex + 1);
            Bing_UpdateImage();
        }

        private void bing_imageRightButtonClick(object sender, RoutedEventArgs e)
        {
            bIndex = ((bIndex - 1 < 0) ? bingWallpapers.Count - 1 : bIndex - 1);
            Bing_UpdateImage();
        }

        private void bing_downloadCurrentImage(object sender, RoutedEventArgs e)
        {
            var item = bingWallpapers.ElementAt(bIndex);
            string res = Bing_SelectDownloadResolution.Text;
            if (File.Exists(bingWallpaperDir + item.Name + "_" + res + ".jpg")) return;
            Bing_DownloadImage(item, res);
        }

        private void bing_setCurrentImageAsBackground(object sender, RoutedEventArgs e)
        {
            if (bingWallpapers.Count <= 0) return;
            var item = bingWallpapers.ElementAt(bIndex);
            string res = Bing_SelectDownloadResolution.Text;
            if (!File.Exists(bingWallpaperDir + item.Name + "_" + res + ".jpg")) Bing_DownloadImage(item, res);
            WallpaperHandler.Set(bingWallpaperDir + item.Name + "_" + res + ".jpg", WallpaperHandler.Style.Fill, backgroundColor);
        }

        private void bing_openBingImageCopyrightLink(object sender, RoutedEventArgs e)
        {
            if (currentBingImage != null) Process.Start(currentBingImage.CopyrightLink);
        }

        private void bing_openWallpaperFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(bingWallpaperDir);
        }
    }
}