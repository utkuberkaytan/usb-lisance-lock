using System;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static ManagementEventWatcher? insertWatcher;
    private static readonly string PublicKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "public_key.pem");

    static void Main()
    {
        Console.WriteLine("USB lisans bekleniyor...");
        
        if (!File.Exists(PublicKeyPath))
        {
            Console.WriteLine($"HATA: public_key.pem dosyası bulunamadı: {PublicKeyPath}");
            Console.WriteLine("Çıkmak için Enter'a basın...");
            Console.ReadLine();
            return;
        }

        StartUsbWatcher();
        
        // Graceful shutdown
        Console.WriteLine("Çıkmak için Enter'a basın...");
        Console.ReadLine();
        
        Cleanup();
    }

    static void StartUsbWatcher()
    {
        try
        {
            insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery(
                    "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2"));

            insertWatcher.EventArrived += async (s, e) =>
            {
                try
                {
                    string? driveLetter = e.NewEvent["DriveName"]?.ToString();
                    if (string.IsNullOrEmpty(driveLetter))
                    {
                        Console.WriteLine("USB takıldı ancak sürücü harfi alınamadı");
                        return;
                    }

                    Console.WriteLine($"USB takıldı: {driveLetter}");
                    Console.WriteLine("Lisans kontrol ediliyor...");
                    
                    // Wait for USB to be fully mounted and accessible
                    await Task.Delay(1500);
                    
                    await CheckUsbLicenseAsync(driveLetter);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            };

            insertWatcher.Start();
            Console.WriteLine("USB izleyici başlatıldı.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"USB izleyici başlatılamadı: {ex.Message}");
        }
    }

    static async Task CheckUsbLicenseAsync(string driveLetter)
    {
        try
        {
            // Get the physical serial number of the USB drive
            string? usbSerial = GetUsbPhysicalSerialFromDrive(driveLetter);

            if (string.IsNullOrEmpty(usbSerial))
            {
                Console.WriteLine($"USB seri numarası alınamadı: {driveLetter}");
                return;
            }

            Console.WriteLine($"Bulunan USB Serial: {usbSerial}");

            // Check if license.json exists on the USB
            string licensePath = Path.Combine(driveLetter, "license.json");
            if (!File.Exists(licensePath))
            {
                Console.WriteLine($"license.json dosyası bulunamadı: {licensePath}");
                Console.WriteLine("Yanlış USB - Lisans dosyası yok");
                return;
            }

            // Read public key
            string publicKey = await File.ReadAllTextAsync(PublicKeyPath);

            // Verify license
            bool isValid = LicenseVerifier.Verify(licensePath, publicKey);
            
            if (isValid)
            {
                // Double-check that the USB serial in license matches
                var json = await File.ReadAllTextAsync(licensePath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                string licenseSerial = doc.RootElement.GetProperty("usbSerial").GetString() ?? "";

                if (licenseSerial.Equals(usbSerial, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("✓ Doğru USB – Lisans geçerli");
                }
                else
                {
                    Console.WriteLine($"✗ Yanlış USB - Lisans başka bir USB için ({licenseSerial})");
                }
            }
            else
            {
                Console.WriteLine("✗ Lisans geçersiz veya süresi dolmuş");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lisans kontrolü sırasında hata: {ex.Message}");
        }
    }

    static string? GetUsbPhysicalSerialFromDrive(string driveLetter)
    {
        try
        {
            // Normalize drive letter (e.g., "E:" or "E:\" -> "E:")
            string normalizedDrive = driveLetter.Replace("\\", "").Trim();
            if (!normalizedDrive.EndsWith(":"))
                normalizedDrive += ":";
            
            using var searcher = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_LogicalDisk WHERE DeviceID = '{normalizedDrive}'");
            
            var query = searcher.Get();
            
            // Find the logical disk
            foreach (ManagementObject logicalDisk in query)
            {
                try
                {
                    string deviceId = logicalDisk["DeviceID"]?.ToString() ?? "";
                    if (!deviceId.Equals(normalizedDrive, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Get the partition
                    string partition = logicalDisk["VolumeSerialNumber"]?.ToString() ?? "";
                    var partitions = logicalDisk.GetRelated("Win32_DiskDriveToDiskPartition");
                    
                    foreach (ManagementObject diskPartition in partitions)
                    {
                        try
                        {
                            var drives = diskPartition.GetRelated("Win32_DiskDrive");
                            foreach (ManagementObject drive in drives)
                            {
                                try
                                {
                                    string interfaceType = drive["InterfaceType"]?.ToString() ?? "";
                                    if (interfaceType == "USB")
                                    {
                                        string? serial = drive["SerialNumber"]?.ToString()?.Trim();
                                        if (!string.IsNullOrEmpty(serial))
                                            return serial;
                                    }
                                }
                                finally
                                {
                                    drive.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            diskPartition.Dispose();
                        }
                    }
                }
                finally
                {
                    logicalDisk.Dispose();
                }
            }

            // Fallback: try to get any USB serial (less reliable)
            return GetFirstUsbSerial();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seri numarası alınırken hata: {ex.Message}");
            return GetFirstUsbSerial();
        }
    }

    static string? GetFirstUsbSerial()
    {
        using var searcher = new ManagementObjectSearcher(
            "SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

        foreach (ManagementObject drive in searcher.Get())
        {
            try
            {
                string? serial = drive["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(serial))
                    return serial;
            }
            finally
            {
                drive.Dispose();
            }
        }

        return null;
    }

    static void Cleanup()
    {
        try
        {
            if (insertWatcher != null)
            {
                insertWatcher.Stop();
                insertWatcher.Dispose();
                Console.WriteLine("USB izleyici durduruldu.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Temizlik sırasında hata: {ex.Message}");
        }
    }
}
