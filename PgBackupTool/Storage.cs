namespace PgBackupTool
{
    public class Storage
    {
        public IEnumerable<DbBackUpdata> BackUpInfo { get; set; }
    }

    public class DbBackUpdata
    {
        public string DbName { get; set; }
        public DateTime? LastBackUp { get; set; }
        public bool BackUpWasSuccesfull { get; set; }
    }
}
