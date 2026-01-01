using System;
using System.IO;
using System.Text.Json;

public static class LicenseVerifier
{
    public static bool Verify(string licensePath, string publicKey)
    {
        var json = File.ReadAllText(licensePath);
        using var doc = JsonDocument.Parse(json);

        string product = doc.RootElement.GetProperty("product").GetString()!;
        string usbSerial = doc.RootElement.GetProperty("usbSerial").GetString()!;
        string expires = doc.RootElement.GetProperty("expires").GetString()!;
        string sigBase64 = doc.RootElement.GetProperty("signature").GetString()!;

        if (DateTime.UtcNow > DateTime.Parse(expires))
            return false;

        string message = $"{product}|{usbSerial}|{expires}";
        byte[] signature = Convert.FromBase64String(sigBase64);

        return Crypto.VerifySignature(message, signature, publicKey);
    }
}
