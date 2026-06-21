namespace StartupConnect.Application.Admin.Interfaces;

public interface ISystemSettingReader
{
    Task<string?> GetValueAsync(string key, CancellationToken cancellationToken);

    Task<bool> GetBooleanAsync(string key, bool fallback, CancellationToken cancellationToken);

    Task<long> GetInt64Async(string key, long fallback, CancellationToken cancellationToken);
}
