# NfEsp32CamApiSample - ESP32-CAM Image Upload Client

This is a nanoFramework application for ESP32-CAM that captures images and uploads them to a .NET Web API.

## Features

- WiFi connectivity
- Camera initialization and image capture
- HTTP client for uploading images
- Configurable capture interval
- Automatic retry and error handling

## Prerequisites

- ESP32-CAM board
- nanoFramework firmware installed on ESP32-CAM
- Visual Studio with nanoFramework extension
- CamApiSample Web API running on your network

## Configuration

Before deploying, update the following constants in `Program.cs`:

```csharp
private const string WIFI_SSID = "YOUR_WIFI_SSID";
private const string WIFI_PASSWORD = "YOUR_WIFI_PASSWORD";
private const string API_URL = "http://YOUR_PC_IP:5000/api/Image/upload";
private const int CAPTURE_INTERVAL = 5000; // Capture every 5 seconds
```

### Getting Your PC's IP Address

**Windows:**
```powershell
ipconfig
```
Look for "IPv4 Address" under your active network adapter.

**Linux/Mac:**
```bash
ifconfig
# or
ip addr show
```

## Required NuGet Packages

The project requires these nanoFramework packages:
- `nanoFramework.CoreLibrary`
- `nanoFramework.Hardware.Esp32.Camera`
- `nanoFramework.System.Net.Http`
- `nanoFramework.Runtime.Events`
- `nanoFramework.System.Device.Wifi`

These are already configured in `packages.config`.

## How It Works

1. **WiFi Connection**: Connects to your WiFi network on startup
2. **Camera Initialization**: Configures the ESP32-CAM with default settings
3. **Capture Loop**: 
   - Captures a JPEG image
   - Sends the image to the Web API via HTTP POST
   - Waits for the configured interval
   - Repeats

## Deployment

1. Open the solution in Visual Studio
2. Connect your ESP32-CAM via USB
3. Select your device in the Device Explorer
4. Build and deploy the project (F5)
5. Monitor the output in the Debug window

## Debug Output

The application provides detailed debug output:

```
ESP32-CAM API Sample Starting...
Connecting to WiFi...
Connected to WiFi: YourNetwork
IP Address: 192.168.1.50
Camera initialized successfully!

--- Capturing image 1 ---
Image captured: 45678 bytes
Sending 45678 bytes to http://192.168.1.100:5000/api/Image/upload...
Response: {"message":"Image uploaded successfully","filename":"img_20251027_123045.jpg","size":45678}
Image uploaded successfully!
```

## Troubleshooting

### WiFi Connection Issues
- Verify SSID and password are correct
- Ensure the network is 2.4GHz (ESP32 doesn't support 5GHz)
- Check that the ESP32-CAM is within range

### Camera Initialization Failed
- Verify the ESP32-CAM hardware is functioning
- Try resetting the device
- Check that the camera firmware is compatible

### Upload Failures
- Ensure the Web API is running
- Verify the IP address and port are correct
- Check firewall settings on your PC
- Ensure both devices are on the same network

### Image Quality Issues
To adjust image quality, modify the camera configuration:

```csharp
var config = CameraConfig.CreateDefault();
config.PixelFormat = PixelFormat.JPEG;
config.FrameSize = FrameSize.SVGA; // Adjust as needed
config.JpegQuality = 10; // Lower = better quality (range: 0-63)
```

## Camera Configuration Options

Available frame sizes (from smallest to largest):
- `QQVGA` - 160x120
- `QCIF` - 176x144
- `HQVGA` - 240x176
- `QVGA` - 320x240
- `CIF` - 400x296
- `VGA` - 640x480
- `SVGA` - 800x600 (default)
- `XGA` - 1024x768
- `SXGA` - 1280x1024
- `UXGA` - 1600x1200

## Performance Tips

- Lower resolution = faster capture and upload
- Higher JPEG quality = larger file sizes
- Adjust `CAPTURE_INTERVAL` based on your needs
- Monitor memory usage for long-running deployments

## Related Projects

- **CamApiSample**: The .NET 9 Web API that receives the images
- **NfEsp32CamSample**: A simpler example that saves images to SD card

## License

This is a sample project for demonstration purposes.
