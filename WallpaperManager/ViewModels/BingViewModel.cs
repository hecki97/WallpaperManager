using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private BingWallpaper currentBingWallpaper;

        private readonly List<string> cbBingWallpaperDownloadResolutions = new List<string>() { "1920x1200", "1920x1080", "1366x768", "1280x720", "1024x768", "800x600", "768x1024", "720x1280", "640x480", "480x800" };

        //private List<string> cbMainWindowBackgroundType;
        //private List<string> cbMainWindowWallpaperStyle;

        private BitmapImage imgBingWallpaper = new BitmapImage(new Uri("pack://application:,,,/Resources/WeatherIcons/default.png"));
        private bool boolCircleBingWallpapers = true;
        private bool boolOpenWallpaperAfterDownload = true;
        private string strCurrentBingWallpaperDate = string.Empty;
        private string toolTipOpenCurrentBingWallpaperCopyrightLink = string.Empty;
        private string cbSelectedBingWallpaperDownloadResolution;

        private readonly ICommand btnLoadBingWallpapers;
        private readonly ICommand btnDownloadCurrentBingWallpaper;
        private readonly DelegateCommand<string> btnOpenBingWallpapersDir;
        private readonly DelegateCommand<string> btnDisplayNextBingWallpaper;
        private readonly DelegateCommand<string> btnOpenCurrentBingWallpaper;
        private readonly DelegateCommand<string> btnOpenCurrentBingWallpaperCopyrightLink;

        public BingViewModel()
        {
            //TODO: Add support for more cultures
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");            

            //TODO: Save last used value and restore on startup
            cbSelectedBingWallpaperDownloadResolution = cbBingWallpaperDownloadResolutions.First();
            
            //Current Resolution
            Console.WriteLine(System.Windows.SystemParameters.PrimaryScreenWidth + "x" + System.Windows.SystemParameters.PrimaryScreenHeight);

            btnLoadBingWallpapers = new AsyncCommand(() => LoadBingWallpapers());
            btnDownloadCurrentBingWallpaper = new AsyncCommand(() => DownloadCurrentBingWallpaper(), () => { return bingWallpapers.Count > 0; });
            btnOpenBingWallpapersDir = new DelegateCommand<string>((s) => OpenBingImagesDir());
            btnOpenCurrentBingWallpaper = new DelegateCommand<string>((s) => OpenCurrentBingImage(), (s) => { return ImgBingWallpaper.UriSource != null; });
            btnDisplayNextBingWallpaper = new DelegateCommand<string>((s) => DisplayNextBingWallpaper(s), (s) => { return bingWallpapers.Count > 0; });
            btnOpenCurrentBingWallpaperCopyrightLink = new DelegateCommand<string>((s) => OpenCurrentBingWallpaperCopyrightLink(), (s) => { return bingWallpapers.Count > 0; });
        }

        public ICommand BtnLoadBingWallpapersCommand
        {
            get { return btnLoadBingWallpapers; }
        }

        public ICommand BtnDownloadCurrentBingWallpaperCommand
        {
            get { return btnDownloadCurrentBingWallpaper; }
        }

        public DelegateCommand<string> BtnOpenBingWallpapersDirCommand
        {
            get { return btnOpenBingWallpapersDir; }
        }

        public DelegateCommand<string> BtnOpenCurrentBingWallpaperCommand
        {
            get { return btnOpenCurrentBingWallpaper; }
        }

        public DelegateCommand<string> BtnDisplayNextBingWallpaperCommand
        {
            get { return btnDisplayNextBingWallpaper; }
        }

        public DelegateCommand<string> BtnOpenCurrentBingWallpaperCopyrightLinkCommand
        {
            get { return btnOpenCurrentBingWallpaperCopyrightLink; }
        }

        public List<string> CbBingWallpaperDownloadResolutions
        {
            get { return cbBingWallpaperDownloadResolutions; }
        }

        /*
        public List<string> CbMainWindowBackgroundType
        {
            get { return cbMainWindowBackgroundType; }
        }

        public List<string> CbMainWindowWallpaperStyle
        {
            get { return cbMainWindowWallpaperStyle; }
        }
        */

        public BitmapImage ImgBingWallpaper
        {
            get { return imgBingWallpaper; }
            set {
                imgBingWallpaper = value;
                OnPropertyChanged("ImgBingWallpaper");
            }
        }

        public bool BoolCircleBingWallpapers
        {
            get { return boolCircleBingWallpapers; }
            set
            {
                boolCircleBingWallpapers = value;
                OnPropertyChanged("BoolCircleBingWallpapers");
            }
        }

        public bool BoolOpenWallpaperAfterDownload
        {
            get { return boolOpenWallpaperAfterDownload; }
            set
            {
                boolOpenWallpaperAfterDownload = value;
                OnPropertyChanged("BoolOpenWallpaperAfterDownload");
            }
        }

        public string StrCurrentBingWallpaperDate
        {
            get { return strCurrentBingWallpaperDate; }
            set
            {
                strCurrentBingWallpaperDate = value;
                OnPropertyChanged("StrCurrentBingWallpaperDate");
            }
        }

        public string CbSelectedBingWallpaperDonwloadResolution
        {
            get { return cbSelectedBingWallpaperDownloadResolution; }
            set
            {
                cbSelectedBingWallpaperDownloadResolution = value;
                OnPropertyChanged("CbSelectedBingWallpaperDonwloadResolution");
            }
        }

        public string BtnOpenBingWallpaperCopyrightLinkToolTip
        {
            get { return toolTipOpenCurrentBingWallpaperCopyrightLink; }
            set
            {
                toolTipOpenCurrentBingWallpaperCopyrightLink = value;
                OnPropertyChanged("BtnOpenBingWallpaperCopyrightLinkToolTip");
            }
        }

        private void UpdateBingWallpaper(BingWallpaper bingWallpaper)
        {
            currentBingWallpaper = bingWallpaper;
            ImgBingWallpaper = new BitmapImage(new Uri(bingWallpaper.FilePath));
            StrCurrentBingWallpaperDate = bingWallpaper.Date.ToString("(ddd.) dd.MM.yy");
            BtnOpenBingWallpaperCopyrightLinkToolTip = bingWallpaper.Copyright;
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
                BtnDisplayNextBingWallpaperCommand.RaiseCanExecuteChanged();
                BtnOpenCurrentBingWallpaperCopyrightLinkCommand.RaiseCanExecuteChanged();
            }
        }

        private async Task DownloadCurrentBingWallpaper()
        {
            await BingHelper.DownloadWallpaperAsync(currentBingWallpaper, cbSelectedBingWallpaperDownloadResolution);

            //TODO: Better implementation
            if (BoolOpenWallpaperAfterDownload) Process.Start(BingHelper.bingWallpaperDir + currentBingWallpaper.Name + "_" + cbSelectedBingWallpaperDownloadResolution + ".jpg");
        }

        private void OpenBingImagesDir()
        {
            Process.Start(BingHelper.bingWallpaperDir);
        }

        private void OpenCurrentBingImage()
        {
            Process.Start(ImgBingWallpaper.UriSource.LocalPath);
        }

        private void DisplayNextBingWallpaper(string direction)
        {
            if (direction == "right")
                currentBingWallpaperIndex = ((currentBingWallpaperIndex - 1 < 0) ? bingWallpapers.Count - 1 : currentBingWallpaperIndex - 1);
            else
                currentBingWallpaperIndex = (currentBingWallpaperIndex + 1 > bingWallpapers.Count - 1 ? 0 : currentBingWallpaperIndex + 1);

            UpdateBingWallpaper(bingWallpapers.ElementAtOrDefault(currentBingWallpaperIndex));
        }

        private void OpenCurrentBingWallpaperCopyrightLink()
        {
            Process.Start(currentBingWallpaper.CopyrightLink);
        }

        private bool CanLoadBingWallpapers()
        {
            Console.WriteLine("Hours: " + (DateTime.Now - File.GetLastWriteTime(BingHelper.bingXMLFile)).Hours);
            //TODO: Download file after a new day has started and not after 24 hours
            return ((!File.Exists(BingHelper.bingXMLFile)) || (DateTime.Now - File.GetLastWriteTime(BingHelper.bingXMLFile)) > new TimeSpan(24, 0, 0));
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
