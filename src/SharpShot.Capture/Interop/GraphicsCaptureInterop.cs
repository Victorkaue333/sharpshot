using System.Runtime.InteropServices;
using Windows.Graphics.Capture;

namespace SharpShot.Capture.Interop;

/// <summary>COM interop for creating a <see cref="GraphicsCaptureItem"/> from an HMONITOR (no picker UI).</summary>
[ComImport]
[Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IGraphicsCaptureItemInterop
{
    nint CreateForWindow(nint window, [In] ref Guid riid);

    nint CreateForMonitor(nint monitor, [In] ref Guid riid);
}

internal static class GraphicsCaptureInterop
{
    private static readonly Guid GraphicsCaptureItemIid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    public static GraphicsCaptureItem CreateItemForMonitor(nint monitorHandle)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var iid = GraphicsCaptureItemIid;
        var abi = interop.CreateForMonitor(monitorHandle, ref iid);
        try
        {
            return GraphicsCaptureItem.FromAbi(abi);
        }
        finally
        {
            Marshal.Release(abi);
        }
    }
}
