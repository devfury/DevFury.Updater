namespace DevFury.AutoUpdate.Models
{
    public enum UpdateStep
    {
        WaitForProcessToExit = 0,
        CreateTemporaryFolder = 1,
        DownloadUpdateFile = 2,
        ExtractZipFile = 3,
        PatchFiles = 4,
        RemoveTemporaryFolder = 5,
        RestartProcess = 6,
    }

    public static class UpdateStepExtension
    {
        public static string ToDescription(this UpdateStep step)
        {
            switch (step)
            {
                case UpdateStep.WaitForProcessToExit: return "Waiting for the application to close...";
                case UpdateStep.CreateTemporaryFolder: return "Creating a temporary folder for the update...";
                case UpdateStep.DownloadUpdateFile: return "Downloading the latest update files...";
                case UpdateStep.ExtractZipFile: return "Extracting the update package...";
                case UpdateStep.PatchFiles: return "Copying updated files to the application directory...";
                case UpdateStep.RemoveTemporaryFolder: return "Cleaning up temporary files...";
                case UpdateStep.RestartProcess: return "Restarting the application to complete the update...";
                default: return "";
            };
        }

        static public double GetWeight(this UpdateStep step)
        {
            switch (step)
            {
                case UpdateStep.WaitForProcessToExit: return 2;
                case UpdateStep.CreateTemporaryFolder: return 2;
                case UpdateStep.DownloadUpdateFile: return 30;
                case UpdateStep.ExtractZipFile: return 30;
                case UpdateStep.PatchFiles: return 5;
                case UpdateStep.RemoveTemporaryFolder: return 2;
                case UpdateStep.RestartProcess: return 2;
                default: return 0;
            };
        }
    }
}
