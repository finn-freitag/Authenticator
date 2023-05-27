using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Authenticator
{
    public class SystemTime // https://stackoverflow.com/questions/650849/change-system-date-programmatically
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SYSTEMTIME st);

        public static void Set(DateTime time)
        {
            time = time.ToUniversalTime();
            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = (short)time.Year;
            st.wMonth = (short)time.Month;
            st.wDay = (short)time.Day;
            st.wHour = (short)time.Hour;
            st.wMinute = (short)time.Minute;
            st.wSecond = (short)time.Second;

            SetSystemTime(ref st);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }
    }
}
