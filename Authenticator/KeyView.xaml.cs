using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using EasyCodeClass;
using System.Diagnostics;

namespace Authenticator
{
    /// <summary>
    /// Interaction logic for KeyView.xaml
    /// </summary>
    public partial class KeyView : UserControl
    {
        public string company = "";
        public string url = "";
        public string username = "";
        internal string Seed = "";
        public int digits = 6;
        public int interval = 30;
        public byte hashmode = 0;
        public ImageSource Image { get { return CompanyLogo.ImageSource; } }

        public string lastToken = "";

        public string SecretName = "";
        private MainWindow parent = null;

        public KeyView(MainWindow parent, string SecretName)
        {
            InitializeComponent();
            this.SecretName = SecretName;
            this.parent = parent;
            CompleteUpdate();
        }

        public void CompleteUpdate()
        {
            // Secret string: SEED;COMPANY;URL;DIGITS;INTERVAL;HASHMODE;USERNAME

            string str = Encoding.UTF8.GetString(Storage.LoadSecret(SecretName));
            string[] parts = str.Split(';');
            Seed = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            company = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            url = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
            digits = BitConverter.ToInt32(Convert.FromBase64String(parts[3]), 0);
            interval = BitConverter.ToInt32(Convert.FromBase64String(parts[4]), 0);
            hashmode = Convert.FromBase64String(parts[5])[0];
            username = Encoding.UTF8.GetString(Convert.FromBase64String(parts[6]));

            CompanyName.Content = company;
            CompanyURL.Content = url;
            CompanyURL.ToolTip = url;
            //CompanyLogo.ToolTip = url;
            img_border.ToolTip = url;
            DateTime time = DateTime.Now;
            try
            {
                time = NTPClient.GetTime();
            }
            catch { }
            UpdateToken(time);
            CompanyLogo.ImageSource = ConvertImagesource(GetIcon(url));
        }

        public void UpdateToken(DateTime time)
        {
            string token = TOTP.GenerateTOTPToken(time, Seed, digits, interval, (HashMode)hashmode);
            string final = "";
            int spacing = Settings.CodeSpacing;
            for(int i = 0; i < token.Length; i++)
            {
                final += token[i];
                if ((i+1) % spacing == 0) final += ' ';
            }
            final = final.Trim(' ');
            lastToken = final;
            Code.Content = final;
        }

        public static BitmapImage ConvertImagesource(Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private static Bitmap GetIcon(string url)
        {
            if (Settings.UseUpleadLogo)
            {
                try
                {
                    string baseURL = GetBaseUrl(url);
                    if (baseURL != "")
                    {
                        if (Storage.CacheObjExists(baseURL))
                        {
                            return new Bitmap(new MemoryStream(Storage.LoadFromCache(baseURL)));
                        }
                        Stream s = get("https://logo.uplead.com/" + baseURL);
                        if (s != null)
                        {
                            byte[] data = Protection.ReadFully(s);
                            Storage.SaveInCache(baseURL, data);
                            return new Bitmap(new MemoryStream(data));
                        }
                    }
                }
                catch { }
            }
            return GetCloudImage();
        }

        private static Bitmap GetCloudImage()
        {
            LightMode mode = Settings.LightMode;
            if (mode == LightMode.Dark || (mode == LightMode.System && SystemTheme.isAppsUseDarkTheme())) return Properties.Resources.Services_dark;
            return Properties.Resources.Services_light;
        }

        private static string GetBaseUrl(string url)
        {
            try
            {
                Match m = Regex.Match(url, "^(?:https?:\\/\\/)?(?:www\\.)?(([a-zA-z0-9\\-]+).([a-zA-z0-9\\-]+))");
                if (m.Groups.Count >= 2) return m.Groups[1].Value;
            }
            catch { }
            return "";
        }

        private static Stream get(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                if (url.StartsWith("https://")) { request.Credentials = CredentialCache.DefaultCredentials; }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode.Equals(HttpStatusCode.OK))
                {
                    return response.GetResponseStream();
                }
                else
                {
                    return null;
                }
            }
            catch { }
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            parent.OpenCodeSettingsPage(this);
        }

        private void Code_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(((string)Code.Content).Replace(" ", ""));
        }

        private void CompanyURL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (url != "") Process.Start(url);
        }
    }
}
