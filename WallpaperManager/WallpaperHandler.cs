using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace WallpaperManager
{
    // Code taken from: https://code.msdn.microsoft.com/windowsdesktop/cssetdesktopwallpaper-2107409c and modified by myself
    public static class WallpaperHandler {

        private static class NativeMethods
        {
            // The second constant represents the wallpaper operation to take place in this sample, to be used in the first argument. The other two constants will be combined together for the final argument.
            public const int COLOR_DESKTOP = 1;
            public const int SPI_SETDESKWALLPAPER = 20;
            public const int SPIF_UPDATEINIFILE = 0x01;
            public const int SPIF_SENDWININICHANGE = 0x02;

            // The SystemParametersInfo function exists in the user32.dll to allow you to set or retrieve hardware and configuration information from your system. The function accepts four arguments.
            // The first indicates the operation to take place, the second two parameters represent data to be set, dependant on requested operation, and the final parameter allows you to specify how changes are saved and/or broadcasted. 
            [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

            [DllImport("user32.dll")]
            public static extern bool SetSysColors(int cElements, int[] lpaElements, int[] lpaRgbValues);
        }

        public enum Style : int
        {
            Fill,
            Fit,
            Stretch,
            Tile,
            Centre,
            Span
        }

        public static void Set(string path, Style style, Color color)
        {
            // The operation to be invoked is SPI_SETDESKWALLPAPER. It sets the desktop wallpaper. The value of the pvParam parameter determines file path of the new wallpaper. The file must be a bitmap (.bmp).
            // On Windows Vista and later pvParam can also specify a .jpg file. If the specified image file is neither .bmp nor .jpg, or if the image is a .jpg file but the operating system is Windows Server 2003 or Windows XP/2000
            // that does not support .jpg as the desktop wallpaper, we convert the image file to .bmp and save it to the %appdata%\Microsoft\Windows\Themes folder.
            string extension = Path.GetExtension(path);
            if ((!extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)) || (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) && !IsWin7OrHigher()))
            {
                using (Image image = Image.FromFile(path))
                {
                    path = string.Format(@"{0}\Microsoft\Windows\Themes\{1}.bmp", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Path.GetFileNameWithoutExtension(path));
                    image.Save(path, ImageFormat.Bmp);
                }
            }
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            key.SetValue(@"Background", string.Format("{0} {1} {2}", color.R, color.G, color.B));
            key.Close();

            key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            switch (style)
            {
                case Style.Fill: // (Windows 7 and later)
                    key.SetValue(@"WallpaperStyle", "10");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case Style.Fit: // (Windows 7 and later)
                    key.SetValue(@"WallpaperStyle", "6");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case Style.Stretch:
                    key.SetValue(@"WallpaperStyle", "2");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case Style.Tile:
                    key.SetValue(@"WallpaperStyle", "1");
                    key.SetValue(@"TileWallpaper", "1");
                    break;
                case Style.Centre:
                    key.SetValue(@"WallpaperStyle", "1");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
                case Style.Span: //Experimental
                    key.SetValue(@"WallpaperStyle", "22");
                    key.SetValue(@"TileWallpaper", "0");
                    break;
            }
            key.Close();

            // Set the desktop wallpapaer by calling the Win32 API SystemParametersInfo with the SPI_SETDESKWALLPAPER desktop parameter. The changes should persist, and also be immediately visible.
            if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, path, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE)) throw new Win32Exception();

            int[] elements = { NativeMethods.COLOR_DESKTOP };
            int[] colors = { ColorTranslator.ToWin32(color) };
            if (!NativeMethods.SetSysColors(elements.Length, elements, colors)) throw new Win32Exception();
        }

        public static void SetSolidColor(Color color)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            key.SetValue(@"Background", string.Format("{0} {1} {2}", color.R, color.G, color.B));
            key.Close();

            // Set the desktop wallpapaer by calling the Win32 API SystemParametersInfo with the SPI_SETDESKWALLPAPER desktop parameter. The changes should persist, and also be immediately visible.
            if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, string.Empty, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE)) throw new Win32Exception();

            int[] elements = { NativeMethods.COLOR_DESKTOP };
            int[] colors = { ColorTranslator.ToWin32(color) };
            if (!NativeMethods.SetSysColors(elements.Length, elements, colors)) throw new Win32Exception();
        }

        private static bool IsWin7OrHigher()
        {
            OperatingSystem OS = Environment.OSVersion;
            return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6) && (OS.Version.Minor >= 1);
        }
    }
}
