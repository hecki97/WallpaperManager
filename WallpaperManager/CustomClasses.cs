using System;
using System.IO;

namespace WallpaperManager
{
    public class WeatherDetails
    {
        public string Weather { get; set; }
        public string WeatherIcon { get; set; }
        public string WeatherDay { get; set; }
        public string Temperature { get; set; }
        public string MaxTemperature { get; set; }
        public string MinTemperature { get; set; }
        public string WindDirection { get; set; }
        public string WindSpeed { get; set; }
        public string Humidity { get; set; }
        public string WeatherConditionCode { get; set; }
    }

    public class BingWallpaper
    {
        public DateTime Date { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Resolution { get; set; }
        public string Directory { get; set; }
        public string FilePath { get; set; }
        public string ThumbnailPath { get; set; }
        public string Copyright { get; set; }
        public string CopyrightLink { get; set; }

        protected bool wallpaperFoundOnDisk = false;

        public bool WallpaperFoundOnDisk
        {
            get => wallpaperFoundOnDisk;
        }

        public void CheckIfWallpaperExistsOnDisk()
        {
            wallpaperFoundOnDisk = File.Exists(FilePath);
            //Console.WriteLine("Found on disk: " + wallpaperFoundOnDisk);
        }

        public void UpdateFilePath(string resolution)
        {
            Resolution = resolution;
            FilePath = Directory + @"\" + Name + @"\" + Name + "_" + Resolution + ".jpg";
            CheckIfWallpaperExistsOnDisk();   
        }
    }

    public class WallpaperSettings
    {
        public WallpaperHandler.Style WallpaperStyle { get; set; }
        public System.Drawing.Color BackgroundColor { get; set; }
    }
}
