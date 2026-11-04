using System.Runtime.InteropServices;

namespace SharpShot.Capture.Interop;

/// <summary>Win32 / Direct3D entry points used by the capture engine.</summary>
internal static partial class NativeMethods
{
    public const int MonitorDefaultToNearest = 2;
    public const uint MonitorInfoPrimary = 0x1;
    public const int MdtEffectiveDpi = 0;

    public const int D3DDriverTypeHardware = 1;
    public const int D3DDriverTypeWarp = 5;
    public const uint D3D11CreateDeviceBgraSupport = 0x20;
    public const uint D3D11SdkVersion = 7;

    public const int DxgiErrorDeviceRemoved = unchecked((int)0x887A0005);
    public const int DxgiErrorDeviceReset = unchecked((int)0x887A0007);

    public static readonly Guid IidIDxgiDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEXW
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    public delegate bool MonitorEnumProc(nint hMonitor, nint hdcMonitor, ref RECT lprcMonitor, nint dwData);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumProc lpfnEnum, nint dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetMonitorInfoW(nint hMonitor, ref MONITORINFOEXW lpmi);

    [DllImport("user32.dll")]
    public static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("shcore.dll")]
    public static extern int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("dwmapi.dll")]
    public static extern int DwmFlush();

    [DllImport("d3d11.dll")]
    public static extern int D3D11CreateDevice(
        nint pAdapter,
        int driverType,
        nint software,
        uint flags,
        nint pFeatureLevels,
        uint featureLevels,
        uint sdkVersion,
        out nint ppDevice,
        out int pFeatureLevel,
        out nint ppImmediateContext);

    [DllImport("d3d11.dll")]
    public static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint graphicsDevice);
}
