using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace Authenticator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            string currentPath = Path.GetDirectoryName(typeof(App).Assembly.Location) + Path.DirectorySeparatorChar;
            if (!File.Exists(currentPath + "AForge.dll")) File.WriteAllBytes(currentPath + "AForge.dll", Authenticator.Properties.Resources.AForge);
            if (!File.Exists(currentPath + "AForge.Video.DirectShow.dll")) File.WriteAllBytes(currentPath + "AForge.Video.DirectShow.dll", Authenticator.Properties.Resources.AForge_Video_DirectShow);
            if (!File.Exists(currentPath + "AForge.Video.dll")) File.WriteAllBytes(currentPath + "AForge.Video.dll", Authenticator.Properties.Resources.AForge_Video);
            if (!File.Exists(currentPath + "zxing.dll")) File.WriteAllBytes(currentPath + "zxing.dll", Authenticator.Properties.Resources.zxing);
            if (!File.Exists(currentPath + "zxing.presentation.dll")) File.WriteAllBytes(currentPath + "zxing.presentation.dll", Authenticator.Properties.Resources.zxing_presentation);
        }
    }
}
