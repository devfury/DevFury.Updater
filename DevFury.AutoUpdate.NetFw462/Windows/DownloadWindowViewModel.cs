using DevFury.AutoUpdate.Commons;
using DevFury.AutoUpdate.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DevFury.AutoUpdate.Windows
{
    public class DownloadWindowViewModel : ViewModelBase
    {
        #region Properties

        private string title = "DevFuryUpdate";
        public string Title { get => title; set { title = value; OnPropertyChanged(() => Title); } }

        private UpdateStep currentStep = UpdateStep.WaitForProcessToExit;
        public UpdateStep CurrentStep
        {
            get => currentStep;
            set
            {
                currentStep = value;
                OnPropertyChanged(() => ProgressStatus);
                OnPropertyChanged(() => ProgressStep);
                OnPropertyChanged(() => ProgressValue);
            }
        }

        public double ProgressValue
        {
            get
            {
                var steps = Enum.GetValues(typeof(UpdateStep)).Cast<UpdateStep>().ToArray();

                // 현재 단계까지의 가중치 합계
                double completedWeight = steps.Take(Convert.ToInt32(CurrentStep)).Sum(e => e.GetWeight());

                // 전체 가중치 합계
                double totalWeight = steps.Sum(e => e.GetWeight());

                // 진행률 계산
                return (completedWeight + ProgressDetail) / totalWeight * 100;
            }
        }

        private double progressDetail = 0.0;
        public double ProgressDetail { get => progressDetail; set { progressDetail = value; OnPropertyChanged(() => ProgressDetail); OnPropertyChanged(() => ProgressValue); } }

        public string ProgressStatus => CurrentStep.ToDescription();

        public string ProgressStep => $"{Convert.ToInt32(CurrentStep) + 1} / {Enum.GetValues(typeof(UpdateStep)).Length}";

        private string progressFileName;
        public string ProgressFileName { get => progressFileName; set { progressFileName = value; OnPropertyChanged(() => ProgressFileName); } }

        private string progressFileCounter;
        public string ProgressFileCounter { get => progressFileCounter; set { progressFileCounter = value; OnPropertyChanged(() => ProgressFileCounter); } }

        private string progressSizeCounter;
        public string ProgressSizeCounter { get => progressSizeCounter; set { progressSizeCounter = value; OnPropertyChanged(() => ProgressSizeCounter); } }

        public readonly ExecutionOptions Options;

        private readonly string TempDirPath;

        private string ZipFilePath => Path.Combine(TempDirPath, "download", "update.zip");

        private string PatchFilesPath => Path.Combine(TempDirPath, "files");

        #endregion

        #region Constructor

        public DownloadWindowViewModel(ExecutionOptions options)
        {
            TempDirPath = Path.Combine(Path.GetTempPath(), $"DevFuryUpdate.{Guid.NewGuid()}");
            Options = options;
            Title = options.Title;
        }

        #endregion

        #region Commands

        public ICommand StartCommand => new RelayCommand(async p => await StartAsync());

        public ICommand ClosedCommand => new RelayCommand(async p => await CloseAsync());

        #endregion

        #region Methods

        public async Task StartAsync()
        {
            await WaitForProcessToExitAsync();

            await CreateTemporaryFolderAsync();

            await DownloadUpdateFileAsync(Options.Url, ZipFilePath);

            await ExtractZipFileAsync(ZipFilePath, PatchFilesPath);

            await PatchFilesAsync(PatchFilesPath, Options.Path ?? GetExecutablePath());

            await RemoveTemporaryFolderAsync(TempDirPath);

            await RestartProcess();

            await Application.Current.Dispatcher.BeginInvoke(new Action(async () => await CloseAsync()));
        }

        public async Task CloseAsync()
        {
            await RemoveTemporaryFolderAsync(TempDirPath);

            OwnerWindow?.Close();

            await Task.CompletedTask;
        }

        private async Task WaitForProcessToExitAsync()
        {
            CurrentStep = UpdateStep.WaitForProcessToExit;
            if (Options.ProcessName == null) return;

            // CancellationTokenSource를 사용하여 5초 후 타이머 설정
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // 프로세스 존재 여부 체크
                        if (!IsProcessRunning(Options.ProcessName))
                        {
                            Console.WriteLine("프로세스가 정상 종료되었습니다.");
                            return;
                        }

                        Console.WriteLine("프로세스가 아직 실행 중입니다. 1초 후 다시 확인...");
                        await Task.Delay(1000, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("5초가 지났지만 프로세스가 종료되지 않아 강제 종료를 시도합니다.");
                }
            }

            // 5초가 지나면 프로세스 강제 종료 시도
            ForceTerminateProcess(Options.ProcessName);

            await Task.Delay(200);
        }

        private async Task RestartProcess()
        {
            CurrentStep = UpdateStep.RestartProcess;

            if (Options.RedirectCommand == null) return;

            string[] arr = Options.RedirectCommand.Split(' ');

            if (string.IsNullOrWhiteSpace(arr.FirstOrDefault())) return;

            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = arr.FirstOrDefault(),
                Arguments = string.Join(" ", arr.Skip(1)),
                UseShellExecute = false
            };

            Process.Start(psi);

            Application.Current.Shutdown();

            await Task.CompletedTask;
        }

        private async Task CreateTemporaryFolderAsync()
        {
            CurrentStep = UpdateStep.CreateTemporaryFolder;

            CreateFolder(TempDirPath);

            await Task.Delay(200);
        }

        private async Task DownloadUpdateFileAsync(string url, string destinationPath)
        {
            CurrentStep = UpdateStep.DownloadUpdateFile;

            FileInfo fi = new FileInfo(destinationPath);

            CreateFolder(fi.DirectoryName);

            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                // 파일 스트림 다운로드
                response.EnsureSuccessStatusCode();

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                long receivedBytes = 0L;
                byte[] buffer = new byte[8192]; // 8 KB 버퍼

                ProgressFileName = "";
                ProgressFileCounter = "(1 / 1)";
                ProgressSizeCounter = "";
                ProgressDetail = 0.0;

                // 파일을 로컬 경로에 저장
                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true))
                {
                    int bytesRead;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        receivedBytes += bytesRead;

                        if (totalBytes > 0)
                        {
                            ProgressSizeCounter = $"{FormatBytes(receivedBytes, 1, true)} / {FormatBytes(totalBytes, 1, true)}";
                            ProgressDetail = (double)receivedBytes / totalBytes * UpdateStep.DownloadUpdateFile.GetWeight();
                        }
                    }
                }
            }

            await Task.Delay(200);
        }

        private async Task ExtractZipFileAsync(string zipFilePath, string extractPath)
        {
            CurrentStep = UpdateStep.ExtractZipFile;
            ProgressSizeCounter = "";
            ProgressDetail = 0.0;

            CreateFolder(extractPath);

            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
            {
                long totalBytes = archive.Entries.Sum(entry => entry.Length);
                long extractedBytes = 0;
                int extractedCount = 0;

                ProgressSizeCounter = $"{FormatBytes(extractedBytes, 1, true)} / {FormatBytes(totalBytes, 1, true)}";

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);

                    // 디렉토리인지 파일인지 확인
                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        // 디렉토리일 경우 생성
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    // 파일인 경우, 디렉토리가 없으면 생성
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    ProgressFileName = entry.FullName;
                    ProgressFileCounter = $"({++extractedCount} / {archive.Entries.Count})";

                    using (FileStream outputFileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await outputFileStream.WriteAsync(buffer, 0, bytesRead);
                            extractedBytes += bytesRead;

                            if (totalBytes > 0)
                            {
                                ProgressSizeCounter = $"{FormatBytes(extractedBytes, 1, true)} / {FormatBytes(totalBytes, 1, true)}";
                                ProgressDetail = (double)extractedBytes / totalBytes * UpdateStep.ExtractZipFile.GetWeight();
                            }
                        }
                    }
                }
            }

            await Task.Delay(200);
        }

        private async Task PatchFilesAsync(string sourceDir, string destinationDir)
        {
            CurrentStep = UpdateStep.PatchFiles;

            ProgressFileName = "";
            ProgressFileCounter = "";
            ProgressSizeCounter = "";
            ProgressDetail = 0.0;

            // 원본 경로가 존재하는지 확인
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"원본 경로가 존재하지 않습니다: {sourceDir}");
            }

            List<string> filePaths = GetAllFiles(sourceDir);
            long totalFileSize = filePaths.Select(e => new FileInfo(e)).Sum(e => e.Length);
            long copiedFileSize = 0;
            int copiedFileCount = 0;

            ProgressSizeCounter = $"{FormatBytes(copiedFileSize, 1, true)} / {FormatBytes(totalFileSize, 1, true)}";

            foreach (string filePath in filePaths)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string relativePath = GetRelativePath(sourceDir, filePath);
                string destFilePath = Path.Combine(destinationDir, relativePath);
                FileInfo destFileInfo = new FileInfo(destFilePath);
                if (!destFileInfo.Directory?.Exists ?? false) destFileInfo.Directory?.Create();

                ProgressFileName = relativePath;
                ProgressFileCounter = $"{++copiedFileCount} / {filePaths.Count}";

                // 덮어쓰기로 파일 복사
                File.Copy(filePath, destFilePath, true);

                copiedFileSize += fileInfo.Length;

                ProgressSizeCounter = $"{FormatBytes(copiedFileSize, 1, true)} / {FormatBytes(totalFileSize, 1, true)}";
                ProgressDetail = (double)copiedFileSize / totalFileSize * UpdateStep.PatchFiles.GetWeight();
            }

            await Task.Delay(200);
        }

        private async Task RemoveTemporaryFolderAsync(string path)
        {
            CurrentStep = UpdateStep.RemoveTemporaryFolder;

            ProgressFileName = "";
            ProgressFileCounter = "";
            ProgressSizeCounter = "";
            ProgressDetail = 0.0;

            if (Directory.Exists(path)) Directory.Delete(path, true);

            await Task.Delay(200);
        }

        private string GetExecutablePath()
        {
            string exePath = AppContext.BaseDirectory;

            return exePath;
        }

        private bool IsProcessRunning(string processName)
        {
            if (processName == null) return false;

            Process[] runningProcesses = Process.GetProcessesByName(processName);
            return runningProcesses.Length > 0;
        }

        private void ForceTerminateProcess(string processName)
        {
            try
            {
                if (processName == null) return;

                Process[] runningProcesses = Process.GetProcessesByName(processName);

                foreach (Process process in runningProcesses)
                {
                    process.CloseMainWindow();
                    process.WaitForExit();
                }

                Console.WriteLine("프로세스를 강제로 종료했습니다.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"프로세스를 강제 종료할 수 없습니다: {ex.Message}");
            }
        }

        private void CreateFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        private List<string> GetAllFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"디렉토리가 존재하지 않습니다: {directoryPath}");
            }

            List<string> files = new List<string>();

            // 모든 파일을 재귀적으로 가져옴
            GetFilesRecursive(directoryPath, files);

            return files;
        }

        private void GetFilesRecursive(string directoryPath, List<string> files)
        {
            // 현재 디렉토리의 모든 파일을 가져옴
            files.AddRange(Directory.GetFiles(directoryPath));

            // 현재 디렉토리의 모든 하위 디렉토리에 대해 재귀 호출
            foreach (string subDir in Directory.GetDirectories(directoryPath))
            {
                GetFilesRecursive(subDir, files);
            }
        }

        private string FormatBytes(long bytes, int decimalPlaces, bool showByteType)
        {
            double newBytes = bytes;
            string formatString = "{0";
            string byteType = "B";

            if (newBytes > 1024 && newBytes < 1048576)
            {
                newBytes /= 1024;
                byteType = "KB";
            }
            else if (newBytes > 1048576 && newBytes < 1073741824)
            {
                newBytes /= 1048576;
                byteType = "MB";
            }
            else
            {
                newBytes /= 1073741824;
                byteType = "GB";
            }

            if (decimalPlaces > 0)
                formatString += ":0.";

            for (int i = 0; i < decimalPlaces; i++)
                formatString += "0";

            formatString += "}";

            if (showByteType)
                formatString += byteType;

            return string.Format(formatString, newBytes);
        }
        private string GetRelativePath(string basePath, string targetPath)
        {
            Uri baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? basePath : basePath + Path.DirectorySeparatorChar);
            Uri targetUri = new Uri(targetPath);

            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        #endregion
    }
}
