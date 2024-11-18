using JW_Utils.JW_DataObjects;
using JW_Utils.JW_HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTP_UpLifter.Pages.Shared
{
    public class CreateInstanceFormModel : PageModel
    {
        private readonly ILogger<CreateInstanceFormModel> _logger;
        private readonly DataObjects _dataObjects;
        private readonly IFileWatcherService _fileWatcherService;

        [BindProperty]
        public InstanceSettings instanceSettings { get; private set; }
        [BindProperty]
        public FtpSettings ftpSettings { get; set; }
        [BindProperty]
        public SftpSettings sftpSettings { get; set; }
        [BindProperty]
        public ActivationSettings activationSettings { get; set; }
        [BindProperty]
        public WatchedDirectorySettings watchedDirectorySettings { get; set; }

        public CreateInstanceFormModel(ILogger<CreateInstanceFormModel> logger, DataObjects dataObjects, IFileWatcherService fileWatcherService)
        {
            _logger = logger;
            _dataObjects = dataObjects;
            _fileWatcherService = fileWatcherService;
        }

        public IActionResult OnPostCreateInstance(string instanceName, string ftpServer, string ftpUsername, string ftpPassword, string ftpDirectory, int ftpPort, bool sftpEnabled, string sftpPrivateKey, string watchDirectory, string fileExtension, bool watcherEnabled, string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, string fromEmail, string toEmail, bool emailEnabled)
        {
            // Logic to create a new instance
            instanceSettings = _dataObjects.Get_InstanceSettings();

            instanceSettings.Id = 1;
            instanceSettings.InstanceCount = instanceSettings.InstanceCount++;
            instanceSettings.ActiveInstance = instanceSettings.InstanceCount;

            _dataObjects.Save_InstanceSettings(instanceSettings);

            // Save the new instance settings
            WatchedDirectorySettings WatchedDirectorySettings = new WatchedDirectorySettings
            {
                Id = instanceSettings.InstanceCount,
                DirectoryPath = watchDirectory,
                FileExtension = fileExtension
            };

            FtpSettings FtpSettings = new FtpSettings
            {
                Id = instanceSettings.InstanceCount,
                FtpServer = ftpServer,
                FtpUsername = ftpUsername,
                FtpPassword = ftpPassword,
                DestinationFolder = ftpDirectory,
                Port = ftpPort,
                FileExtension = fileExtension
            };

            SftpSettings SftpSettings = new SftpSettings
            {
                Id = instanceSettings.InstanceCount,
                Host = ftpServer,
                Username = ftpUsername,
                Password = ftpPassword,
                RemotePath = ftpDirectory,
                Port = ftpPort,
                SshKeyPath = sftpPrivateKey
            };

            if (sftpEnabled)
            {
                _dataObjects.Save_SftpSettings(SftpSettings);
            }
            else
            {
                _dataObjects.Save_FtpSettings(FtpSettings);
            }

            EmailSettings EmailSettings = new EmailSettings
            {
                Id = instanceSettings.InstanceCount,
                EmailServer = smtpServer,
                EmailPort = smtpPort,
                EmailUsername = smtpUsername,
                EmailPassword = smtpPassword,
                EmailFrom = fromEmail,
                EmailTo = toEmail
            };

            _dataObjects.Save_EmailSettings(EmailSettings);

            ActivationSettings ActivationSettings = new ActivationSettings
            {
                Id = instanceSettings.InstanceCount,
                IsEnabled_FileWatcher = watcherEnabled,
                IsEnabled_Sftp = sftpEnabled,
                IsEnabled_EmailNotifications = emailEnabled,
                SettingsChangedSinceNoti = false
            };

            _dataObjects.Save_ActivationSettings(ActivationSettings);

            _fileWatcherService.AddFileWatcher(instanceSettings.InstanceCount);

            if (watcherEnabled)
            {
                _fileWatcherService.UpdateFileWatcherSettings(instanceSettings.InstanceCount);
            }

            // Redirect to the same page to refresh the data
            return RedirectToPage();
        }
    }
}
