using System.Runtime.InteropServices;
using System.Threading;

namespace NMica.Hwc
{
    public static class SystemEvents
    {
        private static readonly ManualResetEvent ExitWaitHandle = new ManualResetEvent(false);

        static SystemEvents()
        {
            SetConsoleCtrlHandler(OnSystemEvent, true);
        }
        public delegate bool ConsoleEventDelegate(CtrlEvent eventType);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
        static bool OnSystemEvent(CtrlEvent eventType)
        {
            ExitWaitHandle.Set();
            return true;
        }

        /// <summary>
        /// Blocks until one of the Ctrl events is received
        /// </summary>
        public static void Wait() => ExitWaitHandle.WaitOne();
        public enum CtrlEvent
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 5,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
    }
}
