using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaServer
{
    public class Server
    {
        private string ip;
        private int port;
        private string mediaDir;
        private TcpListener listener;

        public Server(string ip, int port, string mediaDir)
        {
            this.ip = ip;
            this.port = port;
            this.mediaDir = mediaDir;
        }

        public void Start()
        {
            listener = new TcpListener(IPAddress.Parse(ip), port);
            listener.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                // Accept client connections
                var client = listener.AcceptTcpClient();
                var networkStream = client.GetStream();
                var reader = new StreamReader(networkStream);
                var writer = new StreamWriter(networkStream);

                try
                {
                    string request = reader.ReadLine();
                    if (request != null)
                    {
                        Console.WriteLine("Request: " + request);
                        string[] requestParts = request.Split(' ');

                        if (requestParts.Length >= 2 && requestParts[0] == "GET")
                        {
                            string requestedFile = requestParts[1];
                            Console.WriteLine($"Requested File: {requestedFile}");

                            // Handle directory listing
                            if (requestedFile == "/")
                            {
                                // List all available media files
                                var availableMedia = new AvailableMedia(mediaDir);
                                var files = availableMedia.GetAvailableFiles();
                                writer.WriteLine("HTTP/1.1 200 OK");
                                writer.WriteLine("Content-Type: text/html; charset=UTF-8");
                                writer.WriteLine();
                                writer.WriteLine("<html><body><h1>Media Files</h1><ul>");

                                foreach (var file in files)
                                {
                                    string fileName = Path.GetFileName(file);
                                    writer.WriteLine($"<li><a href=\"/{fileName}\">{fileName}</a></li>");
                                }

                                writer.WriteLine("</ul></body></html>");
                                writer.Flush();
                            }
                            else
                            {
                                // Serve the requested file
                                string filePath = Path.Combine(mediaDir, requestedFile.TrimStart('/').Replace('/', '\\'));
                                if (File.Exists(filePath))
                                {
                                    var fileSender = new FileSenderHelper(filePath, client.Client);
                                    fileSender.SendFile(networkStream);
                                }
                                else
                                {
                                    writer.WriteLine("HTTP/1.1 404 Not Found");
                                    writer.WriteLine();
                                    writer.Flush();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling client: {ex.Message}");
                }
                finally
                {
                    client.Close();
                }
            }
        }

        public void Stop()
        {
            listener.Stop();
        }
    }
}
