using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class UsbScanner
{
    public static IEnumerable<DriveInfo> GetRemovableDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady);
    }
}
