using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaServer
{
    public class AvailableMedia
    {
        static private string[] fileArray;
        static string path;

        public AvailableMedia(string path)
        {
            path = path.ToLower();

            if (AvailableMedia.path == null || !AvailableMedia.path.Equals(path))
            {
                AvailableMedia.path = path;

                var extensions = new[] { "*.mp3", "*.mp4", "*.jpg", "*.png", "*.gif" };
                var files = new List<string>();

                foreach (var ext in extensions)
                {
                    files.AddRange(Directory.GetFiles(path, ext, SearchOption.AllDirectories));
                }

                fileArray = files.ToArray();
            }
        }

        public IEnumerable<string> getAvailableFiles()
        {
            return fileArray;
        }

        public string stripPath(string filename)
        {
            return filename.ToLower().Replace(AvailableMedia.path, "");
        }

        public string getAbsolutePath(int index)
        {
            return fileArray[index];
        }
    }
}
