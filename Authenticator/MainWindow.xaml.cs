using AForge.Video.DirectShow;
using EasyCodeClass;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
using System.Xml.Schema;
using ZXing;
using ZXing.QrCode;

namespace Authenticator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isFirstInfoShow = true;

        double l = 0, t = 0, w = 0, h = 0, mw = 0, mh = 0;

        KeyView currentEdit = null;

        System.Timers.Timer timer;

        System.Timers.Timer timesync;

        System.Timers.Timer t2;

        System.Timers.Timer t3;
        System.Drawing.Bitmap lastFrame = null;
        bool processingQRCode = false;

        FilterInfoCollection fiv = null;
        VideoCaptureDevice captureDevice = null;

        bool settingsLoading = false;

        TimeSpan timeDifference = TimeSpan.Zero;

        Brush timesyncpathBrush = Brushes.DarkGray;
        string timesyncpathHint = "Verified time used!";

        public MainWindow()
        {
            Storage.Init();
            Settings.Init();
            
            InitializeComponent();
            
            maingrid.Visibility = Visibility.Visible;
            infogrid.Visibility = Visibility.Hidden;
            codeinfogrid.Visibility = Visibility.Hidden;
            addmethodgrid.Visibility = Visibility.Hidden;
            fromscreengrid.Visibility = Visibility.Hidden;
            exportgrid.Visibility = Visibility.Hidden;
            safetyquerygrid.Visibility = Visibility.Hidden;
            fromwebcamgrid.Visibility = Visibility.Hidden;
            settingsgrid.Visibility = Visibility.Hidden;
            masterpasswordgrid.Visibility = Visibility.Hidden;
            
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            timesync = new System.Timers.Timer();
            timesync.Interval = 10000;
            timesync.Elapsed += Timesync_Elapsed;
            timesync.Start();
            Timesync_Elapsed(this, null);

            t2 = new System.Timers.Timer();
            t2.Interval = 200;
            t2.Elapsed += T2_Elapsed;
            t2.Enabled = false;

            t3 = new System.Timers.Timer();
            t3.Interval = 300;
            t3.Elapsed += T3_Elapsed;
            t3.Enabled = false;

            if(Settings.Encryption != Encryption.Masterpassword) ReloadKeys(); else
            {
                masterpassword_area.Visibility = Visibility.Visible;
            }
        }

        private void Timesync_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DateTime realTime = NTPClient.GetTime();
                TimeSpan difference = realTime - DateTime.Now;
                if (Math.Abs(difference.TotalSeconds) < 4)
                {
                    timesyncpathBrush = Brushes.DarkGray;
                    timesyncpathHint = "Verified time used!";
                    timeDifference = TimeSpan.Zero;
                }
                else
                {
                    timesyncpathBrush = Brushes.Red;
                    timesyncpathHint = "Your local time deviates from real time! You can't generate tokens offline!";
                    timeDifference = difference;
                }
            }
            catch
            {
                timesyncpathBrush = Brushes.Yellow;
                timesyncpathHint = "You're offline. Your local time can't be verified!";
                timeDifference = TimeSpan.Zero;
            }
        }

        private void mp_continue_Click(object sender, RoutedEventArgs e)
        {
            Protection.MasterPassword = masterpassword_input.Password;
            masterpassword_input.Password = "";
            masterpassword_area.Visibility = Visibility.Collapsed;
            try
            {
                ReloadKeys();
            }
            catch
            {
                MessageBox.Show("Wrong password! Try again!");
                masterpassword_area.Visibility = Visibility.Visible;
            }
        }

        private void T2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Window_LocationChanged(this, null);
                    t2.Stop();
                });
            }
            catch { }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DateTime time = DateTime.Now + timeDifference;

                Dispatcher.Invoke(() =>
                {
                    UpdateAllTokens(time);
                    timesyncpath.Fill = timesyncpathBrush;
                    timesyncpath.ToolTip = timesyncpathHint;
                });
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
            this.Close();
            Application.Current.Shutdown();
        }

        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        public void OpenCodeSettingsPage(KeyView sender)
        {
            maingrid.Visibility = Visibility.Hidden;
            codeinfogrid.Visibility = Visibility.Visible;

            edit_logo.ImageSource = sender.Image;
            CompanyEdit.Text = sender.company;
            UrlEdit.Text = sender.url;
            currentEdit = sender;

            if (Settings.UseAdvancedSettings)
            {
                advSettingsBlock.Visibility = Visibility.Visible;
                UsernameEdit.Text = sender.username;
                DigitEdit.Text = Convert.ToString(sender.digits);
                IntervalEdit.Text = Convert.ToString(sender.interval);
                SHAEdit.Text = TOTP.HashModeToString((HashMode)sender.hashmode);
            }
            else
            {
                advSettingsBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void info_back_Click(object sender, RoutedEventArgs e)
        {
            infogrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;
        }

        private void info_show_Click(object sender, RoutedEventArgs e)
        {
            if (isFirstInfoShow)
            {
                string url = AssemblyInfoHelper.GetURL();
                string info = AssemblyInfoHelper.GetTitle() + Environment.NewLine
                    + AssemblyInfoHelper.GetCopyright() + " " + AssemblyInfoHelper.GetCompany() + Environment.NewLine
                    + url + Environment.NewLine
                    + AssemblyInfoHelper.GetDescription();
                infoblock.Text = info;
                infoblock.ToolTip = url;
                logo_img.ToolTip = url;
            }
            maingrid.Visibility = Visibility.Hidden;
            infogrid.Visibility = Visibility.Visible;
        }

        private void infoblock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(AssemblyInfoHelper.GetURL());
        }

        private void NumericOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = IsTextNumeric(e.Text);
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if(fromscreengrid.Visibility == Visibility.Visible)
            {
                Point pixel = screen_capture_frame.PointToScreen(new Point(0, 0));
                Point secPixel = scfDestMarker.PointToScreen(new Point(0, 0));
                Size size = new Size(secPixel.X - pixel.X, secPixel.Y - pixel.Y);
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)size.Width + 2, (int)size.Height + 2);
                System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                g.CopyFromScreen(new System.Drawing.Point((int)pixel.X, (int)pixel.Y), new System.Drawing.Point(0, 0), new System.Drawing.Size((int)size.Width + 2, (int)size.Height + 2));
                g.Flush();
                g.Dispose();
                BarcodeReader barcodeReader = new BarcodeReader();
                Result result = barcodeReader.Decode(bmp);
                if(result != null)
                {
                    var totp = TOTP.DecodeUrl(result.ToString());
                    if (!String.IsNullOrEmpty(totp.Secret))
                    {
                        string final = Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Secret)) + ';'
                            + Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Issuer)) + ";;"
                            + Convert.ToBase64String(BitConverter.GetBytes(totp.Digits)) + ';'
                            + Convert.ToBase64String(BitConverter.GetBytes(totp.Period)) + ';'
                            + Convert.ToBase64String(new byte[] { (byte)totp.Algorithm }) + ';'
                            + Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Username));
                        Storage.SaveAsSecret("key_" + Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes(final));
                        fromscr_back_Click(this, null);
                        ReloadKeys();
                    }
                }
            }
        }

        public void ReloadKeys()
        {
            codelist.Children.Clear();
            foreach(string secret in Storage.GetAllSecrets())
            {
                if (secret.StartsWith("key_"))
                {
                    codelist.Children.Add(new KeyView(this, secret));
                }
            }
        }

        public void UpdateAllTokens(DateTime time)
        {
            foreach(UIElement element in codelist.Children)
            {
                ((KeyView)element).UpdateToken(time);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Window_LocationChanged(this, null);
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            string str = Encoding.UTF8.GetString(Storage.LoadSecret(currentEdit.SecretName));
            string[] parts = str.Split(';');

            string res = "";

            if (Settings.UseAdvancedSettings)
            {
                string digits = parts[3];
                try { digits = Convert.ToBase64String(BitConverter.GetBytes(Convert.ToInt32(DigitEdit.Text))); } catch { }
                string interval = parts[4];
                try { interval = Convert.ToBase64String(BitConverter.GetBytes(Convert.ToInt32(IntervalEdit.Text))); } catch { }
                string algorithm = parts[5];
                try { algorithm = Convert.ToBase64String(new byte[] { (byte)TOTP.StringToHashMode(SHAEdit.Text) }); } catch { }

                res = parts[0] + ';'
                    + Convert.ToBase64String(Encoding.UTF8.GetBytes(CompanyEdit.Text)) + ';'
                    + Convert.ToBase64String(Encoding.UTF8.GetBytes(UrlEdit.Text)) + ';'
                    + digits + ';'
                    + interval + ';'
                    + algorithm + ';'
                    + Convert.ToBase64String(Encoding.UTF8.GetBytes(UsernameEdit.Text));
            }
            else
            {
                res = parts[0] + ';'
                    + Convert.ToBase64String(Encoding.UTF8.GetBytes(CompanyEdit.Text)) + ';'
                    + Convert.ToBase64String(Encoding.UTF8.GetBytes(UrlEdit.Text)) + ';'
                    + parts[3] + ';'
                    + parts[4] + ';'
                    + parts[5] + ';'
                    + parts[6];
            }

            Storage.SaveAsSecret(currentEdit.SecretName, Encoding.UTF8.GetBytes(res));
            codeinfo_back_Click(this, null);
        }

        private void codeinfo_back_Click(object sender, RoutedEventArgs e)
        {
            codeinfogrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;

            currentEdit.CompleteUpdate();
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            codeinfogrid.Visibility = Visibility.Hidden;
            exportgrid.Visibility = Visibility.Visible;

            Point pixel = screen_capture_frame.PointToScreen(new Point(0, 0));
            Point secPixel = scfDestMarker.PointToScreen(new Point(0, 0));
            Size size = new Size(secPixel.X - pixel.X, secPixel.Y - pixel.Y);

            string codeContent = TOTP.EncodeUrl(currentEdit.company, currentEdit.username, currentEdit.Seed, (HashMode)currentEdit.hashmode, currentEdit.digits, currentEdit.interval);

            QrCodeEncodingOptions options = new QrCodeEncodingOptions()
            {
                DisableECI = true,
                CharacterSet = "UTF-8",
                Width = (int)size.Width + 2,
                Height = (int)size.Height + 2
            };

            BarcodeWriter writer = new BarcodeWriter()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = options
            };

            exportqrcode.Source = KeyView.ConvertImagesource(writer.Write(codeContent));

            /*QRCodeWriter qrcode = new QRCodeWriter();
            exportqrcode.Source = KeyView.ConvertImagesource(qrcode.encode(codeContent,
                BarcodeFormat.QR_CODE, (int)size.Width + 2, (int)size.Height + 2).ToBitmap());*/
            exportSeed.Text = currentEdit.Seed;
        }

        private void export_back_Click(object sender, RoutedEventArgs e)
        {
            exportgrid.Visibility = Visibility.Hidden;
            codeinfogrid.Visibility = Visibility.Visible;
        }

        private void delete_Click(object sender, RoutedEventArgs e)
        {
            codeinfogrid.Visibility = Visibility.Hidden;
            safetyquerygrid.Visibility = Visibility.Visible;
            delete_tb.Text = "";
        }

        private void delete_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(delete_tb.Text.ToLower() == "yes")
            {
                Storage.DeleteSecret(currentEdit.SecretName);
                ReloadKeys();
                safetyquerygrid.Visibility = Visibility.Hidden;
                maingrid.Visibility = Visibility.Visible;
            }
        }

        private void sq_back_Click(object sender, RoutedEventArgs e)
        {
            safetyquerygrid.Visibility = Visibility.Hidden;
            codeinfogrid.Visibility = Visibility.Visible;
        }

        private void fromWebcam_Click(object sender, RoutedEventArgs e)
        {
            fiv = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            cameraSelector.Items.Clear();
            addmethodgrid.Visibility = Visibility.Hidden;
            fromwebcamgrid.Visibility = Visibility.Visible;
            foreach (FilterInfo info in fiv)
            {
                cameraSelector.Items.Add(info.Name);
            }
            cameraSelector.SelectedIndex = 0;
            processingQRCode = false;
            //camera_SelectionChanged(this, null);
        }

        private void camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(fromwebcamgrid.Visibility == Visibility.Visible)
            {
                if (captureDevice != null && captureDevice.IsRunning)
                {
                    captureDevice.SignalToStop();
                    //captureDevice.Stop();
                }
                captureDevice = new VideoCaptureDevice(fiv[cameraSelector.SelectedIndex].MonikerString);
                captureDevice.NewFrame += CaptureDevice_NewFrame;
                captureDevice.Start();
            }
        }

        private void CaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    lastFrame = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
                    webcam_view.ImageSource = KeyView.ConvertImagesource(lastFrame);
                    t3.Start();
                });
            }
            catch { }
        }

        private void T3_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    BarcodeReader barcodeReader = new BarcodeReader();
                    Result result = barcodeReader.Decode(lastFrame);
                    if (result != null)
                    {
                        var totp = TOTP.DecodeUrl(result.ToString());
                        if (!String.IsNullOrEmpty(totp.Secret))
                        {
                            t3.Stop();
                            string final = Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Secret)) + ';'
                                + Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Issuer)) + ";;"
                                + Convert.ToBase64String(BitConverter.GetBytes(totp.Digits)) + ';'
                                + Convert.ToBase64String(BitConverter.GetBytes(totp.Period)) + ';'
                                + Convert.ToBase64String(new byte[] { (byte)totp.Algorithm }) + ';'
                                + Convert.ToBase64String(Encoding.UTF8.GetBytes(totp.Username));
                            if (!processingQRCode)
                            {
                                processingQRCode = true;
                                Storage.SaveAsSecret("key_" + Guid.NewGuid().ToString(), Encoding.UTF8.GetBytes(final));
                            }
                            from_webcam_back_Click(this, null);
                            ReloadKeys();
                        }
                    }
                });
            }
            catch { }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            t3.Stop();
            if (captureDevice != null && captureDevice.IsRunning)
            {
                captureDevice.SignalToStop();
                //captureDevice.Stop();
            }
        }

        private void from_webcam_back_Click(object sender, RoutedEventArgs e)
        {
            t3.Stop();
            if (captureDevice != null && captureDevice.IsRunning)
            {
                captureDevice.SignalToStop();
                //captureDevice.Stop();
            }
            fromwebcamgrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;
        }

        private void settings_back_Click(object sender, RoutedEventArgs e)
        {
            settingsgrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            maingrid.Visibility = Visibility.Hidden;
            settingsgrid.Visibility = Visibility.Visible;
            settingsLoading = true;
            switch (Settings.LightMode)
            {
                case LightMode.Light:
                    rb_lightmode.IsChecked = true;
                    rb_darkmode.IsChecked = false;
                    rb_systemmode.IsChecked = false;
                    break;
                case LightMode.Dark:
                    rb_lightmode.IsChecked = false;
                    rb_darkmode.IsChecked = true;
                    rb_systemmode.IsChecked = false;
                    break;
                case LightMode.System:
                    rb_lightmode.IsChecked = false;
                    rb_darkmode.IsChecked = false;
                    rb_systemmode.IsChecked = true;
                    break;
            }
            cb_loadlogos.IsChecked = Settings.UseUpleadLogo;
            tb_spacing.Text = Convert.ToString(Settings.CodeSpacing);
            switch (Settings.Encryption)
            {
                case Encryption.None:
                    rb_enc_none.IsChecked = true;
                    rb_enc_user.IsChecked = false;
                    rb_enc_machine.IsChecked = false;
                    rb_enc_masterpassword.IsChecked = false;
                    break;
                case Encryption.User:
                    rb_enc_none.IsChecked = false;
                    rb_enc_user.IsChecked = true;
                    rb_enc_machine.IsChecked = false;
                    rb_enc_masterpassword.IsChecked = false;
                    break;
                case Encryption.Machine:
                    rb_enc_none.IsChecked = false;
                    rb_enc_user.IsChecked = false;
                    rb_enc_machine.IsChecked = true;
                    rb_enc_masterpassword.IsChecked = false;
                    break;
                case Encryption.Masterpassword:
                    rb_enc_none.IsChecked = false;
                    rb_enc_user.IsChecked = false;
                    rb_enc_machine.IsChecked = false;
                    rb_enc_masterpassword.IsChecked = true;
                    break;
            }
            cb_advSettings.IsChecked = Settings.UseAdvancedSettings;
            cb_portable.IsChecked = Storage.Portable;
            
            settingsLoading = false;
        }

        private void rb_lightmode_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.LightMode = LightMode.Light;
                Process.Start(Application.ResourceAssembly.Location);
                Close_Click(this, null);
            }
        }

        private void rb_darkmode_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.LightMode = LightMode.Dark;
                Process.Start(Application.ResourceAssembly.Location);
                Close_Click(this, null);
            }
        }

        private void rb_systemmode_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.LightMode = LightMode.System;
                Process.Start(Application.ResourceAssembly.Location);
                Close_Click(this, null);
            }
        }

        private void cb_loadlogos_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.UseUpleadLogo = true;
                ReloadKeys();
            }
        }

        private void cb_loadlogos_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.UseUpleadLogo = false;
                ReloadKeys();
            }
        }

        private void tb_spacing_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!settingsLoading)
            {
                try
                {
                    int spacing = Convert.ToInt32(tb_spacing.Text);
                    Settings.CodeSpacing = spacing;
                }
                catch { }
            }
        }

        private void rb_enc_none_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Storage.SwitchSecretProtection(Encryption.None);
                if (Settings.UseMasterpassword) Settings.UseMasterpassword = false;
            }
        }

        private void rb_enc_user_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Storage.SwitchSecretProtection(Encryption.User);
                if (Settings.UseMasterpassword) Settings.UseMasterpassword = false;
            }
        }

        private void rb_enc_machine_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Storage.SwitchSecretProtection(Encryption.Machine);
                if (Settings.UseMasterpassword) Settings.UseMasterpassword = false;
            }
        }

        private void rb_enc_masterpassword_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                settingsgrid.Visibility = Visibility.Hidden;
                masterpasswordgrid.Visibility = Visibility.Visible;
            }
        }

        private void use_password_Click(object sender, RoutedEventArgs e)
        {
            if(password1.Text == password2.Text)
            {
                Protection.MasterPassword = password1.Text;
                password1.Text = "";
                password2.Text = "";
                Storage.SwitchSecretProtection(Encryption.Masterpassword);
                Settings.UseMasterpassword = true;
                masterpasswordgrid.Visibility = Visibility.Hidden;
                settings_Click(this, null);
            }
            else
            {
                MessageBox.Show("Passwords are not equal!");
            }
        }

        private void cb_advSettings_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.UseAdvancedSettings = true;
            }
        }

        private void cb_advSettings_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                Settings.UseAdvancedSettings = false;
            }
        }

        private void clear_cache_Click(object sender, RoutedEventArgs e)
        {
            foreach(string cacheitem in Storage.GetAllCacheObjs())
            {
                Storage.DeleteCacheObj(cacheitem);
            }
        }

        private static bool IsTextNumeric(string str)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]");
            return reg.IsMatch(str);
        }

        private void ms_back_Click(object sender, RoutedEventArgs e)
        {
            masterpasswordgrid.Visibility = Visibility.Hidden;
            password1.Text = "";
            password2.Text = "";
            settings_Click(this, null);
        }

        private void cb_portable_Checked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                timer.Stop();
                timer.Dispose();
                t2.Stop();
                t2.Dispose();
                t3.Stop();
                t3.Dispose();
                Thread.Sleep(200);
                Process.Start(Application.ResourceAssembly.Location);
                Storage.Portable = true;
                Close_Click(this, null);
            }
        }

        private void cb_portable_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!settingsLoading)
            {
                timer.Stop();
                timer.Dispose();
                t2.Stop();
                t2.Dispose();
                t3.Stop();
                t3.Dispose();
                Thread.Sleep(200);
                Process.Start(Application.ResourceAssembly.Location);
                Storage.Portable = false;
                Close_Click(this, null);
            }
        }

        private void timesyncbtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Settings.Time = NTPClient.GetTime(NTPClient.DEFAULT_SERVER, 6000);
                Timer_Elapsed(this, null);
            }
            catch
            {
                MessageBox.Show("Server not accessable. Please check you internet connection!");
            }
        }

        private void amg_back_Click(object sender, RoutedEventArgs e)
        {
            addmethodgrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;
        }

        private void add_service_Click(object sender, RoutedEventArgs e)
        {
            maingrid.Visibility = Visibility.Hidden;
            addmethodgrid.Visibility = Visibility.Visible;
        }

        private void fromscreen_Click(object sender, RoutedEventArgs e)
        {
            addmethodgrid.Visibility = Visibility.Hidden;
            fromscreengrid.Visibility = Visibility.Visible;
            mainborder.Background = Brushes.Transparent;
            l = this.Left;
            t = this.Top;
            w = this.Width;
            h = this.Height;
            mw = this.MinWidth;
            mh = this.MinHeight;
            this.MinHeight = 220;
            this.MinWidth = 120;
            t2.Start();
        }

        private void fromscr_back_Click(object sender, RoutedEventArgs e)
        {
            mainborder.Background = Settings.Background;
            fromscreengrid.Visibility = Visibility.Hidden;
            maingrid.Visibility = Visibility.Visible;
            this.Left = l;
            this.Top = t;
            this.Width = w;
            this.Height = h;
            this.MinWidth = mw;
            this.MinHeight = mh;
        }

        private void SHAEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            string t = SHAEdit.Text.ToUpper();
            if (t == "SHA1" || t == "SHA256" || t == "SHA512")
            {
                SHAEdit.Foreground = Settings.Foreground;
            }
            else
            {
                SHAEdit.Foreground = Brushes.Red;
            }
        }
    }
}
