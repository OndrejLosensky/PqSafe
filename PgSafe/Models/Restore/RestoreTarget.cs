using PgSafe.Config;

namespace PgSafe.Models.Restore;

public record RestoreTarget(
    string Instance,
    PgInstanceConfig InstanceConfig,
    string Database,
    string DumpFilePath
);  