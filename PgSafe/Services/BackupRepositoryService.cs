using PgSafe.Models.Backup;
using System.Text.Json;

namespace PgSafe.Services;

public class BackupRepositoryService
{
    private readonly string _root;

    public BackupRepositoryService(string root)
    {
        _root = root;
    }

    /// <summary>
    /// Get all backups for a given instance/database.
    /// Returns backups sorted descending by backupId (newest first).
    /// </summary>
    public List<BackupSet> GetAllBackups(string instance, string database)
    {
        var instanceDir = Path.Combine(_root, instance, database);
        if (!Directory.Exists(instanceDir))
            return new List<BackupSet>();

        var backupDirs = Directory.GetDirectories(instanceDir);

        var result = new List<BackupSet>();
        foreach (var dir in backupDirs)
        {
            var dumpFile = Path.Combine(dir, $"{database}.dump");
            var metaFile = Path.Combine(dir, "meta.json");

            if (!File.Exists(dumpFile) || !File.Exists(metaFile))
                continue;

            var metadata = JsonSerializer.Deserialize<BackupMetadata>(File.ReadAllText(metaFile))!;
            result.Add(new BackupSet
            {
                Instance = instance,
                Database = database,
                BackupId = Path.GetFileName(dir),
                BackupDirectory = dir,
                DumpPath = dumpFile,
                MetaPath = metaFile,
                Metadata = metadata
            });
        }

        return result;
    }


    /// <summary>
    /// Get the latest backup for a given instance/database, or null if none.
    /// </summary>
    public BackupSet? GetLatestBackup(string instance, string database)
    {
        return GetAllBackups(instance, database)
            .OrderByDescending(b => b.BackupId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Optionally: delete a backup set from disk.
    /// </summary>
    public bool DeleteBackup(BackupSet backup)
    {
        if (!Directory.Exists(backup.BackupDirectory))
            return false;

        Directory.Delete(backup.BackupDirectory, recursive: true);
        return true;
    }
}
