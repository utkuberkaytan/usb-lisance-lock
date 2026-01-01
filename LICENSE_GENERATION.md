# License Generation Guide

## Quick Start

### Step 1: Generate RSA Keys

```bash
# Generate private key (KEEP THIS SECRET!)
openssl genrsa -out private_key.pem 2048

# Extract public key (safe to distribute)
openssl rsa -in private_key.pem -pubout -out public_key.pem
```

### Step 2: Get USB Serial Number

**Option A: Customer provides it**
Ask your customer to run this PowerShell command:
```powershell
Get-WmiObject Win32_DiskDrive | Where-Object {$_.InterfaceType -eq "USB"} | Select-Object SerialNumber
```

**Option B: Create a serial reader utility**
You can create a simple tool that reads USB serials for your customers.

### Step 3: Generate License

```bash
cd license-generator
dotnet build
dotnet run
```

Follow the prompts or use command-line arguments:

```bash
dotnet run -- private_key.pem "ProductName" "USB_SERIAL" "2025-12-31" license.json
```

### Step 4: Distribute

- Give customer the `license.json` file
- Customer places it on their USB drive (root directory)
- Your application will verify it automatically

## Command Line Usage

```bash
LicenseGenerator.exe <private_key> <product> <usb_serial> <expiration_date> <output_file>
```

**Parameters:**
- `private_key`: Path to your private key PEM file
- `product`: Product name (e.g., "MySoftware v2.0")
- `usb_serial`: USB device serial number
- `expiration_date`: Format `yyyy-MM-dd` or `yyyy-MM-dd HH:mm:ss`
- `output_file`: Where to save the license.json file

**Examples:**

```bash
# License expiring December 31, 2025
dotnet run -- private_key.pem "MyApp" "ABC123" "2025-12-31" license.json

# License expiring at specific time
dotnet run -- private_key.pem "MyApp" "ABC123" "2025-12-31 23:59:59" license.json

# Interactive mode (no arguments)
dotnet run
```

## License File Structure

The generated `license.json` will look like:

```json
{
  "product": "MyProduct",
  "usbSerial": "ABC123456789",
  "expires": "2025-12-31T23:59:59Z",
  "signature": "aVeryLongBase64EncodedSignatureString..."
}
```

**Security Notes:**
- The signature is a cryptographic signature of: `{product}|{usbSerial}|{expires}`
- Anyone can verify the signature with the public key, but only you can create valid signatures with your private key
- Modifying any field will invalidate the signature

## Batch License Generation

You can create multiple licenses at once using a script:

**PowerShell example:**
```powershell
$customers = @(
    @{Serial="ABC123"; Expires="2025-12-31"},
    @{Serial="DEF456"; Expires="2025-12-31"}
)

foreach ($customer in $customers) {
    dotnet run -- private_key.pem "MyProduct" $customer.Serial $customer.Expires "license_$($customer.Serial).json"
}
```

## Security Best Practices

1. **Protect Your Private Key**
   - Never commit `private_key.pem` to version control
   - Store in encrypted storage or HSM
   - Use strong file permissions (Windows: only you can read)
   - Consider using a hardware security module (HSM) for production

2. **Key Rotation**
   - If private key is compromised, generate a new pair
   - Update `public_key.pem` in your application
   - Re-issue all licenses with new key

3. **Backup**
   - Backup your private key securely
   - If lost, you cannot generate new licenses
   - Consider key escrow for business continuity

4. **Testing**
   - Test licenses with your application before distributing
   - Verify expired licenses are rejected
   - Test with wrong USB serial numbers

## Troubleshooting

**"Private key file not found"**
- Check the path to `private_key.pem`
- Use absolute paths if relative paths fail

**"Invalid date format"**
- Use format: `yyyy-MM-dd` or `yyyy-MM-dd HH:mm:ss`
- Dates are in UTC when signed

**License doesn't verify**
- Ensure public key matches the private key used to sign
- Check USB serial number matches exactly (case-sensitive in some cases)
- Verify expiration date hasn't passed

