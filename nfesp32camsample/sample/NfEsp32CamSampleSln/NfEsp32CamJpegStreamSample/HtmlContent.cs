using System.Text;

namespace NfEsp32CamJpegStreamSample
{
    internal static class HtmlContent
    {
        public const string ViewerPage = 
            "<!DOCTYPE html>" +
            "<html>" +
            "<head>" +
            "<meta name=\"viewport\" content=\"width=device-width\">" +
            "<title>ESP32-CAM Stream</title>" +
            "<style>" +
            "body{margin:0;background:#000;display:flex;flex-direction:column;align-items:center;justify-content:center;height:100vh;font-family:Arial,sans-serif}" +
            "h1{color:#fff;margin:10px;font-size:1.5em}" +
            "img{max-width:95%;max-height:85vh;border:2px solid #333;box-shadow:0 4px 8px rgba(0,0,0,0.5)}" +
            ".info{color:#888;margin-top:10px;font-size:0.9em}" +
            "</style>" +
            "</head>" +
            "<body>" +
            "<h1>ESP32-CAM Live Stream</h1>" +
            "<img src=\"/stream\" alt=\"Camera Stream\">" +
            "<div class=\"info\">Hardware JPEG encoding by OV2640 sensor</div>" +
            "</body>" +
            "</html>";

        public static byte[] GetViewerPageResponse()
        {
            string response = 
                "HTTP/1.1 200 OK\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-Length: " + ViewerPage.Length + "\r\n" +
                "Connection: close\r\n" +
                "\r\n" +
                ViewerPage;

            return Encoding.UTF8.GetBytes(response);
        }
    }
}
