using System.Text;

namespace GitManagerApp.Cache;

public sealed class FileCacheService : ICacheService
{
    private readonly Dictionary<string, string> _cache = new();
    private readonly string _file;

    public FileCacheService(string file)
    {
        _file = file;
        Load();
    }

    public string? Get(string key) => _cache.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value)
    {
        _cache[key] = value;
        Save();
    }

    public void DeleteEntry(string key)
    {
        _cache.Remove(key);
        Save();
    }

    public void DeleteAll()
    {
        _cache.Clear();
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_file))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(_file))
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                _cache[parts[0]] = parts[1];
            }
        }
    }

    private void Save()
    {
        var builder = new StringBuilder();

        foreach (var (key, value) in _cache)
        {
            builder.AppendLine($"{key}={value}");
        }

        File.WriteAllText(_file, builder.ToString());
    }
}
