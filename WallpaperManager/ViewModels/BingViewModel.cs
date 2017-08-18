using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using WallpaperManager.Commands;

namespace WallpaperManager.ViewModels
{
    class BingViewModel : INotifyPropertyChanged
    {
        private List<BingWallpaper> bingWallpapers = new List<BingWallpaper>();
        private int currentBingWallpaperIndex = 0;
        private BingWallpaper currentBingWallpaper = new BingWallpaper();

        private readonly List<string> cbBingWallpaperDownloadResolutions = new List<string>() { "1920x1200", "1920x1080", "1366x768", "1280x720", "1024x768", "800x600", "768x1024", "720x1280", "640x480", "480x800" };
        private BitmapImage imgBingWallpaper = new BitmapImage(new Uri("pack://application:,,,/Resources/WeatherIcons/default.png"));

        //View Context Menu
        private bool cmBoolCircleWallpapersInView;
        private bool cmBoolUpdateWallpaperWhenChangingResolution;
        private bool cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet;
        private bool cmBoolOpenWallpaperAfterDownload;

        //View Labels
        private string strCurrentBingWallpaperDate = string.Empty;
        private string toolTipOpenCurrentBingWallpaperCopyrightLink = string.Empty;
        private string cbSelectedBingWallpaperDownloadResolution;

        //View Buttons
        private readonly ICommand btnLoadBingWallpapers;
        private readonly ICommand btnDownloadCurrentBingWallpaper;
        private readonly DelegateCommand<string> btnOpenBingWallpaperDir;
        private readonly DelegateCommand<string> btnDisplayNextBingWallpaperLeft;
        private readonly DelegateCommand<string> btnDisplayNextBingWallpaperRight;
        //private readonly DelegateCommand<string> btnOpenCurrentBingWallpaper;
        private readonly DelegateCommand<string> btnOpenCurrentBingWallpaperCopyrightLink;

        public BingViewModel()
        {
            //TODO: Add support for more cultures
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            MainWindow.ApplicationShutdownEvent += MainWindow_ApplicationShutdownEvent;

            //Test
            //MidnightNotifier.DayChanged += async (s, e) => { Console.WriteLine("It's midnight!"); await LoadBingWallpapers(); };

            cbSelectedBingWallpaperDownloadResolution = Properties.Settings.Default.CbBingSelectedDownloadResolution;
            
            cmBoolUpdateWallpaperWhenChangingResolution = Properties.Settings.Default.CmBoolUpdateWallpaperWhenChangingResolution;
            cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet = Properties.Settings.Default.CmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet;
            cmBoolCircleWallpapersInView = Properties.Settings.Default.CmBoolCircleWallpapersInView;
            cmBoolOpenWallpaperAfterDownload = Properties.Settings.Default.CmBoolOpenWallpaperAfterDownload;

            //Current Resolution
            Console.WriteLine(SystemParameters.PrimaryScreenWidth + "x" + SystemParameters.PrimaryScreenHeight);

            DevTyr.Mvvm.Messaging.Messenger.Instance.Register<WallpaperSettings>(this, "BingSetWallpaper", SetWallpaper);

            if (!Directory.Exists(MainWindow.applicationDataPath + @"\bing")) Directory.CreateDirectory(MainWindow.applicationDataPath + @"\bing");
            if (!Directory.Exists(MainWindow.applicationDataPath + @"\bing\wallpaper")) Directory.CreateDirectory(MainWindow.applicationDataPath + @"\bing\archive");
            //Obsolete
            if (!Directory.Exists(MainWindow.applicationDataPath + @"\bing\wallpaper")) Directory.CreateDirectory(MainWindow.applicationDataPath + @"\bing\wallpaper");
            if (!Directory.Exists(MainWindow.applicationDataPath + @"\bing\thumbnails")) Directory.CreateDirectory(MainWindow.applicationDataPath + @"\bing\thumbnails");

            btnLoadBingWallpapers = new AsyncCommand(() => LoadBingWallpapers());
            btnDownloadCurrentBingWallpaper = new AsyncCommand(() => DownloadCurrentBingWallpaper(), () => { return bingWallpapers.Count > 0; });
            btnOpenBingWallpaperDir = new DelegateCommand<string>((s) => OpenBingWallpaperDir());
            //btnOpenCurrentBingWallpaper = new DelegateCommand<string>((s) => OpenCurrentBingImage(), (s) => { return ImgBingWallpaper.UriSource != null; });
            btnDisplayNextBingWallpaperLeft = new DelegateCommand<string>((s) => DisplayNextBingWallpaper("left"), (s) => { return bingWallpapers.Count > 0 && (cmBoolCircleWallpapersInView ? true : currentBingWallpaperIndex < bingWallpapers.Count - 1); });
            btnDisplayNextBingWallpaperRight = new DelegateCommand<string>((s) => DisplayNextBingWallpaper("right"), (s) => { return bingWallpapers.Count > 0 && (cmBoolCircleWallpapersInView ? true : currentBingWallpaperIndex > 0); });
            btnOpenCurrentBingWallpaperCopyrightLink = new DelegateCommand<string>((s) => OpenCurrentBingWallpaperCopyrightLink(), (s) => { return bingWallpapers.Count > 0; });
        }

        private void MainWindow_ApplicationShutdownEvent()
        {
            Properties.Settings.Default.CbBingSelectedDownloadResolution = cbSelectedBingWallpaperDownloadResolution;
            Properties.Settings.Default.CmBoolUpdateWallpaperWhenChangingResolution = cmBoolUpdateWallpaperWhenChangingResolution;
            Properties.Settings.Default.CmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet = cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet;
            Properties.Settings.Default.CmBoolCircleWallpapersInView = cmBoolCircleWallpapersInView;
            Properties.Settings.Default.CmBoolOpenWallpaperAfterDownload = cmBoolOpenWallpaperAfterDownload;
        }

        public ICommand BtnLoadBingWallpapersCommand => btnLoadBingWallpapers;
        public ICommand BtnDownloadCurrentBingWallpaperCommand => btnDownloadCurrentBingWallpaper;
        public DelegateCommand<string> BtnOpenBingWallpaperDirCommand => btnOpenBingWallpaperDir;
        //public DelegateCommand<string> BtnOpenCurrentBingWallpaperCommand => btnOpenCurrentBingWallpaper;
        public DelegateCommand<string> BtnDisplayNextBingWallpaperLeftCommand => btnDisplayNextBingWallpaperLeft;
        public DelegateCommand<string> BtnDisplayNextBingWallpaperRightCommand => btnDisplayNextBingWallpaperRight;
        public DelegateCommand<string> BtnOpenCurrentBingWallpaperCopyrightLinkCommand => btnOpenCurrentBingWallpaperCopyrightLink;

        public List<string> CbBingWallpaperDownloadResolutions => cbBingWallpaperDownloadResolutions;
        public bool WallpaperFoundOnDisk => currentBingWallpaper.WallpaperFoundOnDisk;
        public bool IsGrayscaleEffectActive => cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet && !currentBingWallpaper.WallpaperFoundOnDisk;

        public BitmapImage ImgBingWallpaper
        {
            get => imgBingWallpaper;
            set {
                imgBingWallpaper = value;
                OnPropertyChanged("ImgBingWallpaper");
            }
        }

        public bool CmBoolUpdateWallpaperWhenChangingResolution
        {
            get => cmBoolUpdateWallpaperWhenChangingResolution;
            set
            {
                cmBoolUpdateWallpaperWhenChangingResolution = value;
                UpdateBingWallpaper(currentBingWallpaper);
                OnPropertyChanged("CmBoolUpdateWallpaperWhenChangingResolution");
                OnPropertyChanged("IsGrayscaleEffectActive");
            }
        }

        public bool CmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet
        {
            get => cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet;
            set
            {
                cmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet = value;
                OnPropertyChanged("CmBoolApplyShaderWhenWallpaperHasNotBeenDownloadedYet");
                OnPropertyChanged("IsGrayscaleEffectActive");
            }
        }

        public bool CmBoolCircleWallpapersInView
        {
            get => cmBoolCircleWallpapersInView;
            set
            {
                cmBoolCircleWallpapersInView = value;
                BtnDisplayNextBingWallpaperLeftCommand.RaiseCanExecuteChanged();
                BtnDisplayNextBingWallpaperRightCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("CmBoolCircleWallpapersInView");
            }
        }

        public bool CmBoolOpenWallpaperAfterDownload
        {
            get => cmBoolOpenWallpaperAfterDownload;
            set
            {
                cmBoolOpenWallpaperAfterDownload = value;
                OnPropertyChanged("CmBoolOpenWallpaperAfterDownload");
            }
        }

        public string StrCurrentBingWallpaperDate
        {
            get => strCurrentBingWallpaperDate;
            set
            {
                strCurrentBingWallpaperDate = value;
                OnPropertyChanged("StrCurrentBingWallpaperDate");
            }
        }

        public string CbSelectedBingWallpaperDonwloadResolution
        {
            get => cbSelectedBingWallpaperDownloadResolution;
            set
            {
                cbSelectedBingWallpaperDownloadResolution = value;

                currentBingWallpaper.UpdateFilePath(cbSelectedBingWallpaperDownloadResolution);
                if (cmBoolUpdateWallpaperWhenChangingResolution) UpdateBingWallpaper(currentBingWallpaper);
                OnPropertyChanged("CbSelectedBingWallpaperDonwloadResolution");
                OnPropertyChanged("IsGrayscaleEffectActive");
                OnPropertyChanged("WallpaperFoundOnDisk");
            }
        }

        public string BtnOpenBingWallpaperCopyrightLinkToolTip
        {
            get => toolTipOpenCurrentBingWallpaperCopyrightLink;
            set
            {
                toolTipOpenCurrentBingWallpaperCopyrightLink = value;
                OnPropertyChanged("BtnOpenBingWallpaperCopyrightLinkToolTip");
            }
        }

        private void UpdateBingWallpaper(BingWallpaper bingWallpaper)
        {
            bingWallpaper.UpdateFilePath(cbSelectedBingWallpaperDownloadResolution);
            currentBingWallpaper = bingWallpaper;
            OnPropertyChanged("IsGrayscaleEffectActive");
            OnPropertyChanged("WallpaperFoundOnDisk");

            //TODO: More refinements
            if (currentBingWallpaper.WallpaperFoundOnDisk && cmBoolUpdateWallpaperWhenChangingResolution)
                ImgBingWallpaper = new BitmapImage(new Uri(currentBingWallpaper.FilePath));
            else
                if (!ImgBingWallpaper.UriSource.Equals(new Uri(currentBingWallpaper.ThumbnailPath))) ImgBingWallpaper = new BitmapImage(new Uri(currentBingWallpaper.ThumbnailPath));

            StrCurrentBingWallpaperDate = currentBingWallpaper.Date.ToString("(ddd.) dd.MM.yy");
            BtnOpenBingWallpaperCopyrightLinkToolTip = currentBingWallpaper.Copyright;
        }

        private async Task LoadBingWallpapers()
        {
            if (CanLoadBingWallpapers()) { await BingHelper.DownloadXMLAsync(); Console.WriteLine("Download!"); }

            XElement xEl = XElement.Load(BingHelper.bingXMLFile);
            bingWallpapers = BingHelper.GetBingXmlInfo(xEl);
            await BingHelper.DownloadMultipleThumbnailsAsync(bingWallpapers);

            UpdateBingWallpaper(bingWallpapers.First());

            if (bingWallpapers.Count > 0)
            {
                btnDisplayNextBingWallpaperLeft.RaiseCanExecuteChanged();
                btnDisplayNextBingWallpaperRight.RaiseCanExecuteChanged();

                btnOpenCurrentBingWallpaperCopyrightLink.RaiseCanExecuteChanged();
            }
        }

        private async Task DownloadCurrentBingWallpaper()
        {
            if (currentBingWallpaper.WallpaperFoundOnDisk) { Process.Start(currentBingWallpaper.FilePath); return; } 

            switch (await BingHelper.DownloadWallpaperAsync(currentBingWallpaper, cbSelectedBingWallpaperDownloadResolution))
            {
                case 0:
                    currentBingWallpaper.CheckIfWallpaperExistsOnDisk();
                    OnPropertyChanged("WallpaperFoundOnDisk");
                    OnPropertyChanged("IsGrayscaleEffectActive");
                    if (cmBoolOpenWallpaperAfterDownload) Process.Start(currentBingWallpaper.FilePath);
                    if (cmBoolUpdateWallpaperWhenChangingResolution) UpdateBingWallpaper(currentBingWallpaper);
                    break;
                case 1:
                    MessageBox.Show(string.Format("Wallpaper not available in selected resolution ({0}). Please try again with a different resolution.", cbSelectedBingWallpaperDownloadResolution), "Error! Wallpaper not found", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                default:
                    MessageBox.Show("Sorry, something seems to have gone incredibly wrong. Please try again!", "Error! Something went wrong!", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void OpenBingWallpaperDir()
        {
            string currentBingWallpaperDir = BingHelper.bingWallpaperDir + currentBingWallpaper.Name;
            Process.Start(Directory.Exists(currentBingWallpaperDir) ? currentBingWallpaperDir : BingHelper.bingWallpaperDir);
        }

        /*
        private void OpenCurrentBingImage()
        {
            if (File.Exists(currentBingWallpaper.FilePath)) Process.Start(currentBingWallpaper.FilePath);
        }
        */

        private void DisplayNextBingWallpaper(string direction)
        {
            if (direction == "right")
            {
                if (CmBoolCircleWallpapersInView)
                    currentBingWallpaperIndex = (currentBingWallpaperIndex - 1 < 0) ? bingWallpapers.Count - 1 : currentBingWallpaperIndex - 1;
                else
                    currentBingWallpaperIndex = (currentBingWallpaperIndex - 1 < 0) ? 0 : currentBingWallpaperIndex - 1;
            }
            else
            {
                if (CmBoolCircleWallpapersInView)
                    currentBingWallpaperIndex = (currentBingWallpaperIndex + 1 > bingWallpapers.Count - 1 ? 0 : currentBingWallpaperIndex + 1);
                else
                    currentBingWallpaperIndex = (currentBingWallpaperIndex + 1 > bingWallpapers.Count - 1 ? bingWallpapers.Count : currentBingWallpaperIndex + 1);
            }

            BtnDisplayNextBingWallpaperLeftCommand.RaiseCanExecuteChanged();
            BtnDisplayNextBingWallpaperRightCommand.RaiseCanExecuteChanged();

            UpdateBingWallpaper(bingWallpapers.ElementAtOrDefault(currentBingWallpaperIndex));
        }

        private void OpenCurrentBingWallpaperCopyrightLink()
        {
            Process.Start(currentBingWallpaper.CopyrightLink);
        }

        private bool CanLoadBingWallpapers()
        {
            //TODO: Download file after a new day has started and not after 24 hours
            //return ((!File.Exists(BingHelper.bingXMLFile)) || (DateTime.Now - File.GetLastWriteTime(BingHelper.bingXMLFile)) > new TimeSpan(24, 0, 0));
            return ((!File.Exists(BingHelper.bingXMLFile)) || DateTime.Now.Day > File.GetLastWriteTime(BingHelper.bingXMLFile).Day || DateTime.Now.Month > File.GetLastWriteTime(BingHelper.bingXMLFile).Month || DateTime.Now.Year > File.GetLastWriteTime(BingHelper.bingXMLFile).Year);
        }

        private void SetWallpaper(WallpaperSettings wallpaperSettings)
        {
            Debug.WriteLine("Set Wallpaper!");
            BingHelper.SetBingWallpaper(currentBingWallpaper, cbSelectedBingWallpaperDownloadResolution, wallpaperSettings);

            currentBingWallpaper.CheckIfWallpaperExistsOnDisk();
            OnPropertyChanged("WallpaperFoundOnDisk");
            OnPropertyChanged("IsGrayscaleEffectActive");
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
