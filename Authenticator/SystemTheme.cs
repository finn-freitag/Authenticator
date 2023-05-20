using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authenticator
{
    public class SystemTheme
    {
        const string keyname = "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";

        public static bool isAppsUseDarkTheme()
        {
            int tInteger = (int)Registry.GetValue(keyname, "AppsUseLightTheme", -1);
            if (tInteger == 0)
            {
                return true;
            }
            if (tInteger == 1)
            {
                return false;
            }
            throw new Exception("Error by reading app-theme!");
        }
    }
}
