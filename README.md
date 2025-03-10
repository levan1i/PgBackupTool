# PgBackupTool

PgBackupTool is a .NET 9 application designed to automate the backup of PostgreSQL databases and upload the backups to an Amazon S3 bucket. The tool runs in an infinite loop, performing backups at specified intervals and managing the backup files in S3.

## Features

- Automated PostgreSQL database backups
- Configurable backup intervals
- Upload backups to Amazon S3
- Manage backup files in S3 (keep only the last 5 backups)

## Prerequisites

- .NET 9 SDK
- PostgreSQL database
- Amazon S3 account

## Configuration

The application is configured using the `appsettings.json` file.
- `backupTime`: The time interval between backups in `HH:mm` format.
- `db`: Database connection details.
  - `host`: The IP address or hostname of the PostgreSQL server.
  - `port`: The port number of the PostgreSQL server.
  - `databases`: A list of database names to back up.
  - `user`: The username for the PostgreSQL server.
  - `password`: The password for the PostgreSQL server.
- `AWS`: Amazon S3 configuration.
  - `keyId`: The AWS access key ID.
  - `key`: The AWS secret access key.
  - `bucket`: The name of the S3 bucket.
  - `baseUrl`: The base URL of the S3 bucket.
  - `region`: The AWS region of the S3 bucket.

## Usage

1. Clone the repository:
git clone https://github.com/levan1i/PgBackupTool.git
2. Navigate to the project directory: cd PgBackupTool  
3. Update the `appsettings.json` file with your configuration details.
4. Build the project: dotnet build
5. Run the project: dotnet run

The application will start and run in an infinite loop, performing backups at the specified intervals and uploading them to the configured S3 bucket.

## Docker Support

A `Dockerfile` is included for containerized deployment.


## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## Acknowledgements

- [Amazon S3 SDK for .NET](https://aws.amazon.com/sdk-for-net/)
- [Npgsql](https://www.npgsql.org/)

