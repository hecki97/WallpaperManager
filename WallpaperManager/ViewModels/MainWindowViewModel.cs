using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private readonly List<string> cbMainWindowBackgroundType = new List<string>() { "Solid Color", "Picture", "Slideshow" };
        private readonly List<string> cbMainWindowWallpaperStyle = new List<string>() { "Fill", "Fit", "Stretch", "Tile", "Centre", "Span" };

        public MainWindowViewModel()
        {

        }

        public List<string> CbMainWindowBackgroundType
        {
            get { return cbMainWindowBackgroundType; }
        }

        public List<string> CbMainWindowWallpaperStyle
        {
            get { return CbMainWindowWallpaperStyle; }
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
