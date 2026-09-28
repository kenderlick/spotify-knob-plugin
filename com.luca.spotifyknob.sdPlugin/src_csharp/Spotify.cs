using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SpotifyKnob
{
    public static class SpotifyController
    {
        private const int WM_APPCOMMAND = 0x0319;
        private const int APPCOMMAND_MEDIA_PLAY_PAUSE = 14 << 16;
        private const int APPCOMMAND_MEDIA_NEXTTRACK = 11 << 16;
        private const int APPCOMMAND_MEDIA_PREVIOUSTRACK = 12 << 16;

        private const byte VK_UP = 0x26;
        private const byte VK_DOWN = 0x28;
        private const byte VK_RIGHT = 0x27;
        private const byte VK_LEFT = 0x25;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MEDIA_NEXT_TRACK = 0xB0;
        private const byte VK_MEDIA_PREV_TRACK = 0xB1;
        private const byte VK_MEDIA_PLAY_PAUSE = 0xB3;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public static bool IsSpotifyRunning()
        {
            var processes = Process.GetProcessesByName("spotify");
            return processes.Length > 0;
        }

        public static List<int> GetSpotifyProcessIds()
        {
            var list = new List<int>();
            foreach (var p in Process.GetProcessesByName("spotify"))
            {
                list.Add(p.Id);
            }
            return list;
        }

        public static bool LaunchSpotify()
        {
            Program.LogInfo("Attempting to launch Spotify...");
            try
            {
                var psi = new ProcessStartInfo("spotify:") { UseShellExecute = true };
                Process.Start(psi);
                Program.LogInfo("Spotify launched via 'spotify:' URI scheme.");
                return true;
            }
            catch { }

            var paths = new List<string>();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            if (!string.IsNullOrEmpty(appData))
                paths.Add(Path.Combine(appData, "Spotify", "Spotify.exe"));
            if (!string.IsNullOrEmpty(localAppData))
            {
                paths.Add(Path.Combine(localAppData, "Spotify", "Spotify.exe"));
                paths.Add(Path.Combine(localAppData, "Microsoft", "WindowsApps", "Spotify.exe"));
                paths.Add(Path.Combine(localAppData, "Programs", "Spotify", "Spotify.exe"));
            }
            if (!string.IsNullOrEmpty(progFiles))
                paths.Add(Path.Combine(progFiles, "Spotify", "Spotify.exe"));
            if (!string.IsNullOrEmpty(progFilesX86))
                paths.Add(Path.Combine(progFilesX86, "Spotify", "Spotify.exe"));

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        Program.LogInfo("Spotify launched via path: " + path);
                        return true;
                    }
                    catch { }
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe", "/c start spotify:") { CreateNoWindow = true, UseShellExecute = false });
                return true;
            }
            catch { }

            Program.LogError("Failed to launch Spotify through any method.");
            return false;
        }

        public static IntPtr GetSpotifyMainWindow()
        {
            // 1. Direct search for SpotifyMainWindow
            IntPtr hwnd = FindWindow("SpotifyMainWindow", null);
            if (hwnd != IntPtr.Zero) return hwnd;

            // 2. Search Spotify process windows (visible main window)
            var pids = GetSpotifyProcessIds();
            var pidSet = new HashSet<int>(pids);
            IntPtr found = IntPtr.Zero;

            EnumWindows((h, lParam) =>
            {
                uint winPid;
                GetWindowThreadProcessId(h, out winPid);
                if (pidSet.Contains((int)winPid))
                {
                    var sbClass = new StringBuilder(256);
                    GetClassName(h, sbClass, 256);
                    string className = sbClass.ToString();

                    if (className == "SpotifyMainWindow" || className.StartsWith("Chrome_WidgetWin"))
                    {
                        var sbTitle = new StringBuilder(256);
                        GetWindowText(h, sbTitle, 256);
                        string title = sbTitle.ToString();

                        if (!string.IsNullOrEmpty(title))
                        {
                            found = h;
                            return false; // Found main window, stop
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        public static bool TogglePlayPause()
        {
            if (!IsSpotifyRunning())
            {
                return LaunchSpotify();
            }

            IntPtr hwnd = GetSpotifyMainWindow();
            if (hwnd != IntPtr.Zero)
            {
                SendMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_PLAY_PAUSE);
                PostMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_PLAY_PAUSE);
                Program.LogInfo("Play/Pause sent directly to Spotify window: " + hwnd);
                return true;
            }

            // Fallback: global media key
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Program.LogInfo("Play/Pause sent via VK_MEDIA_PLAY_PAUSE fallback.");
            return true;
        }

        public static bool EnsurePlay()
        {
            if (!IsSpotifyRunning())
            {
                return LaunchSpotify();
            }

            var info = GetTrackInfo();
            if (!info.IsPlaying)
            {
                return TogglePlayPause();
            }

            return true;
        }

        public static bool NextTrack()
        {
            if (!IsSpotifyRunning()) return false;

            IntPtr hwnd = GetSpotifyMainWindow();
            if (hwnd != IntPtr.Zero)
            {
                SendMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_NEXTTRACK);
                PostMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_NEXTTRACK);
                Program.LogInfo("NextTrack sent directly to Spotify window: " + hwnd);
                return true;
            }

            // Fallback: global media key
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Program.LogInfo("NextTrack sent via VK_MEDIA_NEXT_TRACK fallback.");
            return true;
        }

        public static bool PreviousTrack()
        {
            if (!IsSpotifyRunning()) return false;

            IntPtr hwnd = GetSpotifyMainWindow();
            if (hwnd != IntPtr.Zero)
            {
                SendMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_PREVIOUSTRACK);
                PostMessage(hwnd, WM_APPCOMMAND, IntPtr.Zero, (IntPtr)APPCOMMAND_MEDIA_PREVIOUSTRACK);
                Program.LogInfo("PreviousTrack sent directly to Spotify window: " + hwnd);
                return true;
            }

            // Fallback: global media key
            keybd_event(VK_MEDIA_PREV_TRACK, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            Program.LogInfo("PreviousTrack sent via VK_MEDIA_PREV_TRACK fallback.");
            return true;
        }

        public static void SendVolumeKeys(bool up)
        {
            byte vkKey = up ? VK_UP : VK_DOWN;
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(vkKey, 0, 0, UIntPtr.Zero);
            keybd_event(vkKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static SpotifyTrackInfo GetTrackInfo()
        {
            var info = new SpotifyTrackInfo();
            var pids = GetSpotifyProcessIds();
            if (pids.Count == 0)
            {
                info.IsRunning = false;
                info.IsPlaying = false;
                info.FullTitle = "Spotify Chiuso";
                info.Title = "Chiuso";
                info.Artist = "Spotify";
                return info;
            }

            info.IsRunning = true;
            var pidSet = new HashSet<int>(pids);
            string foundPlayingTitle = null;
            string foundIdleTitle = null;

            EnumWindows((h, lParam) =>
            {
                uint winPid;
                GetWindowThreadProcessId(h, out winPid);
                if (pidSet.Contains((int)winPid))
                {
                    var sbClass = new StringBuilder(256);
                    GetClassName(h, sbClass, 256);
                    string className = sbClass.ToString();

                    if (className == "SpotifyMainWindow" || className.StartsWith("Chrome_WidgetWin"))
                    {
                        var sbTitle = new StringBuilder(512);
                        GetWindowText(h, sbTitle, 512);
                        string t = sbTitle.ToString().Trim();

                        if (!string.IsNullOrEmpty(t))
                        {
                            if (t != "Spotify" && t != "Spotify Free" && t != "Spotify Premium" && t != "GDI+ Window (Spotify.exe)")
                            {
                                foundPlayingTitle = t;
                                return false; // Found playing track, stop search!
                            }
                            else
                            {
                                foundIdleTitle = t;
                            }
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (!string.IsNullOrEmpty(foundPlayingTitle))
            {
                info.IsPlaying = true;
                info.FullTitle = foundPlayingTitle;
                int dashIdx = foundPlayingTitle.IndexOf(" - ");
                if (dashIdx > 0)
                {
                    info.Artist = foundPlayingTitle.Substring(0, dashIdx).Trim();
                    info.Title = foundPlayingTitle.Substring(dashIdx + 3).Trim();
                }
                else
                {
                    info.Artist = "Spotify";
                    info.Title = foundPlayingTitle;
                }
            }
            else
            {
                info.IsPlaying = false;
                info.FullTitle = "In Pausa";
                info.Title = "In Pausa";
                info.Artist = "Spotify";
            }

            return info;
        }
    }

    public class SpotifyTrackInfo
    {
        public bool IsRunning;
        public bool IsPlaying;
        public string Artist = "";
        public string Title = "";
        public string FullTitle = "";
    }
}
