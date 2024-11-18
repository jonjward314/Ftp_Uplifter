using JW_Utils.JW_DataObjects;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System.IO;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;

namespace JW_Utils.JW_HostedServices
{
    public interface IFileWatcherService
    {
        void AddFileWatcher(int instanceNumber);
        void UpdateFileWatcherSettings(int instanceNumber);
    }
    public class FileWatcherService : IHostedService, IFileWatcherService
    {
        private readonly ILogger<FileWatcherService> _logger;
        private readonly DataObjects _dataObject;
        private readonly List<FileSystemWatcher> _fileWatchers = new();
        private Timer? _timer;

        public FileWatcherService(DataObjects dataObject, ILogger<FileWatcherService> logger)
        {
            _dataObject = dataObject;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            InitializeFileWatchers();
            return Task.CompletedTask;
        }

        private void InitializeFileWatchers()
        {
            var instanceSettings = _dataObject.Get_InstanceSettings();
            if ((instanceSettings is not null) && instanceSettings.InstanceCount > 0)
            {
                for (int i = 1; i <= instanceSettings.InstanceCount; i++)
                {
                    AddFileWatcher(i);
                }
            }
            else
            {
                return;
            }
        }

        public void AddFileWatcher(int instanceNumber)
        {
            _dataObject.InitializeAuxilaryDataStore(instanceNumber);
            var directoryPath = _dataObject.Get_WatchedDirectorySettings(instanceNumber).DirectoryPath;

            if (!Directory.Exists(directoryPath))
            {
                _logger.LogError($"Directory does not exist: {directoryPath}");
                return;
            }

            var fileWatcher = new FileSystemWatcher
            {
                Path = directoryPath,
                Filter = "*" + _dataObject.Get_WatchedDirectorySettings(instanceNumber).FileExtension,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                EnableRaisingEvents = _dataObject.Get_ActivationSettings(instanceNumber).IsEnabled_FileWatcher
            };

            fileWatcher.Created += (sender, e) => OnCreated(sender, e, instanceNumber);
            _fileWatchers.Add(fileWatcher);
        }

        public void UpdateFileWatcherSettings(int instanceNumber)
        {
            var instanceSettings = _dataObject.Get_InstanceSettings();

            if (instanceNumber <= 0 || instanceNumber > instanceSettings.InstanceCount)
            {
                _logger.LogError($"Invalid instance number: {instanceNumber}");
                return;
            }

            var fileWatcher = _fileWatchers[instanceNumber - 1];
            _dataObject.InitializeAuxilaryDataStore(instanceNumber);

            fileWatcher.Path = _dataObject.Get_WatchedDirectorySettings(instanceNumber).DirectoryPath;
            fileWatcher.Filter = "*" + _dataObject.Get_WatchedDirectorySettings(instanceNumber).FileExtension;
            fileWatcher.EnableRaisingEvents = _dataObject.Get_ActivationSettings(instanceNumber).IsEnabled_FileWatcher;

            // Check if new instances have been added
            if (_fileWatchers.Count < instanceSettings.InstanceCount)
            {
                for (int i = _fileWatchers.Count + 1; i <= instanceSettings.InstanceCount; i++)
                {
                    AddFileWatcher(i);
                }
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e, int instanceNumber)
        {
            _logger.LogInformation($"File: {e.FullPath} {e.ChangeType} Watcher Instance: {instanceNumber}");
            if (_dataObject.Get_ActivationSettings(instanceNumber).IsEnabled_Sftp)
            {
                ISftpUploader sftpUploader = new SftpUploader(_dataObject.Get_SftpSettings(instanceNumber));
                sftpUploader.UploadFile(e.FullPath);
            }
            else
            {
                IFtpUploader ftpUploader = new FtpUploader(_dataObject.Get_FtpSettings(instanceNumber));
                ftpUploader.UploadFile(e.FullPath);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            foreach (var fileWatcher in _fileWatchers)
            {
                fileWatcher.Dispose();
            }
            return Task.CompletedTask;
        }
    }


    public interface ISftpUploader
    {
        bool UploadFile(string sourceFilePath);
    }

    public class SftpUploader : ISftpUploader, IDisposable
    {
        private readonly ILogger<SftpUploader> _logger;
        private readonly SftpSettings settings;
        private SftpClient sftp;

        public SftpUploader(SftpSettings mySettings)
        {
            settings = mySettings;
            sftp = new SftpClient(settings.Host, settings.Port, settings.Username, settings.Password);
        }

        public bool UploadFile(string sourceFilePath, string destinationFileName)
        {
            try
            {
                sftp.Connect();
                if (sftp.IsConnected)
                {
                    using (var fileStream = File.OpenRead(sourceFilePath))
                    {
                        string RemotePath = Path.Combine(settings.RemotePath, destinationFileName);
                        sftp.UploadFile(fileStream, RemotePath);
                    }
                    sftp.Disconnect();
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Could not connect to the FTP server.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
        public bool UploadFile(string sourceFilePath)
        {

            try
            {
                sftp.Connect();
                if (sftp.IsConnected)
                {
                    using (var fileStream = File.OpenRead(sourceFilePath))
                    {
                        var fileName = Path.GetFileName(sourceFilePath);
                        var destinationFileName = Path.Combine(settings.RemotePath, fileName);
                        sftp.UploadFile(fileStream, destinationFileName);
                    }
                    sftp.Disconnect();
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Could not connect to the FTP server.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
        public void Dispose()
        {
            if (sftp != null)
            {
                sftp.Dispose();
                sftp = null;
            }
        }
    }
    public interface IFtpUploader
    {
        bool UploadFile(string sourceFilePath);
    }

    public class FtpUploader : IFtpUploader, IDisposable
    {
        private readonly ILogger<FtpUploader> _logger;
        private readonly FtpSettings _ftpSettings;

        public FtpUploader(FtpSettings ftpSettings)
        {
            _ftpSettings = ftpSettings ?? throw new ArgumentNullException(nameof(ftpSettings));
            _logger = new Logger<FtpUploader>(new LoggerFactory());
        }

        public void UploadFile(string filePath)
        {
            if (string.IsNullOrEmpty(_ftpSettings.DestinationFolder))
            {
                var errorMessage = "Destination folder is not set in the database. Cannot upload dropped files.";
                _logger.LogError(errorMessage);
                return;
            }

            var fileName = Path.GetFileName(filePath);
            var destinationFileName = Path.Combine(_ftpSettings.DestinationFolder, fileName);

            try
            {
                var request = (FtpWebRequest)WebRequest.Create(new Uri($"ftp://{_ftpSettings.FtpServer}/{destinationFileName}"));
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftpSettings.FtpUsername, _ftpSettings.FtpPassword);

                using (var fileStream = File.OpenRead(filePath))
                using (var requestStream = request.GetRequestStream())
                {
                    fileStream.CopyTo(requestStream);
                }

                _logger.LogInformation($"File uploaded to FTP: {destinationFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file to FTP: {destinationFileName}");
            }
        }

        bool IFtpUploader.UploadFile(string sourceFilePath)
        {
            UploadFile(sourceFilePath);
            return true;
        }

        public void Dispose()
        {
            // Dispose of any resources if necessary
        }
    }
}

   
