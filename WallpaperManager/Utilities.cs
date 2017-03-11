using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Text;
using System.Windows.Media.Imaging;

namespace WallpaperManager
{
    static class Utilities
    {
        private static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

            [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
            public static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters, out uint lpTotalNumberOfClusters);
        }

        public static bool IsWin7OrHigher()
        {
            OperatingSystem OS = Environment.OSVersion;
            return (OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6) && (OS.Version.Minor >= 1);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static long GetFileSizeOnDisk(string file)
        {
            FileInfo info = new FileInfo(file);
            uint dummy, sectorsPerCluster, bytesPerSector;
            int result = NativeMethods.GetDiskFreeSpaceW(info.Directory.Root.FullName, out sectorsPerCluster, out bytesPerSector, out dummy, out dummy);
            if (result == 0) throw new Win32Exception();
            uint clusterSize = sectorsPerCluster * bytesPerSector;
            uint hosize;
            uint losize = NativeMethods.GetCompressedFileSizeW(file, out hosize);
            long size;
            size = (long)hosize << 32 | losize;
            return ((size + clusterSize - 1) / clusterSize) * clusterSize;
        }

        public static string GetMD5HashFromFile(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }

        public static void ClearSort(DataGrid dataGrid)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (view != null && view.SortDescriptions != null)
            {
                view.SortDescriptions.Clear();
                foreach (var column in dataGrid.Columns)
                {
                    column.SortDirection = null;
                }
            }
        }

        public static void SaveImg(this BitmapSource image, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }
        }
    }
}
