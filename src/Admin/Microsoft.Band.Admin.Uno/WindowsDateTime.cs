using System.Runtime.InteropServices;

namespace Microsoft.Band.Admin;

internal static class WindowsDateTime
{
    public struct SYSTEMTIME
    {
        [MarshalAs(UnmanagedType.U2)]
        public short Year;

        [MarshalAs(UnmanagedType.U2)]
        public short Month;

        [MarshalAs(UnmanagedType.U2)]
        public short DayOfWeek;

        [MarshalAs(UnmanagedType.U2)]
        public short Day;

        [MarshalAs(UnmanagedType.U2)]
        public short Hour;

        [MarshalAs(UnmanagedType.U2)]
        public short Minute;

        [MarshalAs(UnmanagedType.U2)]
        public short Second;

        [MarshalAs(UnmanagedType.U2)]
        public short Milliseconds;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TIME_ZONE_INFORMATION
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Bias;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string StandardName;

        public SYSTEMTIME StandardDate;

        [MarshalAs(UnmanagedType.I4)]
        public int StandardBias;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DaylightName;

        public SYSTEMTIME DaylightDate;

        [MarshalAs(UnmanagedType.I4)]
        public int DaylightBias;
    }

    public enum TimezoneDaylightStatus
    {
        NotApplicable,
        Standard,
        Daylight
    }

    private static class SafeNativeMethods
    {
        [DllImport("api-ms-win-core-timezone-l1-1-0.dll", SetLastError = true)]
        public static extern int GetTimeZoneInformation(out TIME_ZONE_INFORMATION timeZoneInformation);
    }

    private static TimezoneDaylightStatus GetTimeZoneInformation(out TIME_ZONE_INFORMATION tzInfo)
    {
        return SafeNativeMethods.GetTimeZoneInformation(out tzInfo).ToTimezoneDaylightStatus();
    }

    private static TimezoneDaylightStatus ToTimezoneDaylightStatus(this int result)
    {
        return result switch
        {
            0 => TimezoneDaylightStatus.NotApplicable, 
            1 => TimezoneDaylightStatus.Standard, 
            2 => TimezoneDaylightStatus.Daylight, 
            _ => throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()), 
        };
    }

    public static CargoTimeZoneInfo GetWindowsCurrentTimeZone()
    {
        GetTimeZoneInformation(out var tzInfo);
        return tzInfo.ToCargoTimeZone();
    }

    public static CargoTimeZoneInfo ToCargoTimeZone(this TIME_ZONE_INFORMATION timeZoneInfo)
    {
        CargoTimeZoneInfo cargoTimeZoneInfo = new CargoTimeZoneInfo();
        if (timeZoneInfo.DaylightDate.Day == 0 && timeZoneInfo.DaylightDate.DayOfWeek == 0)
        {
            cargoTimeZoneInfo.Name = timeZoneInfo.StandardName;
            cargoTimeZoneInfo.ZoneOffsetMinutes = (short)(-(timeZoneInfo.Bias + timeZoneInfo.StandardBias));
            cargoTimeZoneInfo.DaylightOffsetMinutes = 0;
            cargoTimeZoneInfo.StandardDate = new CargoSystemTime();
            cargoTimeZoneInfo.DaylightDate = new CargoSystemTime();
        }
        else
        {
            cargoTimeZoneInfo.Name = timeZoneInfo.StandardName;
            cargoTimeZoneInfo.ZoneOffsetMinutes = (short)(-(timeZoneInfo.Bias + timeZoneInfo.StandardBias));
            cargoTimeZoneInfo.DaylightOffsetMinutes = (short)(-timeZoneInfo.DaylightBias);
            cargoTimeZoneInfo.StandardDate = TranslateWindowsDSTTransitionDateToCargo(timeZoneInfo.StandardDate);
            cargoTimeZoneInfo.DaylightDate = TranslateWindowsDSTTransitionDateToCargo(timeZoneInfo.DaylightDate);
        }
        return cargoTimeZoneInfo;
    }

    private static CargoSystemTime TranslateWindowsDSTTransitionDateToCargo(SYSTEMTIME windowsSystemTime)
    {
        ushort day = 0;
        ushort dayOfWeek = 0;
        if (windowsSystemTime.Year == 0)
        {
            dayOfWeek = (ushort)(windowsSystemTime.DayOfWeek + 1);
            if (windowsSystemTime.Day < 5)
            {
                day = (ushort)((windowsSystemTime.Day - 1) * 7 + 1);
            }
        }
        else
        {
            day = (ushort)windowsSystemTime.Day;
        }
        return new CargoSystemTime
        {
            Year = 0,
            Month = (ushort)windowsSystemTime.Month,
            Day = day,
            DayOfWeek = dayOfWeek,
            Hour = (ushort)windowsSystemTime.Hour,
            Minute = (ushort)windowsSystemTime.Minute,
            Second = (ushort)windowsSystemTime.Second,
            Milliseconds = (ushort)windowsSystemTime.Milliseconds
        };
    }
}
