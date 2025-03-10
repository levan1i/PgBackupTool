using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PgBackupTool
{
    public class BackupProcessor
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupProcessor> _logger;

        public BackupProcessor()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            _configuration = builder.Build();

            _logger = LoggerFactory.Create(x =>
            {

                x.SetMinimumLevel(LogLevel.Information);
                x.AddConsole().AddSimpleConsole(x =>
                {
                    x.IncludeScopes = true;
                });
            }).CreateLogger<BackupProcessor>();
        }

        //run in infinite loop with 15m interval
        public async Task Run()
        {
            while (true)
            {
                try
                {
                    await ProcessBackups();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing backups");
                }
                await Task.Delay(900000);
            }
        }

        public async Task ProcessBackups()
        {
            var databases = _configuration.GetSection("db:databases").Get<string[]>();
            var backupTime = _configuration["backupTime"];
            int hours = Convert.ToInt16(backupTime.Split(":")[0]);
            int minutes = Convert.ToInt16(backupTime.Split(":")[1]);
            var repo = new FileRepo();
            var buckupInfo = repo.GetBackUpInfo();

            foreach (var database in databases ?? [])
            {
                DbBackUpdata dbBackUpdata = null;
                if (buckupInfo != null)
                {
                    var dbInfo = buckupInfo.FirstOrDefault(x => x.DbName == database);
                    if (dbInfo != null)
                    {
                        dbBackUpdata = dbInfo;
                        _logger.LogInformation($"Last backup for {database} was at {dbInfo.LastBackUp}");
                    }
                }
                if (dbBackUpdata == null)
                {
                    dbBackUpdata = new DbBackUpdata
                    {
                        DbName = database
                    };
                }

                if (dbBackUpdata.LastBackUp != null)
                {

                    if (dbBackUpdata.BackUpWasSuccesfull)
                    {
                        var lastBackup = dbBackUpdata.LastBackUp.Value;
                        var nextBackup = lastBackup.AddHours(hours).AddMinutes(minutes);
                        if (nextBackup > DateTime.UtcNow)
                        {
                            _logger.LogInformation($"Next backup for {database} is scheduled at {nextBackup}");
                            continue;
                        }
                    }

                }
                else
                {
                    try
                    {
                        _logger.LogInformation($"{database} backup exec started");
                        dbBackUpdata.BackUpWasSuccesfull = await ProcessBackUp(database);
                    }                   
                    catch(Exception ex)
                    {
                        _logger.LogError(ex, "Error processing backups");
                        dbBackUpdata.BackUpWasSuccesfull = false;
                    }

                    dbBackUpdata.LastBackUp = DateTime.UtcNow;
                    if (buckupInfo == null)
                    {
                        buckupInfo = new List<DbBackUpdata>()
                        {
                            dbBackUpdata
                        };
                    }
                    else
                    {
                        var list = buckupInfo.ToList();
                        list.RemoveAll(x => x.DbName == database);
                        list.Add(dbBackUpdata);
                        buckupInfo = list;
                    }

                    repo.StoreBackUpInfo(buckupInfo);
                }

            }
        }

        async Task<bool> ProcessBackUp(string database)
        {
            var host = _configuration["db:host"];
            var port = _configuration["db:port"];
            var user = _configuration["db:user"];
            var password = _configuration["db:password"];

            var awsBucket = _configuration["AWS:s3:bucket"];
            //generate filename with timestamp
            var filename = $"backup_{DateTime.Now:yyyyMMddHHmmss}.bin";

            var srInfo =
                new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    Arguments = $"-h {host} -p {port} -U {user} -d {database} -v -f ./{filename}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };


            srInfo.EnvironmentVariables.Add("PGPASSWORD", password);

            using (var proc = Process.Start(srInfo))
            {
                _logger.LogInformation($"{database} backup started");

                if (proc == null)
                {
                    _logger.LogError("Failed to start pg_dump");
                    return false;
                }

                //read process output
                while (!proc.HasExited)
                {
                    var line = await proc.StandardOutput.ReadLineAsync();
                    Console.WriteLine(line);
                }

                await proc.WaitForExitAsync();

                //check status
                if (proc.ExitCode != 0)
                {
                    var msg = await proc.StandardError.ReadToEndAsync();
                    _logger.LogError($"{database} Backup failed: {msg}");
                    return false;
                }
                else
                {
                    //check if file exists
                    if (!File.Exists(filename))
                    {
                        _logger.LogError($"{database} Backup file not found");
                        return false;
                    }

                    _logger.LogInformation($"{database} Backup completed");
                }
            }

            AmazonS3Config config = new AmazonS3Config();

            var creds = new BasicAWSCredentials(_configuration["AWS:S3:keyId"], _configuration["AWS:S3:key"]);
            var awsRegion = RegionEndpoint.GetBySystemName(_configuration["AWS:S3:region"]);

            using (var s3 = new AmazonS3Client(creds, awsRegion))
            {
                using (var ms = new MemoryStream())
                {
                    using (var filestrem = new FileStream(filename, FileMode.Open))
                    {
                        filestrem.CopyTo(ms);
                    }
                    _logger.LogInformation($"Uploading {database} backup to S3");
                    //upload to s3
                    var response = await s3.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                    {
                        BucketName = awsBucket,
                        Key = $"dbbackups/{database}/{filename}",
                        InputStream = ms
                    });

                    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                    {
                        _logger.LogInformation($"{database} Backup uploaded to S3");
                        //delete file
                        File.Delete(filename);

                        //keep only last 5 backups
                        var list = new List<string>();
                        var objects = await s3.ListObjectsAsync(awsBucket, $"dbbackups/{database}/");
                        foreach (var obj in objects.S3Objects)
                        {
                            list.Add(obj.Key);
                        }

                        if (list.Count > 5)
                        {
                            list.Sort();
                            for (int i = 0; i < list.Count - 5; i++)
                            {
                                await s3.DeleteObjectAsync(awsBucket, list[i]);
                            }
                        }

                        return true;
                    }
                    else
                    {
                        _logger.LogError($"Failed to upload {database} backup to S3");
                        return false;
                    }
                }
            }
        }
    }
}
