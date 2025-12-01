using nanoFramework.Hardware.Esp32.Camera;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Device.Wifi;
using System.Net.NetworkInformation;

namespace NfEsp32CamJpegStreamSample
{
    public class Program
    {
        // Configure your WiFi credentials here
        private const string WIFI_SSID = "YOUR_WIFI_SSID";
        private const string WIFI_PASSWORD = "YOUR_WIFI_PASSWORD";

        // HTTP server port for MJPEG stream
        private const int HTTP_PORT = 80;

        private const string BOUNDARY = "bf";

        private static Camera _camera;

        public static void Main()
        {
            Debug.WriteLine("=================================");
            Debug.WriteLine("ESP32-CAM High-Speed MJPEG Streamer");
            Debug.WriteLine("=================================\n");

            if (!ConnectToWiFi())
            {
                Debug.WriteLine("Failed to connect to WiFi. Exiting...");
                return;
            }

            if (!InitializeCamera())
            {
                Debug.WriteLine("Failed to initialize camera. Exiting...");
                return;
            }

            StartHttpServer();
        }

        private static bool ConnectToWiFi()
        {
            Debug.WriteLine("Connecting to WiFi...");
            Debug.WriteLine($"SSID: {WIFI_SSID}");

            var wifi = WifiAdapter.FindAllAdapters()[0];

            WifiConnectionResult result = wifi.Connect(WIFI_SSID, WifiReconnectionKind.Automatic, WIFI_PASSWORD);

            DateTime timeout = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < timeout)
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                bool connected = false;

                foreach (var ni in networkInterfaces)
                {
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.IPv4Address != null && ni.IPv4Address != "0.0.0.0")
                    {
                        connected = true;
                        break;
                    }
                }

                if (connected)
                {
                    networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (var ni in networkInterfaces)
                    {
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 && ni.IPv4Address != null)
                        {
                            Debug.WriteLine($"\n✓ WiFi Connected!");
                            Debug.WriteLine($"IP Address: {ni.IPv4Address}");
                            Debug.WriteLine($"Gateway: {ni.IPv4GatewayAddress}");
                            Debug.WriteLine($"Stream URL: http://{ni.IPv4Address}/stream");
                            Debug.WriteLine();
                            return true;
                        }
                    }
                }

                Debug.Write(".");
                Thread.Sleep(500);
            }

            Debug.WriteLine("\n✗ WiFi connection timeout");
            return false;
        }

        private static bool InitializeCamera()
        {
            Debug.WriteLine("Initializing camera for high-speed streaming...");

            _camera = new Camera();
            var config = CameraConfig.CreateDefault();

            if (!_camera.Initialize(config))
            {
                Debug.WriteLine("✗ Camera initialization failed!");
                return false;
            }

            Debug.WriteLine("✓ Camera initialized");

            Debug.WriteLine("Warming up camera (discard first 3 frames)...");
            for (int i = 0; i < 3; i++)
            {
                byte[] warmup = _camera.CaptureImage();
                if (warmup != null)
                {
                    Debug.WriteLine($"  Frame {i + 1}: {warmup.Length} bytes - discarded");
                }
                Thread.Sleep(100);
            }

            Debug.WriteLine("✓ Camera ready for streaming\n");
            return true;
        }

        private static void StartHttpServer()
        {
            Debug.WriteLine($"Starting HTTP server on port {HTTP_PORT}...");

            var listener = new TcpListener(IPAddress.Any, HTTP_PORT);
            listener.Start(5); // max connections

            Debug.WriteLine($"✓ HTTP server started");
            Debug.WriteLine("Waiting for client connections...\n");

            int clientCount = 0;

            while (true)
            {
                try
                {
                    TcpClient client = listener.AcceptTcpClient();
                    clientCount++;

                    Debug.WriteLine($"[Client {clientCount}] Connected from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");

                    int clientId = clientCount;
                    new Thread(() => HandleClient(client, clientId)).Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private static void HandleClient(TcpClient client, int clientId)
        {
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();

                byte[] buffer = new byte[512];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                int previewLength = request.Length < 50 ? request.Length : 50;
                Debug.WriteLine($"[Client {clientId}] Request: {request.Substring(0, previewLength)}...");

                if (request.IndexOf("GET /stream") >= 0)
                {
                    Debug.WriteLine($"[Client {clientId}] Starting MJPEG stream");
                    StreamMjpeg(stream, clientId);
                }
                else
                {
                    SendHtmlPage(stream, clientId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Client {clientId}] Error: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Debug.WriteLine($"[Client {clientId}] Disconnected");
            }
        }

        private static void StreamMjpeg(NetworkStream stream, int clientId)
        {
            string headers =
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: multipart/x-mixed-replace; boundary=" + BOUNDARY + "\r\n" +
                "Cache-Control: no-cache\r\n" +
                "Connection: close\r\n" +
                "\r\n";

            byte[] headerBytes = Encoding.UTF8.GetBytes(headers);
            stream.Write(headerBytes, 0, headerBytes.Length);
            stream.Flush();

            int frameCount = 0;
            DateTime startTime = DateTime.UtcNow;
            DateTime lastFpsReport = startTime;

            Debug.WriteLine($"[Client {clientId}] Streaming started");

            while (true)
            {
                try
                {
                    byte[] jpegData = _camera.CaptureImage();

                    if (jpegData == null || jpegData.Length == 0)
                    {
                        Debug.WriteLine($"[Client {clientId}] Empty frame, skipping");
                        Thread.Sleep(50);
                        continue;
                    }

                    // Send MJPEG frame boundary and headers
                    string frameHeader =
                        "--" + BOUNDARY + "\r\n" +
                        "Content-Type: image/jpeg\r\n" +
                        "Content-Length: " + jpegData.Length + "\r\n" +
                        "\r\n";

                    byte[] frameHeaderBytes = Encoding.UTF8.GetBytes(frameHeader);

                    stream.Write(frameHeaderBytes, 0, frameHeaderBytes.Length);

                    stream.Write(jpegData, 0, jpegData.Length);

                    byte[] footer = Encoding.UTF8.GetBytes("\r\n");
                    stream.Write(footer, 0, footer.Length);

                    stream.Flush();

                    frameCount++;

                    if ((DateTime.UtcNow - lastFpsReport).TotalSeconds >= 5)
                    {
                        double elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        double fps = frameCount / elapsed;
                        Debug.WriteLine($"[Client {clientId}] Frames: {frameCount}, FPS: {fps:F1}, Size: {jpegData.Length} bytes");
                        lastFpsReport = DateTime.UtcNow;
                    }
                }
                catch (SocketException)
                {
                    Debug.WriteLine($"[Client {clientId}] Client disconnected (socket error)");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Client {clientId}] Stream error: {ex.Message}");
                    break;
                }
            }

            double totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            double avgFps = frameCount / totalElapsed;
            Debug.WriteLine($"[Client {clientId}] Stream ended. Total frames: {frameCount}, Avg FPS: {avgFps:F1}");
        }

        private static void SendHtmlPage(NetworkStream stream, int clientId)
        {
            Debug.WriteLine($"[Client {clientId}] Sending HTML viewer page");

            byte[] response = HtmlContent.GetViewerPageResponse();
            stream.Write(response, 0, response.Length);
            stream.Flush();
        }
    }
}
