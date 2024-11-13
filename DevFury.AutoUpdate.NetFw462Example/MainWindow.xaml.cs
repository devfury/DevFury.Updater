using System;
using System.Reflection;
using System.Windows;

namespace DevFury.AutoUpdate.NetFw462Example
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ShowAppVersion();
        }

        private void ShowAppVersion()
        {
            string versionText = $"Version: {App.GetAppVersion()}";
            tbVersion.Text = versionText;
        }
    }
}
