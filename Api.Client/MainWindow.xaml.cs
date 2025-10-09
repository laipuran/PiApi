using Newtonsoft.Json;
using RaspberryPiApi.Models;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Api.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private DeviceStatus _deviceStatus = new();
        public DeviceStatus DeviceStatus
        {
            get { return _deviceStatus; }
            set
            {
                if (_deviceStatus != value)
                {
                    _deviceStatus = value;
                    OnPropertyChanged(nameof(DeviceStatus));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            RefreshStatus();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private async void RefreshStatus()
        {
            try
            {
                string? json = await GetDataAsync("http://192.168.137.164:5000/DeviceStatus");
                if (string.IsNullOrEmpty(json)) return;
                var deviceStatus = JsonConvert.DeserializeObject<DeviceStatus>(json);
                if (deviceStatus != null) DeviceStatus = deviceStatus;
            }
            catch { }
        }

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);


        public enum WindowZIndex
        {
            Top = 0,
            Bottom = 1,
            TopMost = -1,
            NoTopMost = -2,
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            RefreshStatus();
            SetWindowPos(new WindowInteropHelper(this).Handle,
                (IntPtr)WindowZIndex.Top, (int)this.Left,
                (int)this.Top, (int)this.Width, (int)this.Height, 0x0003);
        }

        public static async Task<string?> GetDataAsync(string url)
        {
            try
            {
                HttpClient client = new();
                return await client.GetStringAsync(url);
            }
            catch { }
            return null;
        }
    }
}