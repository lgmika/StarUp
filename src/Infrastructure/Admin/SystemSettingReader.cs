using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StartupConnect.Application.Admin.Interfaces;
using StartupConnect.Infrastructure.Persistence;

namespace StartupConnect.Infrastructure.Admin;

public sealed class SystemSettingReader(AppDbContext dbContext) : ISystemSettingReader
{
    private IReadOnlyDictionary<string, string>? values;

    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken)
    {
        values ??= await dbContext.SystemSettings
            .AsNoTracking()
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value, StringComparer.Ordinal, cancellationToken);

        return values.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<bool> GetBooleanAsync(string key, bool fallback, CancellationToken cancellationToken)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async Task<long> GetInt64Async(string key, long fallback, CancellationToken cancellationToken)
    {
        var value = await GetValueAsync(key, cancellationToken);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }
}
