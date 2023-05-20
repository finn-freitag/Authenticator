using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Authenticator
{
    public static class Storage
    {
        private static string path = "";
        private static string cachepath = "";
        private static string secretpath = "";

        public static void Init()
        {
            if (Portable) path = GetPortablePath();
            else path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Path.DirectorySeparatorChar + "Authenticator" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            cachepath = path + "cache" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(cachepath)) Directory.CreateDirectory(cachepath);
            secretpath = path + "secrets" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(secretpath)) Directory.CreateDirectory(secretpath);
        }

        public static bool Portable
        {
            get
            {
                string p = GetPortablePath();
                if (!Directory.Exists(p)) return false;
                if (File.Exists(p + "portable.dat") && File.ReadAllText(p + "portable.dat").ToLower() == "true") return true;
                return false;
            }
            set
            {
                if (value)
                {
                    path = GetPortablePath();
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    cachepath = path + "cache" + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(cachepath)) Directory.CreateDirectory(cachepath);
                    secretpath = path + "secrets" + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(secretpath)) Directory.CreateDirectory(secretpath);
                    File.WriteAllText(path + "portable.dat", "true");
                }
                else
                {
                    string p = GetPortablePath();
                    if (Directory.Exists(p) && File.Exists(p + "portable.dat")) File.WriteAllText(path + "portable.dat", "false");
                }
            }
        }

        private static string GetPortablePath()
        {
            return Path.GetDirectoryName(typeof(Storage).Assembly.Location) + Path.DirectorySeparatorChar + "Authenticator" + Path.DirectorySeparatorChar;
        }

        public static void SaveEncryption(Encryption encryption)
        {
            File.WriteAllBytes(path + "encryption.dat", new byte[] { (byte)encryption });
        }

        public static Encryption LoadEncryption()
        {
            return (Encryption)File.ReadAllBytes(path + "encryption.dat")[0];
        }

        public static bool EncryptionExists()
        {
            return File.Exists(path + "encryption.dat");
        }

        public static void SaveLightmode(LightMode lightMode)
        {
            File.WriteAllBytes(path + "lightmode.dat", new byte[] { (byte)lightMode });
        }

        public static LightMode LoadLightmode()
        {
            return (LightMode)File.ReadAllBytes(path + "lightmode.dat")[0];
        }

        public static bool LightmodeExists()
        {
            return File.Exists(path + "lightmode.dat");
        }

        public static void SetUseMasterpassword(bool useMasterpassword)
        {
            File.WriteAllBytes(path + "usemasterpassword.dat", new byte[] { (useMasterpassword ? (byte)1 : (byte)0) });
        }

        public static bool IsMasterpasswordUsed()
        {
            return File.ReadAllBytes(path + "usemasterpassword.dat")[0] == 1;
        }

        public static bool MasterpasswordExists()
        {
            return File.Exists(path + "usemasterpassword.dat");
        }

        public static void SaveInCache(string objName, byte[] data)
        {
            File.WriteAllBytes(cachepath + ToBase64(objName), data);
        }

        public static byte[] LoadFromCache(string objName)
        {
            try
            {
                return File.ReadAllBytes(cachepath + ToBase64(objName));
            }
            catch
            {
                return null;
            }
        }

        public static bool CacheObjExists(string objName)
        {
            return File.Exists(cachepath + ToBase64(objName));
        }

        public static void DeleteCacheObj(string objName)
        {
            try { File.Delete(cachepath + ToBase64(objName)); } catch { }
        }

        public static string[] GetAllCacheObjs()
        {
            List<string> objs = new List<string>();
            foreach(string str in Directory.GetFiles(cachepath))
            {
                objs.Add(FromBase64(Path.GetFileName(str)));
            }
            return objs.ToArray();
        }

        public static void SaveAsSecret(string objName, byte[] secret)
        {
            File.WriteAllBytes(secretpath + ToBase64(objName), Protection.Protect(secret, Settings.Encryption));
        }

        public static void SaveAsSecret(string objName, byte[] secret, Encryption encryption)
        {
            File.WriteAllBytes(secretpath + ToBase64(objName), Protection.Protect(secret, encryption));
        }

        public static byte[] LoadSecret(string objName)
        {
            try
            {
                return Protection.UnProtect(File.ReadAllBytes(secretpath + ToBase64(objName)), Settings.Encryption);
            }
            catch
            {
                return null;
            }
        }

        public static byte[] LoadSecret(string objName, Encryption encryption)
        {
            try
            {
                return Protection.UnProtect(File.ReadAllBytes(secretpath + ToBase64(objName)), encryption);
            }
            catch
            {
                return null;
            }
        }

        public static void SwitchSecretProtection(Encryption newEncryption)
        {
            Encryption oldEncryption = Settings.Encryption;
            foreach(string secret in GetAllSecrets())
            {
                byte[] bytes = LoadSecret(secret, oldEncryption);
                SaveAsSecret(secret, bytes, newEncryption);
            }
            SaveEncryption(newEncryption);
        }

        public static bool SecretExists(string objName)
        {
            return File.Exists(secretpath + ToBase64(objName));
        }

        public static string[] GetAllSecrets()
        {
            List<string> objs = new List<string>();
            foreach (string str in Directory.GetFiles(secretpath))
            {
                objs.Add(FromBase64(Path.GetFileName(str)));
            }
            return objs.ToArray();
        }

        public static void DeleteSecret(string objName)
        {
            try { File.Delete(secretpath + ToBase64(objName)); } catch { }
        }

        private static string ToBase64(string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        private static string FromBase64(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }
    }
}
