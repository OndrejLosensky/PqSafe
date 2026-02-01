public class BackupPathResolver
{
    private readonly string _root;

    public BackupPathResolver(string root)
    {
        _root = root;
    }

    public string GetDatabaseRoot(string instance, string database)
        => Path.Combine(_root, instance, database);

    public string GetBackupDir(string instance, string database, string backupId)
        => Path.Combine(_root, instance, database, backupId);

    public string GetDumpPath(string instance, string database, string backupId)
        => Path.Combine(
            GetBackupDir(instance, database, backupId),
            $"{database}.dump"
        );

    public string GetMetaPath(string instance, string database, string backupId)
        => Path.Combine(
            GetBackupDir(instance, database, backupId),
            "meta.json"
        );
}