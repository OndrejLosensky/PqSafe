using PgSafe.Config;

namespace PgSafe.Models.Migration;

public sealed record MigrationTarget(
    string SourceInstanceName,
    string SourceDatabaseName,
    PgInstanceConfig SourceInstanceConfig,

    string TargetInstanceName,
    string TargetDatabaseName,
    PgInstanceConfig TargetInstanceConfig,

    string OutputDir,
    bool DryRun
);