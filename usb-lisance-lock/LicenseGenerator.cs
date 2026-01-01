using System;
using System.IO;
using System.Text.Json;

public static class LicenseGenerator
{
    public static void GenerateLicense(
        string productName,
        string usbSerial,
        DateTime expirationDate,
        string privateKeyPath,
        string outputPath)
    {
        if (!File.Exists(privateKeyPath))
        {
            throw new FileNotFoundException($"Private key file not found: {privateKeyPath}");
        }

        // Read private key
        string privateKey = File.ReadAllText(privateKeyPath);

        // Create the message that will be signed
        string expires = expirationDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        string message = $"{productName}|{usbSerial}|{expires}";

        // Sign the message
        byte[] signature = Crypto.SignData(message, privateKey);
        string signatureBase64 = Convert.ToBase64String(signature);

        // Create license object
        var license = new
        {
            product = productName,
            usbSerial = usbSerial,
            expires = expires,
            signature = signatureBase64
        };

        // Serialize to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(license, options);

        // Write to file
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"License generated successfully!");
        Console.WriteLine($"Product: {productName}");
        Console.WriteLine($"USB Serial: {usbSerial}");
        Console.WriteLine($"Expires: {expires}");
        Console.WriteLine($"Output: {outputPath}");
    }
}

