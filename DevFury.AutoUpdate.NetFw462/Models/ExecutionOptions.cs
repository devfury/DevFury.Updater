using System;
using System.Text;

namespace DevFury.AutoUpdate.Models
{
    public class ExecutionOptions
    {
        public string Title { get; set; }

        public string Url { get; set; }

        public string Path { get; set; }

        public string ProcessName { get; set; }

        public string RedirectCommand { get; set; } = "";

        public bool IsForced { get; set; } = false;

        public ExecutionOptions(string title, string url, string path)
        {
            Title = title;
            Url = url;
            Path = path;
        }

        public static ExecutionOptions ParseArgs(string[] args)
        {
            int index = 0;
            string title = null;
            string url = null;
            string path = null;
            string processName = null;
            string redirectCommand = "";
            bool isForced = false;

            while (index < args.Length)
            {
                string arg = args[index++];

                switch (arg.ToLower())
                {
                    case "-t":
                        title = args[index++];
                        break;
                    case "-o":
                        path = args[index++];
                        break;
                    case "-p":
                        processName = args[index++];
                        break;
                    case "-c":
                        byte[] bytes = Convert.FromBase64String(args[index++]);
                        redirectCommand = Encoding.UTF8.GetString(bytes);
                        break;
                    case "-f":
                        isForced = true;
                        break;
                    default:
                        url = arg;
                        break;
                }
            }

            return new ExecutionOptions(
                title: title ?? "DevFuryUpdate",
                url: url ?? throw new ArgumentNullException("Url"),
                path: path ?? throw new ArgumentNullException("Path"))
            {
                ProcessName = processName,
                RedirectCommand = redirectCommand,
                IsForced = isForced,
            };
        }

        public bool IsValid()
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri uriResult))
                return false;

            if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
                return false;

            if (Path == null)
                return false;

            return true;
        }
    }
}
