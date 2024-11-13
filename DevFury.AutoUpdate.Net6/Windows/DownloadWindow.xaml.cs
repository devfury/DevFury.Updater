using DevFury.AutoUpdate.Models;
using System.Windows;

namespace DevFury.AutoUpdate.Windows
{
    /// <summary>
    /// Interaction logic for DownloadWindow.xaml
    /// </summary>
    public partial class DownloadWindow : Window
    {
        public DownloadWindowViewModel viewModel { get; internal set; }

        public DownloadWindow(ExecutionOptions options)
        {
            InitializeComponent();

            viewModel = new DownloadWindowViewModel(options)
            {
                Owner = this,
            };

            DataContext = viewModel;

            viewModel.StartCommand.Execute(null);
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
