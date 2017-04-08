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

namespace WallpaperManager
{
    class BingHelper
    {
        private static readonly string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WallpaperManager\";
        private static readonly string bingMainDir = applicationDataPath + @"bing\";
        public static readonly string bingXMLFile = bingMainDir + @"bing.xml";
        public static readonly string bingWallpaperDir = bingMainDir + @"wallpaper\";
        private static readonly string bingThumbnailDir = bingMainDir + @"thumbnails\";

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

        public static async Task DownloadWallpaperAsync(BingWallpaper item, string resolution)
        {
            string filePath = bingWallpaperDir + item.Name + "_" + resolution + ".jpg";
            if (!File.Exists(filePath))
            {
                using (WebClient wc = new WebClient())
                {
                    await wc.DownloadFileTaskAsync(new Uri(string.Format(bingImageUrl, item.Url, resolution)), filePath);
                }
            }
            else
                Debug.WriteLine("File '" + filePath + "' already exists");
        }

        public static async Task DownloadMultipleWallpaperAsync(List<BingWallpaper> imagelist, string resolution)
        {
            await Task.WhenAll(imagelist.Select(image => DownloadWallpaperAsync(image, resolution)));
        }

        public static async Task<BitmapSource> DownloadThumbnailAsync(BingWallpaper item)
        {
            BitmapSource bmp = null;
            if (!File.Exists(item.FilePath))
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
                    Utilities.SaveImg(bmp, item.FilePath);
                }
            }
            else
            {
                bmp = new BitmapImage(new Uri(item.FilePath));
                Debug.WriteLine("File '" + item.FilePath + "' already exists");
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
                FilePath = bingThumbnailDir + (el.Element("urlBase").Value.Split('/'))[4] + "_" + thumbnailResolution + ".bmp",
                Copyright = el.Element("copyright").Value,
                CopyrightLink = el.Element("copyrightlink").Value
            });
            return bingWallpaperDetails.ToList();
        }
    }
}
