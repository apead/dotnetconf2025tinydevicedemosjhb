# CamApiSample - ESP32-CAM Image Upload API

This is a .NET 9 Web API solution that receives JPEG images from ESP32-CAM devices and stores them on the file system.

## Features

- **Image Upload Endpoint**: Accepts JPEG images via HTTP POST
- **File Storage**: Automatically saves images with unique timestamped filenames
- **Image Listing**: Endpoint to list all uploaded images
- **Configurable Storage**: Storage path can be configured in appsettings.json
- **Swagger UI**: Interactive API documentation and testing interface

## API Endpoints

### 1. Upload Image
- **URL**: `POST /api/Image/upload`
- **Content-Type**: `image/jpeg`
- **Body**: Raw JPEG image data
- **Response**: JSON with upload details

Example response:
```json
{
  "message": "Image uploaded successfully",
  "filename": "img_20251027_123045_a1b2c3d4.jpg",
  "size": 45678,
  "path": "F:\\work\\experiments\\espcamdeploy\\sample\\CamApiSample\\Images\\img_20251027_123045_a1b2c3d4.jpg"
}
```

### 2. List Images
- **URL**: `GET /api/Image/list`
- **Response**: JSON with list of all stored images

Example response:
```json
{
  "count": 5,
  "images": [
    {
      "filename": "img_20251027_123045_a1b2c3d4.jpg",
      "size": 45678,
      "created": "2025-10-27T12:30:45Z"
    }
  ]
}
```

## Configuration

Edit `appsettings.json` to configure the image storage path:

```json
{
  "ImageStoragePath": "Images"
}
```

The path can be absolute or relative to the application directory.

## Running the API

1. Navigate to the CamApiSample directory:
   ```powershell
   cd f:\work\experiments\espcamdeploy\sample\CamApiSample
   ```

2. Run the application:
   ```powershell
   dotnet run
   ```

3. The API will start on `http://localhost:5000`

4. **Access Swagger UI** at: `http://localhost:5000/`
   - Interactive API documentation
   - Test endpoints directly from your browser
   - View request/response schemas

5. Note your PC's IP address for the ESP32-CAM client configuration

## ESP32-CAM Client Configuration

The `NfEsp32CamApiSample` project is configured to capture images and send them to this API.

Before deploying to your ESP32-CAM, update these values in `Program.cs`:

```csharp
private const string WIFI_SSID = "YOUR_WIFI_SSID";
private const string WIFI_PASSWORD = "YOUR_WIFI_PASSWORD";
private const string API_URL = "http://YOUR_PC_IP:5000/api/Image/upload";
```

Replace:
- `YOUR_WIFI_SSID` with your WiFi network name
- `YOUR_WIFI_PASSWORD` with your WiFi password
- `YOUR_PC_IP` with your PC's IP address (e.g., "192.168.1.100")

## Testing

You can test the upload endpoint using curl:

```powershell
curl -X POST http://localhost:5000/api/Image/upload -H "Content-Type: image/jpeg" --data-binary "@test.jpg"
```

Or list all images:

```powershell
curl http://localhost:5000/api/Image/list
```

## Project Structure

```
CamApiSample/
├── Controllers/
│   └── ImageController.cs    # Main API controller
├── Images/                     # Default image storage directory
├── Program.cs                  # Application entry point
├── appsettings.json           # Configuration
└── README.md                  # This file
```

## Requirements

- .NET 9 SDK
- Windows, Linux, or macOS
- Network connectivity for ESP32-CAM clients
