using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaServer
{
    public class AvailableMedia
    {
        private string[] fileArray;
        private string path;

        public AvailableMedia(string path)
        {
            this.path = path.ToLower();
            LoadFiles();
        }

        // Loads media files from the specified directory
        private void LoadFiles()
        {
            fileArray = Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(path, "*.mp4", SearchOption.AllDirectories))
                .Union(Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories))
                .Union(Directory.GetFiles(path, "*.png", SearchOption.AllDirectories))
                .ToArray();

            Console.WriteLine($"Loaded {fileArray.Length} media files from {path}");
        }

        public IEnumerable<string> GetAvailableFiles()
        {
            return fileArray;
        }

        public string GetFilePath(int index)
        {
            return fileArray[index];
        }
    }
}
