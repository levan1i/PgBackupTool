using Microsoft.Extensions.Configuration;
using PgBackupTool;

var processor = new BackupProcessor();


await processor.Run();