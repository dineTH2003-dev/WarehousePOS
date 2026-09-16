using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace WarehousePOS.Infrastructure.Printing;

/// <summary>
/// Native Win32 Print Spooler interface (winspool.drv) for sending raw text and ESC/P2
/// commands directly to hardware dot-matrix printers.
/// </summary>
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private sealed class DocInfoA
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)]
        public string? pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In, MarshalAs(UnmanagedType.LPStruct)] DocInfoA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    /// <summary>
    /// Sends raw byte array directly to the specified Windows printer queue without graphical rasterization.
    /// </summary>
    public static bool SendBytesToPrinter(string printerName, byte[] bytes, string documentName = "WarehousePOS_RawPrint")
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(printerName) || bytes.Length == 0)
        {
            return false;
        }

        IntPtr pUnmanagedBytes = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
            {
                return false;
            }

            try
            {
                var di = new DocInfoA
                {
                    pDocName = documentName,
                    pDataType = "RAW"
                };

                if (!StartDocPrinter(hPrinter, 1, di))
                {
                    return false;
                }

                try
                {
                    if (!StartPagePrinter(hPrinter))
                    {
                        return false;
                    }

                    bool success = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out _);
                    EndPagePrinter(hPrinter);
                    return success;
                }
                finally
                {
                    EndDocPrinter(hPrinter);
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(pUnmanagedBytes);
        }
    }

    /// <summary>
    /// Sends an ASCII/ANSI formatted string directly to the Windows printer queue.
    /// </summary>
    public static bool SendStringToPrinter(string printerName, string text, string documentName = "WarehousePOS_RawPrint")
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        return SendBytesToPrinter(printerName, bytes, documentName);
    }
}
