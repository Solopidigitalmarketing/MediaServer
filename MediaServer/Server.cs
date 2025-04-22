using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Server
{
    private readonly int port;

    public Server(int port)
    {
        this.port = port;
    }

    public void Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine($"Server started on port {port}");

        while (true)
        {
            var client = listener.AcceptTcpClient();

            Console.WriteLine($"[{DateTime.Now}] Connection from {client.Client.RemoteEndPoint}");

            Task.Run(() => HandleClient(client));
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream) { AutoFlush = true })
            {
                string requestLine = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(requestLine)) return;

                Console.WriteLine($"Request: {requestLine}");

                string[] tokens = requestLine.Split(' ');
                if (tokens.Length != 3) return;

                string method = tokens[0];
                string url = Uri.UnescapeDataString(tokens[1]);
                string protocol = tokens[2];

                if (method != "GET" && method != "HEAD")
                {
                    writer.WriteLine("HTTP/1.1 405 Method Not Allowed");
                    writer.WriteLine("Allow: GET, HEAD");
                    writer.WriteLine();
                    return;
                }

                string filePath = "wwwroot" + url.Replace('/', Path.DirectorySeparatorChar);

                if (Directory.Exists(filePath))
                    filePath = Path.Combine(filePath, "index.html");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    writer.WriteLine("HTTP/1.1 404 Not Found");
                    writer.WriteLine("Content-Type: text/plain");
                    writer.WriteLine();
                    writer.WriteLine("404 Not Found");
                    return;
                }

                FileInfo file = new FileInfo(filePath);
                string mimeType = GetMimeType(filePath);

                long start = 0;
                long end = file.Length - 1;
                bool isPartial = false;

                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    if (line.StartsWith("Range:"))
                    {
                        Console.WriteLine($"Range requested: {line}");
                        var range = line.Substring("Range: bytes=".Length).Split('-');
                        start = long.Parse(range[0]);
                        if (!string.IsNullOrEmpty(range[1]))
                            end = long.Parse(range[1]);

                        isPartial = true;
                    }
                }

                if (start >= file.Length || start > end)
                {
                    writer.WriteLine("HTTP/1.1 416 Range Not Satisfiable");
                    writer.WriteLine($"Content-Range: bytes */{file.Length}");
                    writer.WriteLine();
                    return;
                }

                if (isPartial)
                {
                    writer.WriteLine("HTTP/1.1 206 Partial Content");
                    writer.WriteLine($"Content-Range: bytes {start}-{end}/{file.Length}");
                    writer.WriteLine($"Content-Length: {end - start + 1}");
                }
                else
                {
                    writer.WriteLine("HTTP/1.1 200 OK");
                    writer.WriteLine($"Content-Length: {file.Length}");
                }

                writer.WriteLine($"Content-Type: {mimeType}");
                writer.WriteLine("Accept-Ranges: bytes");
                writer.WriteLine();
                writer.Flush();

                if (method == "GET")
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        fs.Seek(start, SeekOrigin.Begin);
                        byte[] buffer = new byte[8192];
                        long remaining = end - start + 1;
                        int bytesRead;

                        while (remaining > 0 && (bytesRead = fs.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining))) > 0)
                        {
                            stream.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }
                        stream.Flush();
                    }
                }
            }
        }
        catch (IOException ioEx)
        {
            Console.WriteLine($"IO Exception: {ioEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }

    private string GetMimeType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        switch (ext)
        {
            case ".html": return "text/html";
            case ".htm": return "text/html";
            case ".css": return "text/css";
            case ".js": return "application/javascript";
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".mp4": return "video/mp4";
            case ".mp3": return "audio/mpeg";
            case ".txt": return "text/plain";
            default: return "application/octet-stream";
        }
    }
}
