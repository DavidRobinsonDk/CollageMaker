using Windows.Win32;
using Windows.Win32.Foundation;
using System.Runtime.InteropServices;

namespace CollageMaker.Services;

/// <summary>
/// Service for controlling window visibility.
/// </summary>
internal static partial class WindowVisibilityService
{
    private const uint ATTACH_PARENT_PROCESS = uint.MaxValue;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(uint dwProcessId);

    /// <summary>
    /// Allocates a console window for the application, or attaches to parent console if one exists.
    /// </summary>
    /// <returns>True if a console was allocated or attached, false otherwise.</returns>
    public static bool AllocateConsoleWindow()
    {
        var existingConsoleWindow = PInvoke.GetConsoleWindow();
        return existingConsoleWindow == IntPtr.Zero && (AttachConsole(ATTACH_PARENT_PROCESS) || (bool)PInvoke.AllocConsole());
    }
}
