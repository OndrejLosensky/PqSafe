using PgSafe.Config;

namespace PgSafe.Models.Backup;

public record BackupTarget(
    string InstanceName,
    PgInstanceConfig InstanceConfig,
    string DatabaseName
);