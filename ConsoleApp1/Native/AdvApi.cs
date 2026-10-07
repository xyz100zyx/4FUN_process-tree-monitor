using Microsoft.Diagnostics.Tracing;
using System.Runtime.InteropServices;

namespace ConsoleApp1.Native;

internal static class AdvApi
{
    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern uint EnableTraceEx2(
        long traceHandle,
        ref Guid providerId,
        uint controlCode,
        TraceEventLevel level,
        ulong matchAnyKeyword,
        ulong matchAllKeyword,
        uint timeout,
        IntPtr enableParameters
    );
}