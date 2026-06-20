namespace GitManagerApp.Cache;

public interface ICacheService
{
    string? Get(string key);
    void Set(string key, string value);
    void DeleteEntry(string key);
    void DeleteAll();
}
