using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace CollageMaker.Services;

/// <summary>
/// Service for setting Windows desktop background.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DesktopBackgroundService
{
    /// <summary>
    /// Sets the specified image as the Windows desktop background.
    /// </summary>
    /// <param name="imagePath">Full path to the image file.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public static bool SetDesktopBackground(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return false;
        }

        if (!File.Exists(imagePath))
        {
            return false;
        }

        try
        {
            string fullPath = Path.GetFullPath(imagePath);
            
            IDesktopWallpaper desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaper();
            try
            {
                desktopWallpaper.SetWallpaper(null, fullPath);
            }
            finally
            {
                Marshal.ReleaseComObject(desktopWallpaper);
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}
