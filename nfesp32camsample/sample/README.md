# ESP32-CAM Sample Projects

This repository contains three nanoFramework sample projects demonstrating different ways to work with the ESP32-CAM.

## 📦 Projects

### 1. NfEsp32CamSample - SD Card Storage
**Location:** `NfEsp32CamSampleSln/NfEsp32CamSample/`

Captures photos and saves them directly to an SD card.

**Features:**
- Saves JPEG images to SD card
- File naming with timestamps
- Local storage without network

**Usage:**
1. Insert formatted SD card (FAT32) into ESP32-CAM
2. Deploy the application
3. Images saved to `D:\images\` on SD card
4. Check debug output for saved file paths

---

### 2. NfEsp32CamApiSample - Upload to Web API
**Location:** `NfEsp32CamSampleSln/NfEsp32CamApiSample/`

Captures photos and uploads them to a .NET Web API via HTTP POST.

**Features:**
- WiFi connectivity
- HTTP client for uploading images
- Camera warm-up (discards first 3 frames for better quality)
- Automatic retry and error handling

**Usage:**
1. Configure WiFi credentials in `Program.cs`:
   ```csharp
   private const string WIFI_SSID = "YourWiFiSSID";
   private const string WIFI_PASSWORD = "YourPassword";
   ```

2. Start the Web API (in `CamApiSample/`):
   ```bash
   cd CamApiSample
   dotnet run
   ```

3. Update API URL in ESP32 code if needed (default: port 5000)

4. Deploy to ESP32-CAM and check debug output for upload status

**Companion Web API:**
- **Location:** `CamApiSample/`
- **Endpoint:** `POST /api/Image/upload`
- **Storage:** Images saved to configured path
- **Swagger UI:** Available at `http://[API-IP]:5000/`

---

### 3. NfEsp32CamJpegStream - High-Speed MJPEG Streaming
**Location:** `NfEsp32CamSampleSln/NfEsp32CamJpegStream/`

Real-time video streaming using hardware JPEG encoding from the OV2640 sensor.

**Features:**
- ⚡ High-speed streaming (15-30 FPS)
- 🎥 MJPEG protocol (Motion JPEG)
- 🌐 Built-in web viewer
- 📺 Compatible with VLC, browsers, ffmpeg, OpenCV
- 🔧 Hardware JPEG encoding (zero CPU overhead)
- 👥 Multi-client support

**Usage:**
1. Configure WiFi credentials in `Program.cs`:
   ```csharp
   private const string WIFI_SSID = "YourWiFiSSID";
   private const string WIFI_PASSWORD = "YourPassword";
   ```

2. Deploy to ESP32-CAM

3. Check debug output for IP address:
   ```
   IP Address: [IP]
   Stream URL: http://[IP]/stream
   ```

4. Access the stream:
   - **Web Browser:** `http://[IP]/` (HTML viewer)
   - **Direct Stream:** `http://[IP]/stream` (for VLC, apps)

**View Options:**
- **Browser:** Beautiful HTML viewer with styled layout
- **VLC:** Media → Open Network Stream → Enter stream URL
- **ffmpeg:** `ffmpeg -i http://IP/stream -f image2 snapshot.jpg`
- **Python/OpenCV:** `cv2.VideoCapture('http://IP/stream')`

**Performance:**
- Frame Rate: 15-30 FPS
- Latency: < 200ms
- Hardware JPEG encoding by OV2640 sensor (no ESP32 CPU usage)

---

## 🎯 Which Project Should I Use?

| Use Case | Project | Why? |
|----------|---------|------|
| Offline photo capture | **NfEsp32CamSample** | No network needed, local SD storage |
| Remote photo storage | **NfEsp32CamApiSample** | Upload to cloud/server, centralized storage |
| Live video streaming | **NfEsp32CamJpegStream** | Real-time viewing, security cameras, monitoring |
| Testing camera | Any | Start with NfEsp32CamSample (simplest) |

---

**Created:** 2025  
**Framework:** nanoFramework (.NET for embedded devices)  
**Hardware:** ESP32-CAM with OV2640 sensor
