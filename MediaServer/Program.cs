using System;
using System.IO;
using System.Threading;
using System.Text.Json.Nodes;

namespace MediaServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("eL33T Media Server");

            // Load properties from JSON file
            string fileName = "properties.json";
            string rawProperties = File.ReadAllText(fileName);

            // Parse the JSON into a JsonNode
            JsonNode properties = JsonNode.Parse(rawProperties)!;

            // Retrieve values from the properties JSON
            string ip = properties["ip"].GetValue<string>();
            int port = properties["port"].GetValue<int>();
            string mediaDir = properties["mediaDir"].GetValue<string>();

            // Initialize and start the server on a separate thread
            Server server = new Server(ip, port, mediaDir);
            Thread serverThread = new Thread(server.Start);

            Console.WriteLine($"Access via link: http://{ip}:{port}");

            // Start the server thread
            serverThread.Start();

            Console.WriteLine("Press Enter to stop the server...");
            Console.ReadLine();

            // Stop the server gracefully when Enter is pressed
            server.Stop();
        }
    }
}
