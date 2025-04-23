using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MediaServer
{
    public class FileSenderHelper
    {
        private string filePath;
        private Socket socket;

        public FileSenderHelper(string filePath, Socket socket)
        {
            this.filePath = filePath;
            this.socket = socket;
        }

        public void SendFile(NetworkStream stream)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096]; // Read in 4KB chunks
                    int bytesRead;

                    // Get the file extension and set the correct content type
                    string contentType = GetContentType(filePath);
                    stream.Write(Encoding.UTF8.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: {contentType}\r\n\r\n"));

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending file: {ex.Message}");
            }
        }

        // Get the content type based on the file extension
        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".mp3": return "audio/mpeg";
                case ".mp4": return "video/mp4";
                case ".jpg": return "image/jpeg";
                case ".png": return "image/png";
                case ".gif": return "image/gif";
                default: return "application/octet-stream"; // Default content type
            }
        }
    }
}
