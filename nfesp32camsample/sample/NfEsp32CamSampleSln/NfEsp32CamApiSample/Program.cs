using nanoFramework.Hardware.Esp32.Camera;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Device.Wifi;
using System.Net.NetworkInformation;

namespace NfEsp32CamApiSample
{
    public class Program
    {
        // Configure your WiFi credentials here
        private const string WIFI_SSID = "YOUR_WIFI_SSID";
        private const string WIFI_PASSWORD = "YOUR_WIFI_PASSWORD";

        // Configure your API endpoint here
        private const string API_URL = "http://192.168.1.85:5000/api/Image/upload";
        
        // Capture interval in milliseconds
        private const int CAPTURE_INTERVAL = 5000; // 5 seconds

        public static void Main()
        {
            Debug.WriteLine("ESP32-CAM API Sample Starting...");

            // Connect to WiFi
            if (!ConnectToWiFi())
            {
                Debug.WriteLine("Failed to connect to WiFi. Exiting...");
                return;
            }

            // Initialize camera
            var camera = new Camera();
            var config = CameraConfig.CreateDefault();

            if (!camera.Initialize(config))
            {
                Debug.WriteLine("Failed to initialize camera!");
                return;
            }

            Debug.WriteLine("Camera initialized successfully!");
            
            // Give camera time to warm up and adjust exposure
            Debug.WriteLine("Warming up camera (2 seconds)...");
            Thread.Sleep(2000);
            
            // Capture and discard first few frames (they're often dark/underexposed)
            Debug.WriteLine("Discarding first 3 frames...");
            for (int i = 0; i < 3; i++)
            {
                Debug.WriteLine($"  Warm-up frame {i + 1}/3");
                byte[] warmup = camera.CaptureImage();
                if (warmup != null)
                {
                    Debug.WriteLine($"  Discarded {warmup.Length} bytes");
                }
                Thread.Sleep(500);
            }
            
            Debug.WriteLine("Camera ready for capture!");

            try
            {
                int imageCount = 0;

                while (true)
                {
                    imageCount++;
                    Debug.WriteLine($"\n--- Capturing image {imageCount} ---");

                    try
                    {
                        // Capture image from camera
                        byte[] imageData = camera.CaptureImage();

                        if (imageData != null && imageData.Length > 0)
                        {
                            Debug.WriteLine($"Image captured: {imageData.Length} bytes");

                            // Send image to Web API
                            bool success = SendImageToApi(imageData);

                            if (success)
                            {
                                Debug.WriteLine("Image uploaded successfully!");
                            }
                            else
                            {
                                Debug.WriteLine("Failed to upload image.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Failed to capture image - no data received");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error capturing/sending image: {ex.Message}");
                    }

                    // Wait before next capture
                    Thread.Sleep(CAPTURE_INTERVAL);
                }
            }
            finally
            {
                camera.Dispose();
                Debug.WriteLine("Camera disposed");
            }
        }

        private static bool ConnectToWiFi()
        {
            Debug.WriteLine("Connecting to WiFi...");
            Debug.WriteLine($"SSID: {WIFI_SSID}");
            
            try
            {
                // Get the wireless network interface
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                if (interfaces.Length == 0)
                {
                    Debug.WriteLine("No network interfaces found!");
                    return false;
                }

                var ni = interfaces[0];
                Debug.WriteLine("Network interface found");
                
                // Check if already connected
                if (ni.IPv4Address != "0.0.0.0" && ni.IPv4Address != null && ni.IPv4Address != "")
                {
                    Debug.WriteLine($"Already connected! IP: {ni.IPv4Address}");
                    return true;
                }

                // Configure WiFi credentials
                Wireless80211Configuration[] configs = Wireless80211Configuration.GetAllWireless80211Configurations();
                
                if (configs.Length == 0)
                {
                    Debug.WriteLine("No WiFi configuration found!");
                    return false;
                }

                Wireless80211Configuration wifiConfig = configs[0];
                
                Debug.WriteLine($"Configuring WiFi - SSID: {WIFI_SSID}");
                wifiConfig.Ssid = WIFI_SSID;
                wifiConfig.Password = WIFI_PASSWORD;
                
                // Try different authentication types
                wifiConfig.Authentication = AuthenticationType.WPA2;
                wifiConfig.Encryption = EncryptionType.WPA2;
                
                wifiConfig.Options = Wireless80211Configuration.ConfigurationOptions.AutoConnect | 
                                     Wireless80211Configuration.ConfigurationOptions.Enable;
                
                wifiConfig.SaveConfiguration();
                Debug.WriteLine("WiFi configuration saved");

                // Small delay for config to apply
                Thread.Sleep(1000);

                // Enable DHCP and DNS
                Debug.WriteLine("Enabling DHCP and DNS...");
                ni.EnableDhcp();
                ni.EnableAutomaticDns();

                Debug.WriteLine("Waiting for network connection...");
                
                // Wait for DHCP to assign an IP address
                int retries = 60; // 60 * 1000ms = 60 seconds
                while (retries > 0)
                {
                    Thread.Sleep(1000); // Check every second
                    
                    ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                    
                    string currentIp = ni.IPv4Address;
                    Debug.WriteLine($"[{60 - retries}s] IP: {currentIp}");
                    
                    if (currentIp != "0.0.0.0" && currentIp != null && currentIp != "")
                    {
                        Debug.WriteLine($"✓ Connected to WiFi: {WIFI_SSID}");
                        Debug.WriteLine($"✓ IP Address: {ni.IPv4Address}");
                        Debug.WriteLine($"✓ Subnet Mask: {ni.IPv4SubnetMask}");
                        Debug.WriteLine($"✓ Gateway: {ni.IPv4GatewayAddress}");
                        return true;
                    }
                    
                    retries--;
                }

                Debug.WriteLine("Failed to obtain IP address after 60 seconds");
                Debug.WriteLine("\nTroubleshooting tips:");
                Debug.WriteLine("- Verify SSID and password are correct (case-sensitive!)");
                Debug.WriteLine("- Ensure WiFi network is 2.4GHz (ESP32 doesn't support 5GHz)");
                Debug.WriteLine("- Check if router allows new devices (MAC filtering?)");
                Debug.WriteLine("- Try moving ESP32-CAM closer to router");
                Debug.WriteLine("- Check router logs for connection attempts");
                Debug.WriteLine($"- Try connecting another device to '{WIFI_SSID}' to verify it works");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WiFi connection error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static bool SendImageToApi(byte[] imageData)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    Debug.WriteLine($"Sending {imageData.Length} bytes to {API_URL}...");

                    // Create HTTP request
                    using (var request = new HttpRequestMessage(HttpMethod.Post, API_URL))
                    {
                        // Set content
                        request.Content = new ByteArrayContent(imageData);
                        
                        // Add Content-Type header
                        request.Headers.Add("Content-Type", "image/jpeg");

                        // Send POST request
                        using (HttpResponseMessage response = httpClient.Send(request))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string responseBody = response.Content.ReadAsString();
                                Debug.WriteLine($"Response: {responseBody}");
                                return true;
                            }
                            else
                            {
                                Debug.WriteLine($"HTTP Error: {response.StatusCode}");
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending image: {ex.Message}");
                return false;
            }
        }
    }
}
