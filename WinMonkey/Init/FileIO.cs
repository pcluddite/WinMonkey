using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WinMonkey {
    public class FileIO {
        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATA {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);
        [DllImport("kernel32.dll")]
        private static extern bool FindClose(IntPtr hFindFile);

        public static string[] FindFiles(string dir) {
            List<string> ret = new List<string>();
            WIN32_FIND_DATA data;
            IntPtr handle = FindFirstFile(dir, out data);
            if (handle != new IntPtr(-1)) {
                do {
                    ret.Add(data.cFileName);
                }
                while (FindNextFile(handle, out data) != 0);
                FindClose(handle);
            }
            return ret.ToArray();
        }
    }
}
