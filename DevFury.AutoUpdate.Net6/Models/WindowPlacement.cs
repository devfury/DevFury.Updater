using System.Runtime.InteropServices;

namespace DevFury.AutoUpdate
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPlacement
    {
        public int length;
        public int flags;
        public ShowWindowCommands showCmd;
        public System.Drawing.Point ptMinPosition;
        public System.Drawing.Point ptMaxPosition;
        public System.Drawing.Rectangle rcNormalPosition;
    }

    internal enum ShowWindowCommands
    {
        Hide = 0,
        Normal = 1,
        Minimized = 2,
        Maximized = 3,
    }
}
