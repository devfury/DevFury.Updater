using DevFury.AutoUpdater;
using System;
using System.Reflection;
using System.Windows;

namespace DevFury.AutoUpdate.NetFw462Example
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (GetAppVersion() != "1.0.0.1")
            {
                DoUpdate();
            }
        }

        private void DoUpdate()
        {
            try
            {
                DevFuryUpdater.Update(
                    path: ".",
                    url: "http://localhost/update/update-v1.0.0.1.zip",
                    title: "UpdaterExample");

                Shutdown();
            }
            catch (Exception)
            {
            }
        }

        public static string GetAppVersion()
        {
            // 현재 애플리케이션의 Assembly 객체 가져오기
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 버전 정보 가져오기
            Version version = assembly.GetName().Version;

            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}" : null;
        }
    }
}
