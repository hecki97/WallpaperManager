using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WallpaperManager.Commands;
using DevTyr.Mvvm.Messaging;
using System.Windows.Media;

namespace WallpaperManager.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        public enum BackgroundType : int
        {
            SolidColor,
            Picture,
            Slideshow
        }

        public enum Instance : int
        {
            Shared,
            Custom,
            Bing,
            Weather
        }

        //TODO: Implement Slideshow first
        //private readonly Dictionary<string, BackgroundType> cbMainWindowBackgroundType = new Dictionary<string, BackgroundType>() { { "Solid Color", BackgroundType.SolidColor }, { "Picture", BackgroundType.Picture }, { "Slideshow", BackgroundType.Slideshow } };
        private readonly Dictionary<string, BackgroundType> cbMainWindowBackgroundType = new Dictionary<string, BackgroundType>() { { "Solid Color", BackgroundType.SolidColor }, { "Picture", BackgroundType.Picture } };
        private readonly Dictionary<string, WallpaperHandler.Style> cbMainWindowWallpaperStyle = new Dictionary<string, WallpaperHandler.Style>() { { "Fill", WallpaperHandler.Style.Fill }, { "Fit", WallpaperHandler.Style.Fit }, { "Stretch", WallpaperHandler.Style.Stretch }, { "Tile", WallpaperHandler.Style.Tile }, { "Centre", WallpaperHandler.Style.Centre }, { "Span", WallpaperHandler.Style.Span } };

        private string cbMainWindowSelectedBackgroundType;
        private string cbMainWindowSelectedWallpaperStyle;
        private string btnMainWindowSetWallpaperContent = "Set Solid Color";
        private string btnMainWindowSetWallpaperToolTip = string.Empty;
        private Color cpMainWindowSelectedBackgroundColor;

        private Color lastUsedBackgroundColor = new Color();

        private Instance instance;

        private readonly DelegateCommand<string> btnMainWindowSetWallpaper;

        public MainWindowViewModel()
        {
            MainWindow.ApplicationShutdownEvent += MainWindow_ApplicationShutdownEvent;

            //Messenger.Instance.Register<string>(this, "SharedCbMainWindowSelectedBackgroundType", CbMainWindowSelectedBackgroundType);

            btnMainWindowSetWallpaper = new DelegateCommand<string>((s) => SetWallpaper(s), (s) => CanSetWallpaper());
        }

        private void MainWindow_ApplicationShutdownEvent()
        {
            Properties.Settings.Default.CbMainWindowSelectedBackgroundType[(int)instance] = CbMainWindowSelectedBackgroundType;
            Properties.Settings.Default.CbMainWindowSelectedWallpaperStyle[(int)instance] = CbMainWindowSelectedWallpaperStyle;
            Properties.Settings.Default.CpMainWindowSelectedBackgroundColor[(int)instance] = CpMainWindowSelectedBackgroundColor.ToString();
        }

        public Instance SetActiveInstance
        {
            set
            {
                //TODO: Implement Global System & Implement Global Switch
                instance = Properties.Settings.Default.UseGlobalSettings ? Instance.Shared : value;
                cbMainWindowSelectedBackgroundType = Properties.Settings.Default.CbMainWindowSelectedBackgroundType[(int)instance];
                cbMainWindowSelectedWallpaperStyle = Properties.Settings.Default.CbMainWindowSelectedWallpaperStyle[(int)instance];
                cpMainWindowSelectedBackgroundColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.CpMainWindowSelectedBackgroundColor[(int)instance]);
            }
        }

        public DelegateCommand<string> BtnMainWindowSetWallpaperCommand => btnMainWindowSetWallpaper;

        public List<string> CbMainWindowBackgroundType => cbMainWindowBackgroundType.Keys.ToList();
        public List<string> CbMainWindowWallpaperStyle => cbMainWindowWallpaperStyle.Keys.ToList();

        public string CbMainWindowSelectedBackgroundType
        {
            get => cbMainWindowSelectedBackgroundType;
            set
            {
                cbMainWindowSelectedBackgroundType = value;
                OnPropertyChanged("CbMainWindowSelectedBackgroundType");

                //if (instance == Instance.Shared) Messenger.Instance.Send(value, "SharedCbMainWindowSelectedBackgroundType");

                OnPropertyChanged("BtnMainWindowSetWallpaperContent");
            }
        }

        public string CbMainWindowSelectedWallpaperStyle
        {
            get => cbMainWindowSelectedWallpaperStyle;
            set
            {
                cbMainWindowSelectedWallpaperStyle = value;
                OnPropertyChanged("CbMainWindowSelectedWallpaperStyle");

                //if (instance == Instance.Shared) Messenger.Instance.Send(value, "SharedCbMainWindowSelectedWallpaperStyle");

                btnMainWindowSetWallpaper.RaiseCanExecuteChanged();
                OnPropertyChanged("BtnMainWindowSetWallpaperToolTip");
            }
        }

        public Color CpMainWindowSelectedBackgroundColor
        {
            get => cpMainWindowSelectedBackgroundColor;
            set
            {
                cpMainWindowSelectedBackgroundColor = value;

                //if (instance == Instance.Shared) Messenger.Instance.Send(value, "SharedCpMainWindowSelectedBackgroundColor");

                OnPropertyChanged("CpMainWindowSelectedBackgroundColor");
            }
        }

        //TODO: Works for now, but needs to be reworked later
        public string BtnMainWindowSetWallpaperToolTip => CanSetWallpaper() ? BtnMainWindowSetWallpaperContent : "Sorry, this feature needs at least Windows 7 or newer";

        public string BtnMainWindowSetWallpaperContent
        {
            get {
                switch (cbMainWindowBackgroundType[cbMainWindowSelectedBackgroundType])
                {
                    case BackgroundType.SolidColor:
                        btnMainWindowSetWallpaperContent = "Set Color";
                        break;
                    case BackgroundType.Picture:
                        btnMainWindowSetWallpaperContent = "Set Wallpaper";
                        break;
                    case BackgroundType.Slideshow:
                        btnMainWindowSetWallpaperContent = "Start Slideshow";
                        break;
                }
                return btnMainWindowSetWallpaperContent;
            }
        }

        private void SetWallpaper(string mainWindowTab)
        {
            System.Drawing.Color currentBackgroundColor = System.Drawing.Color.FromArgb(255, cpMainWindowSelectedBackgroundColor.R, cpMainWindowSelectedBackgroundColor.G, cpMainWindowSelectedBackgroundColor.B);
            WallpaperSettings ws = new WallpaperSettings() { WallpaperStyle = cbMainWindowWallpaperStyle[cbMainWindowSelectedWallpaperStyle], BackgroundColor = currentBackgroundColor };
            switch (cbMainWindowBackgroundType[cbMainWindowSelectedBackgroundType])
            {
                case BackgroundType.Picture:
                    Messenger.Instance.Send(ws, mainWindowTab + "SetWallpaper");
                    break;
                case BackgroundType.SolidColor:
                    if (cpMainWindowSelectedBackgroundColor.Equals(lastUsedBackgroundColor)) return;
                    lastUsedBackgroundColor = cpMainWindowSelectedBackgroundColor;
                    WallpaperHandler.SetSolidColor(currentBackgroundColor);
                    break;
                //TODO: Implement Slideshow
                case BackgroundType.Slideshow:
                    break;
            }
        }

        private bool CanSetWallpaper()
        {
            WallpaperHandler.Style wallpaperStyle = cbMainWindowWallpaperStyle[cbMainWindowSelectedWallpaperStyle];
            return (wallpaperStyle == WallpaperHandler.Style.Fill || wallpaperStyle == WallpaperHandler.Style.Fit || wallpaperStyle == WallpaperHandler.Style.Span) ? Utilities.IsWin7OrHigher() : true;
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
