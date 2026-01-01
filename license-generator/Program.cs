using System;
using System.IO;
using System.Globalization;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== USB License Generator ===");
        Console.WriteLine();

        // Parse command line arguments or prompt for input
        string? privateKeyPath;
        string? productName;
        string? usbSerial;
        string? expirationDateStr;
        string? outputPath;

        if (args.Length >= 5)
        {
            privateKeyPath = args[0];
            productName = args[1];
            usbSerial = args[2];
            expirationDateStr = args[3];
            outputPath = args[4];
        }
        else
        {
            // Interactive mode
            Console.WriteLine("Enter license details (or press Enter for interactive mode):");
            Console.WriteLine();

            Console.Write("Private key file path (e.g., private_key.pem): ");
            privateKeyPath = Console.ReadLine();

            Console.Write("Product name: ");
            productName = Console.ReadLine();

            Console.Write("USB Serial Number: ");
            usbSerial = Console.ReadLine();

            Console.Write("Expiration date (yyyy-MM-dd or yyyy-MM-dd HH:mm:ss): ");
            expirationDateStr = Console.ReadLine();

            Console.Write("Output license file path (e.g., license.json): ");
            outputPath = Console.ReadLine();
        }

        // Validate inputs
        if (string.IsNullOrWhiteSpace(privateKeyPath) ||
            string.IsNullOrWhiteSpace(productName) ||
            string.IsNullOrWhiteSpace(usbSerial) ||
            string.IsNullOrWhiteSpace(expirationDateStr) ||
            string.IsNullOrWhiteSpace(outputPath))
        {
            PrintUsage();
            return;
        }

        // Expand relative paths
        privateKeyPath = Path.GetFullPath(privateKeyPath);
        outputPath = Path.GetFullPath(outputPath);

        try
        {
            // Parse expiration date
            DateTime expirationDate;
            if (DateTime.TryParseExact(expirationDateStr, new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss" }, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                expirationDate = parsedDate;
            }
            else if (DateTime.TryParse(expirationDateStr, out DateTime parsedDate2))
            {
                expirationDate = parsedDate2;
            }
            else
            {
                Console.WriteLine($"Error: Invalid date format: {expirationDateStr}");
                Console.WriteLine("Please use format: yyyy-MM-dd or yyyy-MM-dd HH:mm:ss");
                return;
            }

            // Generate license
            LicenseGenerator.GenerateLicense(
                productName!,
                usbSerial!,
                expirationDate,
                privateKeyPath,
                outputPath
            );
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating license: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  LicenseGenerator.exe <private_key> <product> <usb_serial> <expiration_date> <output_file>");
        Console.WriteLine();
        Console.WriteLine("Example:");
        Console.WriteLine("  LicenseGenerator.exe private_key.pem \"MyProduct\" \"123456789\" \"2025-12-31\" license.json");
        Console.WriteLine();
        Console.WriteLine("Or run without arguments for interactive mode.");
    }
}
