# USB License Lock System

A secure USB-based license verification system for .NET applications using RSA asymmetric cryptography.

## Overview

This system allows you to protect your software with USB dongle licensing. Each license is cryptographically signed and tied to a specific USB device's serial number.

## How It Works

### Security Model

The system uses **RSA asymmetric cryptography**:

- **Private Key** (kept secret): Used to sign licenses. Only you (the license issuer) have this.
- **Public Key** (`public_key.pem`): Bundled with your application. Used to verify license signatures.

### License Flow

1. **You generate a key pair** (one-time setup)
2. **You create licenses** for customers using the private key
3. **Customer receives**:
   - Your application (with embedded `public_key.pem`)
   - A `license.json` file (placed on their licensed USB drive)
4. **At runtime**: Application verifies the USB's serial number matches the license and validates the cryptographic signature

## Project Structure

```
usb-lisance-lock/
├── usb-lisance-lock/          # Main application (license verifier)
│   ├── Program.cs             # USB monitoring and license verification
│   ├── Crypto.cs              # Cryptographic operations
│   ├── LicenseVerifier.cs     # License validation logic
│   └── public_key.pem         # Public key (safe to distribute)
│
└── license-generator/          # License generation tool
    └── Program.cs             # Command-line license generator
```

## Getting Started

### 1. Generate RSA Key Pair

**First, generate your public/private key pair:**

```bash
# Generate private key (2048-bit RSA)
openssl genrsa -out private_key.pem 2048

# Extract public key
openssl rsa -in private_key.pem -pubout -out public_key.pem
```

⚠️ **IMPORTANT**: 
- Keep `private_key.pem` **SECRET** - never commit it to version control
- Only distribute `public_key.pem` with your application
- Store the private key securely (encrypted storage, HSM, etc.)

### 2. Get USB Serial Number

Before creating a license, you need the customer's USB serial number. They can provide this, or you can create a utility to read it.

**Windows PowerShell:**
```powershell
Get-WmiObject Win32_DiskDrive | Where-Object {$_.InterfaceType -eq "USB"} | Select-Object SerialNumber
```

### 3. Generate License

Use the license generator tool:

```bash
cd license-generator
dotnet build
dotnet run
```

**Or with arguments:**
```bash
dotnet run -- private_key.pem "MyProduct" "USB_SERIAL_NUMBER" "2025-12-31" license.json
```

**Interactive mode:**
```bash
dotnet run
# Then follow the prompts
```

### 4. Deploy Application

1. Copy `public_key.pem` to your application's output directory
2. Distribute the application to customers
3. Customer places `license.json` on their licensed USB drive

## License File Format

The `license.json` file contains:

```json
{
  "product": "MyProduct",
  "usbSerial": "1234567890ABCDEF",
  "expires": "2025-12-31T23:59:59Z",
  "signature": "base64_encoded_signature_here"
}
```

- **product**: Your product name
- **usbSerial**: The USB device's physical serial number
- **expires**: Expiration date (ISO 8601 format)
- **signature**: RSA signature of `{product}|{usbSerial}|{expires}`

## Usage in Your Application

The main application (`usb-lisance-lock`) demonstrates:

1. **USB Insertion Detection**: Monitors for USB drives being plugged in
2. **Serial Number Reading**: Extracts the physical USB serial number
3. **License Verification**: 
   - Checks for `license.json` on the USB
   - Validates cryptographic signature
   - Verifies USB serial matches license
   - Checks expiration date

## Security Considerations

✅ **What this system provides:**
- Cryptographic signature verification (prevents tampering)
- Hardware binding (license tied to USB serial)
- Expiration date checking
- Only public key in application (private key stays secret)

⚠️ **Best Practices:**
- Use strong key sizes (2048+ bits for RSA)
- Store private key securely (never in source control)
- Regularly rotate keys if compromised
- Consider additional protections (obfuscation, anti-debugging) for production

## Building

```bash
# Build main application
cd usb-lisance-lock
dotnet build

# Build license generator
cd ../license-generator
dotnet build
```

## Requirements

- .NET 8.0 or later
- Windows (uses WMI for USB detection)
- System.Management NuGet package

## Example Workflow

1. **Developer** generates key pair
2. **Customer** provides USB serial number: `ABC123456789`
3. **Developer** runs license generator:
   ```bash
   dotnet run -- private_key.pem "MyApp" "ABC123456789" "2025-12-31" license.json
   ```
4. **Developer** sends customer:
   - Application (with `public_key.pem`)
   - `license.json` file
5. **Customer** places `license.json` on their USB drive
6. **Application** verifies license when USB is inserted

## Troubleshooting

**USB serial not detected:**
- Some USB drives don't report serial numbers
- Try using a different USB drive
- Check device manager for USB properties

**License verification fails:**
- Ensure `license.json` is in the root of the USB drive
- Verify USB serial number matches exactly
- Check expiration date hasn't passed
- Ensure `public_key.pem` is in the application directory

## License

This project is provided as-is for educational and commercial use.

## Contributing

Contributions welcome! Please ensure:
- Code follows existing style
- Security implications are considered
- Documentation is updated

