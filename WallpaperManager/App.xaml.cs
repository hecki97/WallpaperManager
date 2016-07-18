using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WallpaperManager
{
    public delegate void ApplicationEventHandler();

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public event ApplicationEventHandler OnApplicationExit;

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (OnApplicationExit != null) OnApplicationExit();
        }
    }
}
