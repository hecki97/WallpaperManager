using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Xml;
using System.Windows.Media;

namespace WallpaperManager
{
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

    public class IntervalComboBoxItem
    {
        public string Key { get; set; }
        public int Value { get; set; }

        public IntervalComboBoxItem(string key, int value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public new enum Background : int 
        {
            SolidColor,
            Picture,
            Circle
        }
    
        private readonly List<string> supportedExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".GIF", ".PNG" };

        private int wallpaperIndex = 0;
        private WallpaperHandler.Style wallpaperStyle = WallpaperHandler.Style.Fill;
        private Background background = Background.SolidColor;

        private DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.SystemIdle);
        private long counter = 0;
        private int interval = 10;

        private ObservableCollection<DataGridItem> dataGridData = new ObservableCollection<DataGridItem>();
        private ObservableCollection<IntervalComboBoxItem> intervalComboBoxData1 = new ObservableCollection<IntervalComboBoxItem>() { {new IntervalComboBoxItem("10 Sec", 10)}, { new IntervalComboBoxItem("15 Sec", 15)}, {new IntervalComboBoxItem("1 Min", 60)}, {new IntervalComboBoxItem("5 Mins", 300)} };

        private ObservableCollection<Tuple<string, int>> intervalComboBoxData = new ObservableCollection<Tuple<string, int>>() { new Tuple<string, int>("15 Sec", 15), new Tuple<string, int>("30 Sec", 30), new Tuple<string, int>("1 Min", 60), new Tuple<string, int>("5 Min", 300), new Tuple<string, int>("15 Min", 900)};

        private string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WallpaperManager";

        private DataGridItem lastItem = new DataGridItem();

        private int dirCount = 0; private int fileCount = 0; private int timeCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            dataGrid.ItemsSource = dataGridData;
            IntervalComboBox.DataContext = intervalComboBoxData;

            //Save and retrieve last selected Item
            interval = intervalComboBoxData[0].Item2;
            wallpaperStyle = 0;
            background = 0;

            // TODO: Write more userfriendly GUI
            /*
            if (Utilities.IsWin7OrHigher())
            {
                ((ComboBoxItem)WPStyleComboBox.Items[0]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[0]).ToolTip = "This wallpaperstyle needs Win7 or higher";
                ((ComboBoxItem)WPStyleComboBox.Items[1]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[1]).ToolTip = "This wallpaperstyle needs Win7 or higher";
                ((ComboBoxItem)WPStyleComboBox.Items[5]).IsEnabled = false;
                ((ComboBoxItem)WPStyleComboBox.Items[5]).ToolTip = "This wallpaperstyle needs Win7 or higher";
            }
            */

            // TODO: Save and retrieve values from config files
            /*
            if (!Directory.Exists(applicationDataPath)) Directory.CreateDirectory(applicationDataPath);
            if (!Directory.Exists(applicationDataPath + @"\bing")) Directory.CreateDirectory(applicationDataPath + @"\bing");
            XmlDocument xml = (File.Exists(applicationDataPath + @"\settings.xml")) ? XMLHelper.LoadXmlDocument(applicationDataPath + @"\settings.xml") : XMLHelper.CreateXmlDocument(applicationDataPath, "settings");

            Dictionary<string, Dictionary<string, string>> settingsDictionary = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> customSettingsList = new Dictionary<string, string>();
            if (xml.ChildNodes.Count > 0)
            {
                settingsDictionary = XMLHelper.GetAttributesByTagName(xml, "custom", new string[] { "wallpaperStyle", "solidColor", "wallpaperIndex" });
                if (settingsDictionary.ContainsKey("custom")) customSettingsList = settingsDictionary["custom"];
            }

            if (customSettingsList.ContainsKey("wallpaperStyle") && !string.IsNullOrWhiteSpace(customSettingsList["wallpaperStyle"]) && GetEnumStringEnumType<WallpaperHandler.Style>(customSettingsList["wallpaperStyle"]))
            {
                wallpaperStyle = (customSettingsList.ContainsKey("wallpaperStyle") && !string.IsNullOrWhiteSpace(customSettingsList["wallpaperStyle"])) ? (WallpaperHandler.Style)Enum.Parse(typeof(WallpaperHandler.Style), customSettingsList["wallpaperStyle"], true) : WallpaperHandler.Style.Fill;
                WPStyleComboBox.SelectedValue = wallpaperStyle.ToString();
            }
            else
                DebugLog.Error("Wallpaper style attribute is missing or couldn't be parsed! Default settings will be used.");

            if (customSettingsList.ContainsKey("wallpaperIndex") && !string.IsNullOrWhiteSpace(customSettingsList["wallpaperIndex"]) && listBoxData.Count > 0)
            {
                wallpaperIndex = (int.Parse(customSettingsList["wallpaperIndex"]) % listBoxData.Count);
            }
            else
                DebugLog.Error("Wallpaper index attribute is missing, couldn't be parsed or the listBox has no elements! Default settings will be used.");

            if (customSettingsList.ContainsKey("solidColor") && !string.IsNullOrWhiteSpace(customSettingsList["solidColor"]))
            {
                string[] colorRgb = customSettingsList["solidColor"].Split('_');
                ColorPicker.SelectedColor = System.Windows.Media.Color.FromArgb(255, byte.Parse(colorRgb[0]), byte.Parse(colorRgb[1]), byte.Parse(colorRgb[2]));
            }
            */

            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Start();
            if (background == Background.Picture || background == Background.SolidColor) dispatcherTimer.Stop();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (dataGridData.Count > 0 && background != Background.Picture && background != Background.SolidColor)
            {
                counter++;
                DebugLog.Log(counter.ToString());
                if ((counter % interval) == 0)
                {
                    counter = 0;
                    UpdateWallpaper();
                    DebugLog.Log("Wallpaper was updated!");
                }
            }
        }

        private void switchTheme_Click(object sender, RoutedEventArgs e)
        {
            Tuple<AppTheme, Accent> theme = ThemeManager.DetectAppStyle(Application.Current);
            AppTheme invertedAppTheme = ThemeManager.GetInverseAppTheme(theme.Item1);
            ThemeManager.ChangeAppStyle(Application.Current, theme.Item2, invertedAppTheme);
            switchTheme.Content = ((invertedAppTheme.Name == "BaseLight") ? "Dark" : "Light") + " Theme";
        }

        /*
        private void cm_exit_button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void cm_nextWallpaper_button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cm_interval_button_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, int> interval = new Dictionary<string, int>();
            interval.Add("1 Min", 1);
            interval.Add("5 Min", 5);
            interval.Add("1 Hour", 60);
            interval.Add("4 Hours", 240);
            interval.Add("Never", -1);

            var element = interval.ElementAt(intervalCount % interval.Count);
            cm_interval_button.Header = "Interval: " + element.Key;
            intervalCount++;
        }

        private void cm_restore_button_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                ShowInTaskbar = true;
            }
        }
        */

        private void ButtonOpenDir_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog objDialog = new System.Windows.Forms.FolderBrowserDialog();
            objDialog.ShowDialog();
            if (!string.IsNullOrWhiteSpace(objDialog.SelectedPath)) {
                TreeScan(objDialog.SelectedPath);
            } else {
                DebugLog.Warning("No directory selected!");
            }
            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void ButtonAddFile_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.bmp;*.gif;*.png)|*.jpg;*.jpeg;*.bmp;*.gif;*.png;|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (openFileDialog.ShowDialog() == true)
            {
                AddFilesToList(openFileDialog.FileNames);
            }
            else
                DebugLog.Log("No file(s) selected!");
            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void dataGrid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                List<string> filepathList = new List<string>((string[])e.Data.GetData(DataFormats.FileDrop, true));
                if (filepathList.Count > 0)
                {
                    List<string> tmp_list = new List<string>();
                    foreach (string filepath in filepathList)
                    {
                        if (Directory.Exists(filepath))
                            TreeScan(filepath);
                        else if (File.Exists(filepath))
                            AddFilesToList(new[] { filepath });
                    }
                    UpdateGUI();
                    UpdateDataGridGUI();
                }
                else
                    DebugLog.Log("There were no files in filedrop!");
            }
        }

        private void AddFilesToList(string[] files)
        {
            if (files == null || (!files.Any())) { DebugLog.Warning("No file(s) were selected!"); return; }

           foreach (string file in files) {
                if (string.IsNullOrWhiteSpace(file)) continue;
                if (supportedExtensions.Contains(Path.GetExtension(file).ToUpperInvariant())) {
                    var img = System.Drawing.Image.FromFile(file);
                    DataGridItem item = new DataGridItem(true, Path.GetFileNameWithoutExtension(file), file, string.Format("{0}x{1}", img.Width, img.Height), Utilities.GetFileSizeOnDisk(file), Path.GetExtension(file).Substring(1).ToUpper());
                    if (!dataGridData.Contains(item))
                        dataGridData.Add(item);
                    else
                        DebugLog.Warning("File '" + file + "' is already in list!");
                } else {
                    DebugLog.Warning("File '" + file + "' has an unsupported file extension!");
                }
            }
        }

        private void ButtonClearList_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridData.Count <= 0) return;
            dataGridData.Clear();
            UpdateGUI();
            UpdateDataGridGUI();
        }

        private void TreeScan(string path)
        {
            AddFilesToList(Directory.GetFiles(path));
            
            //Loop trough each directory
            foreach (string dir in Directory.GetDirectories(path))
            {
                try {
                    TreeScan(dir); //Recursive call to get subdirs
                }
                catch (UnauthorizedAccessException e) {
                    DebugLog.Error(e.Message);
                    continue;
                }
            }
        }

        private void dataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (background == Background.SolidColor) background = Background.Picture;
            DataGridItem item = (DataGridItem)dataGrid.SelectedItem;
            if (item != null && !string.IsNullOrWhiteSpace(item.Path))
            {
                if (!lastItem.Path.Equals(item.Path) && item.IsEnabled)
                {
                    var wmcolor = ColorPicker.SelectedColor.Value;
                    WallpaperHandler.Set(item.Path, wallpaperStyle, System.Drawing.Color.FromArgb(wmcolor.A, wmcolor.R, wmcolor.G, wmcolor.B));
                    wallpaperIndex = (dataGrid.SelectedIndex + 1) % dataGrid.Items.Count;

                    //When double clicking on an item the timer will be reset to zero
                    counter = 0;

                    lastItem = item;
                    UpdateGUI();
                }
                else
                    DebugLog.Warning("Item '" + item.Path + "' already set as wallpaper!");
            }
            else
                DebugLog.Error("Item couldn't be selected because it is null or the path is missing!");
        }

        private void ButtonChangeWP_Click(object sender, RoutedEventArgs e)
        {
            if (background == Background.SolidColor)
            {
                var wmcolor = ColorPicker.SelectedColor.Value;
                WallpaperHandler.SetSolidColor(System.Drawing.Color.FromArgb(wmcolor.A, wmcolor.R, wmcolor.G, wmcolor.B));
            }
            else
                UpdateWallpaper();
        }

        private void WPStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            wallpaperStyle = (WallpaperHandler.Style) Enum.Parse(typeof(WallpaperHandler.Style), WPStyleComboBox.SelectedValue.ToString(), true);
        }

        private void IntervalComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            interval = ((Tuple<string,int>)IntervalComboBox.SelectedItem).Item2;
            counter = 0;
        }

        //TODO: Add support for 'IsEnabled' value and think about special edge cases (eg one item which is disabled -> Change Button deactivated in Picture Mode)
        private void UpdateWallpaper()
        {
            //if (background == Background.Circle && dataGridData.Count <= 1) return;
            if (dataGridData.Count > 0)
            {
                DataGridItem item = dataGridData.ElementAt(wallpaperIndex % dataGridData.Count);
                if (item != null && !string.IsNullOrWhiteSpace(item.Path))
                {
                    var wmcolor = ColorPicker.SelectedColor.Value;
                    WallpaperHandler.Set(item.Path, wallpaperStyle, System.Drawing.Color.FromArgb(wmcolor.A, wmcolor.R, wmcolor.G, wmcolor.B));
                    wallpaperIndex = (wallpaperIndex + 1) % dataGridData.Count;
                }
            }
        }

        private void UpdateGUI()
        {
            UpdateButton(ButtonClearList);
            //TODO: Special cases for ButtonChangeWP
            UpdateButton(ButtonChangeWP);
            UpdateButton(ButtonShuffleList);
            IntervalComboBox.IsEnabled = !(background == Background.Picture || background == Background.SolidColor);
            WPStyleComboBox.IsEnabled = !(background == Background.SolidColor);

            BackgroundComboBox.SelectedIndex = (int)background;

            // Disable specific Items on specific datagriddata count
            //((ComboBoxItem)BackgroundComboBox.Items[2]).IsEnabled = (dataGridData.Count > 1) ? true : false;
            //((ComboBoxItem)BackgroundComboBox.Items[1]).IsEnabled = (dataGridData.Count > 0) ? true : false;
            //TODO: Find better way to determine last selected item when datagriddata count <= 1 
            //if (dataGridData.Count == 0) BackgroundComboBox.SelectedIndex = 0;

            //Special Case
            if (background == Background.SolidColor && !ButtonChangeWP.IsEnabled) { ButtonChangeWP.IsEnabled = true; ButtonChangeWP.Opacity = 1; }
        }

        private void UpdateButton(Button button)
        {
            button.IsEnabled = (dataGridData.Count > 0);
            button.Opacity = (button.IsEnabled) ? 1 : .5;   
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

        private bool GetEnumStringEnumType<TEnum>(string testString) where TEnum : struct
        { 
            TEnum testEnum = default(TEnum);
            return (Enum.TryParse(testString, true, out testEnum) && Enum.IsDefined(typeof(TEnum), testEnum));
        }

        private void BackgroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            background = (Background)BackgroundComboBox.SelectedIndex;
            if (dispatcherTimer.IsEnabled) { dispatcherTimer.Stop(); counter = 0; }
            if (background == Background.Circle) dispatcherTimer.Start();
            if (background == Background.SolidColor) lastItem = new DataGridItem();
            UpdateGUI();
        }

        private void ButtonShuffleList_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.UnselectAllCells();
            Utilities.ClearSort(dataGrid);
            dataGridData.Shuffle();
        }

        private void ImageButton_Click(object sender, RoutedEventArgs e)
        {
            int index = (DataGridHelper.TryFindParent<DataGridRow>((Button)sender)).GetIndex();

            if (dataGridData.ElementAt(index) != null)
                System.Diagnostics.Process.Start(dataGridData[index].Path);
            else
                DebugLog.Error("The Item which should be opened couldn't be found");
        }

        private bool datagridHasItems = true;

        private void UpdateDataGridGUI()
        {
            if (dataGrid.HasItems == datagridHasItems) return;
            datagridHasItems = dataGrid.HasItems;

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                dataGrid.Columns[i].Visibility = (dataGrid.HasItems) ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}
