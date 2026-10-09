using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CampusLife.Core.Save
{
    /// <summary>Key/value text storage. Implementations must make <see cref="Write"/> atomic.</summary>
    public interface ISaveStorage
    {
        bool TryRead(string key, out string content);
        void Write(string key, string content);
        bool Exists(string key);
        void Delete(string key);
    }

    /// <summary>In-memory storage for tests and for platforms where persistence is unavailable.</summary>
    public sealed class MemorySaveStorage : ISaveStorage
    {
        readonly Dictionary<string, string> _data = new Dictionary<string, string>(StringComparer.Ordinal);

        public bool TryRead(string key, out string content) => _data.TryGetValue(key, out content);
        public void Write(string key, string content) => _data[key] = content ?? throw new ArgumentNullException(nameof(content));
        public bool Exists(string key) => _data.ContainsKey(key);
        public void Delete(string key) => _data.Remove(key);
    }

    /// <summary>
    /// File storage that writes to a temporary file first and then swaps it in,
    /// so a crash or power loss mid-write never leaves a half-written save.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        readonly string _directory;

        public FileSaveStorage(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Directory required.", nameof(directory));
            _directory = directory;
        }

        string PathFor(string key)
        {
            if (string.IsNullOrEmpty(key) || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || key.Contains(".."))
                throw new ArgumentException($"Invalid save key '{key}'.", nameof(key));
            return Path.Combine(_directory, key + ".json");
        }

        public bool TryRead(string key, out string content)
        {
            string path = PathFor(key);
            if (!File.Exists(path)) { content = null; return false; }
            content = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        public void Write(string key, string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            Directory.CreateDirectory(_directory);
            string path = PathFor(key);
            string temp = path + ".tmp";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                try { File.Replace(temp, path, null); }
                catch (PlatformNotSupportedException) { File.Delete(path); File.Move(temp, path); }
                catch (IOException) { File.Delete(path); File.Move(temp, path); }
            }
            else
            {
                File.Move(temp, path);
            }
        }

        public bool Exists(string key) => File.Exists(PathFor(key));

        public void Delete(string key)
        {
            string path = PathFor(key);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
