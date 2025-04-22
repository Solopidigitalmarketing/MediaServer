using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace MediaServer
{
    public class Server
    {
        private TcpListener server;
        private string[] fileArray;
        private string path;
        private bool isRunning;

        // Constructor with mediaPath validation
        public Server(string address, int port, string mediaPath)
        {
            server = new TcpListener(IPAddress.Parse(address), port);
            path = mediaPath ?? throw new ArgumentNullException(nameof(mediaPath));

            // Use verbatim string or escape the backslashes
            fileArray = Directory.GetFiles(@"C:\Users\solop\Documents\MP3");
            isRunning = false;
        }

        // Start the server
        public void Start()
        {
            server.Start();
            isRunning = true;
            Console.WriteLine("Server started...");

            while (isRunning)
            {
                if (!server.Pending())
                {
                    Thread.Sleep(100); // prevent tight loop
                    continue;
                }

                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        // Stop the server
        public void Stop()
        {
            isRunning = false;
            server.Stop();
        }

        // Handle incoming client requests
        private void HandleClient(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            try
            {
                string? requestLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(requestLine)) return;

                Console.WriteLine("Request: " + requestLine);

                string[] tokens = requestLine.Split(' ');
                string method = tokens[0];
                string requestPath = Uri.UnescapeDataString(tokens[1].TrimStart('/'));
                string filePath = Path.Combine(path, requestPath);

                if (method == "HEAD")
                {
                    HandleHead(writer, filePath);
                }
                else if (method == "GET")
                {
                    if (Directory.Exists(filePath))
                    {
                        string indexPath = Path.Combine(filePath, "index.html");
                        if (File.Exists(indexPath))
                        {
                            ServeFile(writer, stream, indexPath, method, requestLine);
                        }
                        else
                        {
                            writer.WriteLine("HTTP/1.1 404 Not Found\r\n\r\n");
                        }
                    }
                    else
                    {
                        ServeFile(writer, stream, filePath, method, requestLine);
                    }
                }
                else
                {
                    writer.WriteLine("HTTP/1.1 405 Method Not Allowed\r\n\r\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling client: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        // Handle HEAD request (file metadata)
        private void HandleHead(StreamWriter writer, string filePath)
        {
            if (File.Exists(filePath))
            {
                FileInfo fi = new FileInfo(filePath);
                string mime = GetMimeType(fi.Extension);

                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: " + mime);
                writer.WriteLine("Content-Length: " + fi.Length);
                writer.WriteLine("Connection: close\r\n");
            }
            else
            {
                writer.WriteLine("HTTP/1.1 404 Not Found\r\n\r\n");
            }
        }

        // Serve file for GET requests
        private void ServeFile(StreamWriter writer, NetworkStream stream, string filePath, string method, string requestLine)
        {
            if (!File.Exists(filePath))
            {
                writer.WriteLine("HTTP/1.1 404 Not Found\r\n\r\n");
                return;
            }

            FileInfo fi = new FileInfo(filePath);
            string mime = GetMimeType(fi.Extension);
            long totalLength = fi.Length;

            bool isPartial = false;
            long start = 0;
            long end = totalLength - 1;

            string rangeHeader = null;
            while (true)
            {
                string line = new StreamReader(stream).ReadLine();
                if (string.IsNullOrWhiteSpace(line)) break;

                if (line.StartsWith("Range:"))
                {
                    rangeHeader = line;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(rangeHeader))
            {
                isPartial = true;
                string[] parts = rangeHeader.Split('=');
                string[] range = parts[1].Split('-');

                if (!string.IsNullOrEmpty(range[0])) start = long.Parse(range[0]);
                if (!string.IsNullOrEmpty(range[1])) end = long.Parse(range[1]);
            }

            long contentLength = end - start + 1;

            if (isPartial)
            {
                writer.WriteLine("HTTP/1.1 206 Partial Content");
                writer.WriteLine($"Content-Range: bytes {start}-{end}/{totalLength}");
            }
            else
            {
                writer.WriteLine("HTTP/1.1 200 OK");
            }

            writer.WriteLine("Content-Type: " + mime);
            writer.WriteLine("Content-Length: " + contentLength);
            writer.WriteLine("Accept-Ranges: bytes");
            writer.WriteLine("Connection: close\r\n");

            using FileStream fs = File.OpenRead(filePath);
            fs.Seek(start, SeekOrigin.Begin);

            byte[] buffer = new byte[8192];
            int bytesRead;
            long remaining = contentLength;

            while (remaining > 0 && (bytesRead = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining))) > 0)
            {
                stream.Write(buffer, 0, bytesRead);
                remaining -= bytesRead;
            }
        }

        // Get MIME type for a file extension
        private string GetMimeType(string extension) => extension.ToLower() switch
        {
            ".html" => "text/html",
            ".htm" => "text/html",
            ".txt" => "text/plain",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".mp3" => "audio/mpeg",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream",
        };
    }
}
