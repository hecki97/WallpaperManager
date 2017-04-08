using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using WallpaperManager.Commands;

namespace WallpaperManager.ViewModels
{
    class WeatherViewModel : INotifyPropertyChanged
    {
        public class WeatherDataGridItem
        {
            public bool IsEnabled { get; set; }
            public string Name { get; set; }
            //public string Author { get; set; }
            public string Count { get; set; }
            public string WarningSignVisibility { get; set; }
        }

        private string currentTemperature = "--.--°";
        private string currentWeather = "---";
        private string currentWeatherConditionCode = string.Empty;
        private int upcomingWallpaperIndex = 0;
        //Credits: Icon made by Freepik from www.flaticon.com 
        private BitmapImage currentWeatherIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/WeatherIcons/default.png"));
        private BitmapImage upcomingWallpaper = new BitmapImage(new Uri("pack://application:,,,/Resources/WeatherIcons/default.png"));
        private string defaultIcon = "pack://application:,,,/Resources/WeatherIcons/default.png";

        private ObservableCollection<WeatherDataGridItem> dgWeatherPackagesData = new ObservableCollection<WeatherDataGridItem>();

        private Dictionary<string, Dictionary<string, List<string>>> packagesDictionary = new Dictionary<string, Dictionary<string, List<string>>>();
        private Dictionary<string, List<string>> imagePoolDictionary = new Dictionary<string, List<string>>() { {"packages", new List<string>()} };

        private readonly DelegateCommand<string> btnOpenUpcomingWallpaper;
        private readonly DelegateCommand<string> btnShuffleUpcomingWallpapers;
        private readonly DelegateCommand<string> btnDisplayNextUpcomingWallpaper;
        private readonly ICommand btnLoadWeatherCommand;

        public WeatherViewModel()
        {
            btnLoadWeatherCommand = new AsyncCommand(() => LoadWeather());
            btnOpenUpcomingWallpaper = new DelegateCommand<string>((s) => OpenUpcomingPicture(), (s) => CanUseWallpapers());
            btnShuffleUpcomingWallpapers = new DelegateCommand<string>((s) => ShuffleList(), (s) => CanUseWallpapers());
            btnDisplayNextUpcomingWallpaper = new DelegateCommand<string>((s) => DisplayNextUpcomingWallpaper(s), (s) => CanUseWallpapers());
        }

        public ObservableCollection<WeatherDataGridItem> DgWeatherPackagesData
        {
            get { return dgWeatherPackagesData; }
        }

        public ICommand BtnLoadWeatherCommand
        {
            get { return btnLoadWeatherCommand; }
        }

        public DelegateCommand<string> BtnDisplayNextUpcomingWallpaperCommand
        {
            get { return btnDisplayNextUpcomingWallpaper; }
        }

        public DelegateCommand<string> BtnOpenUpcomingWallpaperCommand
        {
            get { return btnOpenUpcomingWallpaper; }
        }

        public DelegateCommand<string> BtnShuffleUpcomingWallpapersCommand
        {
            get { return btnShuffleUpcomingWallpapers; }
        }

        public string CurrentTemperature
        {
            get { return currentTemperature; }
            set
            {
                currentTemperature = value;
                OnPropertyChanged("CurrentTemperature");
            }
        }

        public string CurrentWeather
        {
            get { return currentWeather; }
            set
            {
                currentWeather = value;
                OnPropertyChanged("CurrentWeather");
            }
        }

        public BitmapImage CurrentWeatherIcon
        {
            get { return currentWeatherIcon; }
            set {
                currentWeatherIcon = value;
                OnPropertyChanged("CurrentWeatherIcon");
            }
        }

        public BitmapImage UpcomingWallpaper
        {
            get { return upcomingWallpaper; }
            set {
                upcomingWallpaper = value;
                OnPropertyChanged("UpcomingWallpaper");
            }
        }

        public async Task LoadWeather()
        {
            if (CanLoadWeather()) { await WeatherHelper.GetOpenWeatherDataAsync(); Console.WriteLine("Download!"); }

            XElement xEl = XElement.Load(WeatherHelper.openWeatherFilePath);
            List<WeatherDetails> weatherDetails = WeatherHelper.GetOpenWeatherInfo(xEl);

            CurrentTemperature = weatherDetails.First().Temperature;
            CurrentWeather = weatherDetails.First().Weather;
            CurrentWeatherIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/WeatherIcons/" + weatherDetails.First().WeatherIcon + ".png"));
            currentWeatherConditionCode = weatherDetails.First().WeatherConditionCode;

            Console.WriteLine(weatherDetails.First().WeatherConditionCode);
            TryToSelectUpcomingWallpaper();
        }

        private void TryToSelectUpcomingWallpaper()
        {
            if (imagePoolDictionary.Count <= 1 || UpcomingWallpaper.UriSource != new Uri(defaultIcon)) return;

            upcomingWallpaperIndex = 0;
            string upcomingWallpaperPath = string.Empty;

            if (char.GetNumericValue(currentWeatherConditionCode.ElementAt(2)) > 0 && imagePoolDictionary.ContainsKey(currentWeatherConditionCode))
                upcomingWallpaperPath = imagePoolDictionary[currentWeatherConditionCode].First();
            else if (char.GetNumericValue(currentWeatherConditionCode.ElementAt(1)) > 0 && imagePoolDictionary.ContainsKey(currentWeatherConditionCode.Remove(2, 1).Insert(2, "0")))
            {
                currentWeatherConditionCode = currentWeatherConditionCode.Remove(2, 1).Insert(2, "0");
                upcomingWallpaperPath = imagePoolDictionary[currentWeatherConditionCode].First();
            }   
            else if (imagePoolDictionary.ContainsKey(currentWeatherConditionCode.Remove(1, 2).Insert(1, "00")))
            {
                currentWeatherConditionCode = currentWeatherConditionCode.Remove(1, 2).Insert(1, "00");
                upcomingWallpaperPath = imagePoolDictionary[currentWeatherConditionCode].First();
            }
            
            if (!string.IsNullOrEmpty(upcomingWallpaperPath)) UpcomingWallpaper = new BitmapImage(new Uri(upcomingWallpaperPath));
        }

        public bool CanLoadWeather()
        {
            return ((!File.Exists(WeatherHelper.openWeatherFilePath)) || (DateTime.Now - File.GetLastWriteTime(WeatherHelper.openWeatherFilePath)) > new TimeSpan(0, 30, 0));
        }

        //TODO: Find better function name!
        public bool CanUseWallpapers()
        {
            return (imagePoolDictionary.ContainsKey(currentWeatherConditionCode) && imagePoolDictionary[currentWeatherConditionCode].Count > 0);
        }

        private void ShuffleList()
        {
            //TRY: Instead of just shuffling one category, shuffle all categories!
            imagePoolDictionary.ToList().ForEach(x => x.Value.Shuffle());
            TryToSelectUpcomingWallpaper();
            /*
            imagePoolDictionary[currentWeatherConditionCode].Shuffle();
            upcomingWallpaperIndex = 0;
            UpcomingWallpaper = new BitmapImage(new Uri(imagePoolDictionary[currentWeatherConditionCode].First()));
            */
        }

        private void DisplayNextUpcomingWallpaper(string direction)
        {
            if (direction == "right")
                upcomingWallpaperIndex = ((upcomingWallpaperIndex - 1 < 0) ? imagePoolDictionary[currentWeatherConditionCode].Count - 1 : upcomingWallpaperIndex - 1);
            else
                upcomingWallpaperIndex = (upcomingWallpaperIndex + 1 > imagePoolDictionary[currentWeatherConditionCode].Count - 1 ? 0 : upcomingWallpaperIndex + 1);

            UpcomingWallpaper = new BitmapImage(new Uri(imagePoolDictionary[currentWeatherConditionCode].ElementAtOrDefault(upcomingWallpaperIndex)));
        }

        private void OpenUpcomingPicture()
        {
            //if (UpcomingWallpaper.UriSource != null) Process.Start(UpcomingWallpaper.UriSource.LocalPath);
            Process.Start(UpcomingWallpaper.UriSource.LocalPath);
        }

        public void AddPackageToDataGrid(string[] filepaths)
        {
            foreach (string filepath in filepaths)
            {
                string packageName = Path.GetFileNameWithoutExtension(filepath);
                if (packagesDictionary.ContainsKey(packageName)) continue;
                Dictionary<string, List<string>> package = new Dictionary<string, List<string>>();
                int fileCount = 0;
                WeatherHelper.WeatherTreeScan(ref package, ref fileCount, filepath);
                if (fileCount <= 0) continue;
                packagesDictionary.Add(packageName, package);
                dgWeatherPackagesData.Add(new WeatherDataGridItem
                {
                    IsEnabled = false,
                    Name = packageName,
                    Count = fileCount + " item(s)",
                    WarningSignVisibility = "Visible" //"Hidden"
                });
            }
        }

        public void AddPackageToImagePool(bool? isChecked, string parentName)
        {
            if ((isChecked == true) && !imagePoolDictionary["packages"].Contains(parentName))
            {
                imagePoolDictionary["packages"].Add(parentName);
                packagesDictionary[parentName].ToList().ForEach(x => {
                    if (imagePoolDictionary.ContainsKey(x.Key))
                        imagePoolDictionary[x.Key].AddRange(x.Value);
                    else imagePoolDictionary.Add(x.Key, x.Value);
                });
            }
            else if ((isChecked == false) && imagePoolDictionary["packages"].Contains(parentName))
            {
                imagePoolDictionary["packages"].Remove(parentName);
                Dictionary<string, List<string>> package = packagesDictionary[parentName];
                foreach (string key in package.Keys)
                {
                    package[key].ToList().ForEach(x => imagePoolDictionary[key].Remove(x));
                }
            }
            btnDisplayNextUpcomingWallpaper.RaiseCanExecuteChanged();
            btnOpenUpcomingWallpaper.RaiseCanExecuteChanged();
            btnShuffleUpcomingWallpapers.RaiseCanExecuteChanged();

            TryToSelectUpcomingWallpaper();
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
