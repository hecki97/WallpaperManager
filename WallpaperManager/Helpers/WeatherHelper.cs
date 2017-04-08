using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace WallpaperManager
{
    class WeatherHelper
    {
        private static readonly string appid = "0c42e9b22f0b4a9c8d69ed09e4f1123c";
        private static readonly string openWeatherUrl = "http://api.openweathermap.org/data/2.5/forecast/daily?q={0}&type=like&mode=xml&units=metric&cnt=3&lang={1}&appid={2}";

        private static readonly string applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\WallpaperManager";
        public static readonly string openWeatherFilePath = applicationDataPath + @"\weather\openweather.xml";
        private static readonly string packagePath = applicationDataPath + @"\weather\packages\";
        private static string location = "Saarbrücken";
        private static string language = "de";

        public static async Task GetOpenWeatherDataAsync()
        {
            using (WebClient wc = new WebClient())
            {
                wc.Encoding = Encoding.GetEncoding("UTF-8");
                try
                {
                    string response = await wc.DownloadStringTaskAsync(string.Format(openWeatherUrl, location, language, appid));
                    if (!(response.Contains("message") && response.Contains("cod")))
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(response);
                        using (StringWriter sw = new StringWriter())
                        {
                            XmlTextWriter textWriter = new XmlTextWriter(sw);
                            textWriter.Formatting = Formatting.Indented;
                            xmlDoc.WriteTo(textWriter);
                            xmlDoc.Save(openWeatherFilePath);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("!Error: " + e.ToString());
                }
            } 
        }

        public static List<WeatherDetails> GetOpenWeatherInfo(XElement xEl)
        {
            IEnumerable<WeatherDetails> weatherDetails = xEl.Descendants("time").Select((el) =>
                new WeatherDetails
                {
                    Humidity = el.Element("humidity").Attribute("value").Value + "%",
                    MaxTemperature = el.Element("temperature").Attribute("max").Value + "°",
                    MinTemperature = el.Element("temperature").Attribute("min").Value + "°",
                    Temperature = el.Element("temperature").Attribute("day").Value + "°",
                    Weather = el.Element("symbol").Attribute("name").Value,
                    WeatherDay = DayOfTheWeek(el),
                    WeatherIcon = el.Element("symbol").Attribute("var").Value, //WeatherIconPath(el),
                    WindDirection = el.Element("windDirection").Attribute("name").Value,
                    WindSpeed = el.Element("windSpeed").Attribute("mps").Value + "mps",
                    WeatherConditionCode = WeatherConditionCode(el)
                });

            return weatherDetails.ToList();
        }

        private static string DayOfTheWeek(XElement el)
        {
            DayOfWeek dW = Convert.ToDateTime(el.Attribute("day").Value).DayOfWeek;
            return CultureInfo.CurrentCulture.DateTimeFormat.DayNames[(int)dW];
        }

        private static string WeatherConditionCode(XElement el)
        {
            string symbolVar = el.Element("symbol").Attribute("var").Value;
            string symbolNumber = el.Element("symbol").Attribute("number").Value;
            string dayOrNight = symbolVar.ElementAt(2).ToString(); // d or n
            return string.Format("{0}{1}", symbolNumber, dayOrNight);
        }


        public static void WeatherTreeScan(ref Dictionary<string, List<string>> package, ref int fileCount, string path)
        {
            if (Directory.Exists(path))
            {
                AddFilesToDictionary(ref package, ref fileCount, Directory.GetFiles(path));
                //Loop trough each directory
                foreach (string dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        WeatherTreeScan(ref package, ref fileCount, dir); //Recursive call to get subdirs
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        DebugLog.Error(e.Message);
                        continue;
                    }
                }
            }
        }

        public static void AddFilesToDictionary(ref Dictionary<string, List<string>> packageDictionary, ref int fileCount, string[] filepaths)
        {
            foreach (string filepath in filepaths)
            {
                fileCount += 1;
                string weathercode = Path.GetFileNameWithoutExtension(filepath).Truncate(4);
                if (packageDictionary.ContainsKey(weathercode))
                    packageDictionary[weathercode].Add(filepath);
                else
                    packageDictionary.Add(weathercode, new List<string> { filepath });
            }
        }

    }
}
