using System.Text.Json;

namespace PgBackupTool
{
    public class FileRepo
    {
        public IEnumerable<DbBackUpdata>? GetBackUpInfo()
        {

            //check file
            if (!System.IO.File.Exists("DbBackUpdata.json"))
            {
                System.IO.File.Create("DbBackUpdata.json").Close();
                return null;
            }

            var json = System.IO.File.ReadAllText("DbBackUpdata.json");

            try
            {
                // json to DbBackUpdata using json convert
                var parsed = JsonSerializer.Deserialize<IEnumerable<DbBackUpdata>>(json);
                return parsed;
            }
            catch
            {
                return null;
            }
        }

        //store data
        public void StoreBackUpInfo(IEnumerable<DbBackUpdata> data)
        {
            var json = JsonSerializer.Serialize(data);
            System.IO.File.WriteAllText("DbBackUpdata.json", json);
        }
    }
}
