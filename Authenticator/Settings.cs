using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Authenticator
{
    public static class Settings
    {
        public static void Init()
        {
            if (!Storage.EncryptionExists()) Storage.SaveEncryption(Encryption.User);
            if (!Storage.LightmodeExists()) Storage.SaveLightmode(LightMode.System);
            if (!Storage.MasterpasswordExists()) Storage.SetUseMasterpassword(false);
            if (!Storage.SecretExists("upleadlogo")) UseUpleadLogo = true;
            if (!Storage.SecretExists("codespacing")) CodeSpacing = 3;
            if (!Storage.SecretExists("advsettings")) UseAdvancedSettings = false;
        }

        public static CornerRadius CornerRadius { get; set; } = new CornerRadius(8);

        public static Brush Background// { get; set; } = Brushes.White;
        {
            get
            {
                LightMode mode = Settings.LightMode;
                if (mode == LightMode.Dark || (mode == LightMode.System && SystemTheme.isAppsUseDarkTheme())) return Brushes.Black;
                return Brushes.White;
            }
        }

        public static Brush Foreground// { get; set; } = Brushes.Black;
        {
            get
            {
                LightMode mode = Settings.LightMode;
                if (mode == LightMode.Dark || (mode == LightMode.System && SystemTheme.isAppsUseDarkTheme())) return Brushes.White;
                return Brushes.Black;
            }
        }

        public static DateTime Time
        {
            get
            {
                return DateTime.Now;
            }
            set
            {
                SystemTime.Set(value);
            }
        }

        public static LightMode LightMode
        {
            get
            {
                return Storage.LoadLightmode();
            }
            set
            {
                Storage.SaveLightmode(value);
            }
        }

        public static bool UseMasterpassword
        {
            get
            {
                return Storage.IsMasterpasswordUsed();
            }
            set
            {
                Storage.SetUseMasterpassword(value);
            }
        }

        public static Encryption Encryption
        {
            get
            {
                return Storage.LoadEncryption();
            }
            set
            {
                Storage.SaveEncryption(value);
            }
        }

        public static bool UseUpleadLogo
        {
            get
            {
                return Storage.LoadSecret("upleadlogo")[0] == 1;
            }
            set
            {
                Storage.SaveAsSecret("upleadlogo", new byte[] { (value ? (byte)1 : (byte)0) });
            }
        }

        public static bool UseAdvancedSettings
        {
            get
            {
                return Storage.LoadSecret("advsettings")[0] == 1;
            }
            set
            {
                Storage.SaveAsSecret("advsettings", new byte[] { (value ? (byte)1 : (byte)0) });
            }
        }

        public static int CodeSpacing
        {
            get
            {
                return BitConverter.ToInt32(Storage.LoadSecret("codespacing"), 0);
            }
            set
            {
                Storage.SaveAsSecret("codespacing", BitConverter.GetBytes(value));
            }
        }
    }

    public enum LightMode : byte
    {
        Light = 0,
        Dark = 1,
        System = 2
    }

    public enum Encryption : byte
    {
        None = 0,
        User = 1,
        Machine = 2,
        Masterpassword = 3
    }
}
