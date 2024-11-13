using System;
using System.Diagnostics;
using System.Text;

namespace DevFury.AutoUpdater
{
    public class DevFuryUpdater
    {
        public static void Update(string url, string title = "DevFuryUpdater", string path = ".", bool isAdmin = false)
        {
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                string commandArguments = string.Join(" ", Environment.GetCommandLineArgs());
                byte[] bytes = Encoding.UTF8.GetBytes(commandArguments);
                string base64Command = Convert.ToBase64String(bytes);

                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "DevFuryUpdate",
                    Arguments = $"-t {title} -o {path} -p {currentProcess.ProcessName} -c {base64Command} {url}",
                    UseShellExecute = isAdmin,
                    Verb = isAdmin ? "runas" : null,
                };

                Process.Start(psi);
            }
            catch (Exception)
            {
            }
        }
    }
}
