using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading;

namespace MediaServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("eL33T Media Server");

            string fileName = "properties.json";
            string rawProperties;

            try
            {
                // Check if the properties file exists
                if (!File.Exists(fileName))
                {
                    Console.WriteLine("Error: The properties.json file is missing.");
                    return;
                }

                rawProperties = File.ReadAllText(fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading properties file: {ex.Message}");
                return;
            }

            JsonNode properties;

            try
            {
                properties = JsonNode.Parse(rawProperties)!;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing properties file: {ex.Message}");
                return;
            }

            // Extract values or fallback to defaults
            string ip = properties["ip"]?.GetValue<string>() ?? "127.0.0.1";
            int port = properties["port"]?.GetValue<int>() ?? 8080;
            string mediaDir = properties["mediaDir"]?.GetValue<string>() ?? "./media";

            // Initialize the Server with provided config
            Server server = new Server(ip, port, mediaDir);

            // Start the server in a new thread
            Thread thread = new Thread(() =>
            {
                server.Start();
            });
            thread.Start();

            Console.WriteLine("Server is running... Access via link: http://{0}:{1}", ip, port);
            Console.WriteLine("Press Enter to stop the server...");

            Console.ReadLine(); // Wait for user input to stop the server
            server.Stop();      // Stop the server gracefully
            thread.Join();      // Wait for the server thread to finish
            Console.WriteLine("Server stopped.");
        }
    }
}
