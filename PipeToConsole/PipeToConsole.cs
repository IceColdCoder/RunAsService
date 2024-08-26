using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PipeToConsole
{
    public class PipeToConsole
    {
        const uint WM_KEYDOWN = 0x100;
        const uint KEY_ENTER = 13;

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern short VkKeyScanA(char ch);

        public static void SendInput(string input, string windowTitle)
        {
            IntPtr windowHandle = FindWindowByCaption(IntPtr.Zero, windowTitle);

            foreach (char c in input)
            {
                PostMessage(windowHandle, WM_KEYDOWN, ((IntPtr)VkKeyScanA(c)), IntPtr.Zero);
            }
            PostMessage(windowHandle, WM_KEYDOWN, ((IntPtr)KEY_ENTER), IntPtr.Zero);
        }
    }
}
