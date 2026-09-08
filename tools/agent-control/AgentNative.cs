using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public static class AgentNative
{
    public const int INPUT_MOUSE = 0;
    public const int INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_SCANCODE = 0x0008;
    public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const uint MOUSEEVENTF_MOVE = 0x0001;
    public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    public const uint MOUSEEVENTF_LEFTUP = 0x0004;
    public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;
    public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
    public const uint SW_RESTORE = 9;
    public const int SW_SHOW = 5;

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public static string ScreenInfo()
    {
        Rectangle v = SystemInformation.VirtualScreen;
        return v.X + "," + v.Y + "," + v.Width + "," + v.Height;
    }

    public static void Screenshot(string path, int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0)
        {
            Rectangle v = SystemInformation.VirtualScreen;
            x = v.X;
            y = v.Y;
            w = v.Width;
            h = v.Height;
        }
        using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(w, h), CopyPixelOperation.SourceCopy);
            bmp.Save(path, ImageFormat.Png);
        }
    }

    public static void Move(int x, int y)
    {
        Rectangle v = SystemInformation.VirtualScreen;
        int nx = (int)Math.Round((double)(x - v.X) * 65535.0 / Math.Max(1, v.Width - 1));
        int ny = (int)Math.Round((double)(y - v.Y) * 65535.0 / Math.Max(1, v.Height - 1));
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dx = nx;
        inputs[0].u.mi.dy = ny;
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void Click(int x, int y, string button)
    {
        Move(x, y);
        uint down;
        uint up;
        if (button == "right")
        {
            down = MOUSEEVENTF_RIGHTDOWN;
            up = MOUSEEVENTF_RIGHTUP;
        }
        else if (button == "middle")
        {
            down = MOUSEEVENTF_MIDDLEDOWN;
            up = MOUSEEVENTF_MIDDLEUP;
        }
        else
        {
            down = MOUSEEVENTF_LEFTDOWN;
            up = MOUSEEVENTF_LEFTUP;
        }
        INPUT[] inputs = new INPUT[2];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dwFlags = down;
        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dwFlags = up;
        SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void Scroll(int amount)
    {
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.mouseData = unchecked((uint)amount);
        inputs[0].u.mi.dwFlags = MOUSEEVENTF_WHEEL;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static void Key(ushort vk, bool down)
    {
        uint flags = down ? 0u : KEYEVENTF_KEYUP;
        // arrows, insert/delete/home/end/pgup/pgdn, right ctrl/alt
        if (vk == 0x21 || vk == 0x22 || vk == 0x23 || vk == 0x24 || vk == 0x25 || vk == 0x26 || vk == 0x27 || vk == 0x28 || vk == 0x2D || vk == 0x2E || vk == 0xA3 || vk == 0xA5)
        {
            flags |= KEYEVENTF_EXTENDEDKEY;
        }
        INPUT[] inputs = new INPUT[1];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vk;
        inputs[0].u.ki.wScan = (ushort)MapVirtualKey(vk, 0);
        if (vk == 0xA1) inputs[0].u.ki.wScan = 0x36;
        inputs[0].u.ki.dwFlags = flags;
        SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    public static bool FocusWindow(string needle)
    {
        foreach (Process p in Process.GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle == IntPtr.Zero) continue;
                string title = p.MainWindowTitle ?? "";
                if (title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (IsIconic(p.MainWindowHandle)) ShowWindow(p.MainWindowHandle, (int)SW_RESTORE);
                ShowWindow(p.MainWindowHandle, SW_SHOW);
                return SetForegroundWindow(p.MainWindowHandle);
            }
            catch { }
        }
        return false;
    }

    public static string WindowRect(string needle)
    {
        foreach (Process p in Process.GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle == IntPtr.Zero) continue;
                string title = p.MainWindowTitle ?? "";
                if (title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                RECT r;
                if (!GetWindowRect(p.MainWindowHandle, out r)) continue;
                return title.Replace("|", "/") + "|" + r.Left + "," + r.Top + "," + (r.Right - r.Left) + "," + (r.Bottom - r.Top);
            }
            catch { }
        }
        return "";
    }

    public static string ListWindows()
    {
        StringBuilder sb = new StringBuilder();
        foreach (Process p in Process.GetProcesses())
        {
            try
            {
                if (p.MainWindowHandle == IntPtr.Zero) continue;
                string title = p.MainWindowTitle;
                if (string.IsNullOrWhiteSpace(title)) continue;
                sb.Append(p.Id).Append('\t').Append(p.ProcessName).Append('\t').Append(title).AppendLine();
            }
            catch { }
        }
        return sb.ToString();
    }
}
