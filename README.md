# PgSafe

<div align="center">

<!-- Add your badges here -->
[![wakatime](https://wakatime.com/badge/user/018dd279-af88-40d4-86db-db3b3100ed1e/project/2256ad83-7d0d-4af3-9670-7d042251da98.svg)](https://wakatime.com/badge/user/018dd279-af88-40d4-86db-db3b3100ed1e/project/2256ad83-7d0d-4af3-9670-7d042251da98)
[![Version](https://img.shields.io/badge/version-0.3.2-blue.svg)](https://github.com/OndrejLosensky/PgSafe/releases)

lightweight PostgreSQL backup and restore CLI tool designed to manage multiple instances and databases safely. It allows parallel backups, restores to new database names, and keeps detailed metadata for every backup.

</div>

---
## Features 
- Backup and restore PostgreSQL databases
- Restore into the same database or a new database
- Parallel backup/restore operations
- Safety backups before overwriting existing databases
- Detailed metadata for backups:
  - Creation timestamp
  - Size in bytes
  - Table counts & row counts
  - PostgreSQL version
- CLI prompts with friendly menus
- Migrate to other PostgreSQL instances