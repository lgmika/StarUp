namespace StartupConnect.Domain.Entities;

public sealed class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Group { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Type { get; set; } = "string";

    public bool IsReadonly { get; set; }
}
