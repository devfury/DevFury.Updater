using DevFury.AutoUpdate.Models;
using DevFury.AutoUpdate.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DevFury.AutoUpdate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var options = ExecutionOptions.ParseArgs(e.Args);

                if (!options.IsValid())
                {
                    throw new ArgumentException();
                }

                SetForgroundWindowWhenIsRunning();

                DownloadWindow win = new DownloadWindow(options);
                win.Show();
            }
            catch (Exception)
            {
                Shutdown();
            }
        }

        private void SetForgroundWindowWhenIsRunning()
        {
            // Store all running process in the system
            Process currentProcess = Process.GetCurrentProcess();
            Process[] runningProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

            if (runningProcesses.Length <= 1)
            {
                return;
            }

            foreach (Process process in runningProcesses)
            {
                if (process.Id == currentProcess.Id)
                    continue;

                if (process.MainWindowHandle == IntPtr.Zero)
                    continue;

                WindowPlacement placement = new WindowPlacement();
                placement.length = Marshal.SizeOf(placement);

                GetWindowPlacement(process.MainWindowHandle, ref placement);

                if (placement.showCmd == ShowWindowCommands.Minimized)
                {
                    ShowWindow(process.MainWindowHandle, SW_RESTORE);
                }

                SetForegroundWindow(process.MainWindowHandle);
                break;
            }

            Shutdown();
        }
    }
}
