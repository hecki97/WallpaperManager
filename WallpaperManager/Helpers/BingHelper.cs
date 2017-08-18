using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.Serialization;

namespace WallpaperManager
{
    class BingHelper
    {
        private static readonly string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WallpaperManager\";
        private static readonly string bingMainDir = applicationDataPath + @"bing\";
        public static readonly string bingXMLFile = bingMainDir + @"bing.xml";
        public static readonly string bingWallpaperDir = bingMainDir + @"wallpaper\";
        private static readonly string bingThumbnailDir = bingMainDir + @"thumbnails\";

        //Archive Test
        public static Dictionary<int, Dictionary<int, List<KeyValuePair<int, BingWallpaper>>>> bingArchive = new Dictionary<int, Dictionary<int, List<KeyValuePair<int, BingWallpaper>>>>();

        /* Bing Properties */
        private static readonly string bingXMLUrl = "http://www.bing.com/hpimagearchive.aspx?format=xml&idx=-1&n={0}&mkt={1}";
        private static readonly string bingImageUrl = "http://www.bing.com{0}_{1}.jpg";

        /* Customizable & Saveable Properties */
        private static string thumbnailResolution = "1280x720";
        private static string region = "de-DE";
        private static int n = 8; /// > 8 not supported

        #region Asynchronous Operations
        public static async Task DownloadXMLAsync()
        {
            try
            {
                using (WebClient wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.GetEncoding("UTF-8");
                    string xml = await wc.DownloadStringTaskAsync(string.Format(bingXMLUrl, n, region));

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);
                    using (StringWriter sw = new StringWriter())
                    {
                        XmlTextWriter textWriter = new XmlTextWriter(sw);
                        textWriter.Formatting = Formatting.Indented;
                        xmlDoc.WriteTo(textWriter);
                        xmlDoc.Save(bingXMLFile);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("!Error: " + e.ToString());
            }
        }

        public static async Task<int> DownloadWallpaperAsync(BingWallpaper item, string resolution)
        {
            string fileDir = bingWallpaperDir + item.Name + @"\";
            string filePath = fileDir + item.Name + "_" + resolution + ".jpg";
            if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);
            if (!File.Exists(filePath))
            {
                //TODO: Do something when the download has failed! & Move MessageBoxes from Helper to ViewModel
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        await wc.DownloadFileTaskAsync(new Uri(string.Format(bingImageUrl, item.Url, resolution)), filePath);
                    }
                }
                catch (WebException)
                {
                    File.Delete(filePath);
                    return 1;
                }
                catch (Exception e)
                {
                    //TODO: Better error logging
                    DebugLog.Error(e.GetType().ToString() + ": " + e.Message);
                    return -1;
                }
            }
            else
                DebugLog.Log("File '" + filePath + "' already exists");

            return 0;
        }

        /*
        public static async Task DownloadMultipleWallpaperAsync(List<BingWallpaper> imagelist, string resolution)
        {
            await Task.WhenAll(imagelist.Select(image => DownloadWallpaperAsync(image, resolution)));
        }
        */

        public static async Task<BitmapSource> DownloadThumbnailAsync(BingWallpaper item)
        {
            BitmapSource bmp = null;
            if (!File.Exists(item.ThumbnailPath))
            {

                byte[] imgData = null;
                using (WebClient wc = new WebClient())
                {
                    imgData = await wc.DownloadDataTaskAsync(string.Format(bingImageUrl, item.Url, thumbnailResolution));
                }

                using (MemoryStream ms = new MemoryStream(imgData))
                {
                    var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    bmp = decoder.Frames[0];
                    Utilities.SaveImg(bmp, item.ThumbnailPath);
                }
            }
            else
            {
                bmp = new BitmapImage(new Uri(item.ThumbnailPath));
                Debug.WriteLine("File '" + item.ThumbnailPath + "' already exists");
            }
            return bmp;
        }

        public static async Task<BitmapSource[]> DownloadMultipleThumbnailsAsync(List<BingWallpaper> thmbnList)
        {
            return await Task.WhenAll(thmbnList.Select(item => DownloadThumbnailAsync(item)));
        }
        #endregion

        public static List<BingWallpaper> GetBingXmlInfo(XElement xEl)
        {
            IEnumerable<BingWallpaper> bingWallpaperDetails = xEl.Descendants("image").Select((el) => new BingWallpaper
            {
                Date = DateTime.ParseExact(el.Element("enddate").Value, "yyyyMMdd", CultureInfo.InvariantCulture),
                Url = el.Element("urlBase").Value,
                Name = (el.Element("urlBase").Value.Split('/'))[4],
                Resolution = thumbnailResolution,
                Directory = bingWallpaperDir,
                FilePath = bingThumbnailDir + (el.Element("urlBase").Value.Split('/'))[4] + "_" + thumbnailResolution + ".jpg",
                ThumbnailPath = bingThumbnailDir + (el.Element("urlBase").Value.Split('/'))[4] + "_" + thumbnailResolution + ".jpg",
                Copyright = el.Element("copyright").Value,
                CopyrightLink = el.Element("copyrightlink").Value
            });

            //Test
            //AddMultipleBingWallpapersToArchive(bingWallpaperDetails.ToList());

            //SerializeBingArchive();
            //var x = bingArchive;

            return bingWallpaperDetails.ToList();
        }

        /*
        private static void SerializeBingArchive()
        {
            DataContractSerializer serializer = new DataContractSerializer(bingArchive.GetType());

            XmlDocument xmlDoc = new XmlDocument();
            using (StringWriter sw = new StringWriter())
            {
                using (XmlTextWriter writer = new XmlTextWriter(sw))
                {
                    // add formatting so the XML is easy to read in the log
                    writer.Formatting = Formatting.Indented;
                    serializer.WriteObject(writer, bingArchive);

                    xmlDoc.WriteTo(writer);
                    xmlDoc.Save(bingMainDir + "test.xml");

                    writer.Flush();
                }
            }
        }
        */

        /*
        private static void AddMultipleBingWallpapersToArchive(List<BingWallpaper> bingWallpapers)
        {
           foreach (BingWallpaper bingWallpaper in bingWallpapers)
           {
                AddSingleBingWallpaperToArchive(bingWallpaper);
           }
        }

        private static void AddSingleBingWallpaperToArchive(BingWallpaper bingWallpaper)
        {
            DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
            int bingWallpaperYear = bingWallpaper.Date.Year;
            int bingWallpaperWeekOfYear = dfi.Calendar.GetWeekOfYear(bingWallpaper.Date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
            int bingWallpaperDayOfMonth = dfi.Calendar.GetDayOfMonth(bingWallpaper.Date);
            if (bingArchive.ContainsKey(bingWallpaperYear))
            {
                Dictionary<int, List<KeyValuePair<int, BingWallpaper>>> subDir = bingArchive[bingWallpaperYear];
                if (subDir.ContainsKey(bingWallpaperWeekOfYear))
                    subDir[bingWallpaperWeekOfYear].Add(new KeyValuePair<int, BingWallpaper>(bingWallpaperDayOfMonth, bingWallpaper));
                else
                    subDir.Add(bingWallpaperWeekOfYear, new List<KeyValuePair<int, BingWallpaper>>() { new KeyValuePair<int, BingWallpaper>(bingWallpaperDayOfMonth, bingWallpaper) });
            }
            else
            {
                bingArchive.Add(bingWallpaperYear, new Dictionary<int, List<KeyValuePair<int,BingWallpaper>>>() { { bingWallpaperWeekOfYear, new List<KeyValuePair<int,BingWallpaper>>() { new KeyValuePair<int, BingWallpaper>(bingWallpaperDayOfMonth, bingWallpaper) } } });
            }
        }
        */

        public static async void SetBingWallpaper(BingWallpaper bingWallpaper, string downloadResolution, WallpaperSettings wallpaperSettings)
        {
            string filePath = bingWallpaperDir + bingWallpaper.Name +  @"\" + bingWallpaper.Name + "_" + downloadResolution + ".jpg";
            if (!File.Exists(filePath)) await DownloadWallpaperAsync(bingWallpaper, downloadResolution);
            WallpaperHandler.Set(filePath, wallpaperSettings.WallpaperStyle, wallpaperSettings.BackgroundColor);
        }
    }
}
