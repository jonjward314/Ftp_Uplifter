using FTP_UpLifter.Pages.Shared;
using JW_Utils.JW_DataObjects;
using JW_Utils.JW_HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
public class IndexModel : PageModel
{
    public CreateInstanceFormModel CreateInstanceForm { get; set; }
    private readonly ILogger<IndexModel> _logger;
    private readonly DataObjects _dataObjects;
    private readonly IFileWatcherService _fileWatcherService;

    [BindProperty]
    public InstanceSettings instanceSettings { get; private set; }
    [BindProperty]
    public FtpSettings[] ftpSettings { get; set; }
    [BindProperty]
    public SftpSettings[] sftpSettings { get; set; }
    [BindProperty]
    public ActivationSettings[] activationSettings { get; set; }
    [BindProperty]
    public WatchedDirectorySettings[] watchedDirectorySettings { get; set; }
    public bool ShowForm { get; private set; }

    public IndexModel(ILogger<IndexModel> logger, DataObjects dataObjects, IFileWatcherService fileWatcherService)
    {
        // Initialize the CreateInstanceForm property
        _logger = logger;
        _dataObjects = dataObjects;
        _fileWatcherService = fileWatcherService;
    }

    public void OnGet()
    {
        // Example usage of DataObjects
        instanceSettings = _dataObjects.Get_InstanceSettings();
        // Use instanceSettings as needed

        if (instanceSettings.InstanceCount == 0)
        {
            ShowForm = true;
        }
        else
        {
            ShowForm = false;

            ftpSettings = new FtpSettings[instanceSettings.InstanceCount];
            sftpSettings = new SftpSettings[instanceSettings.InstanceCount];
            activationSettings = new ActivationSettings[instanceSettings.InstanceCount];
            watchedDirectorySettings = new WatchedDirectorySettings[instanceSettings.InstanceCount];

            for (int i = 1; i <= instanceSettings.InstanceCount; i++)
            {
                ftpSettings[i - 1] = _dataObjects.Get_FtpSettings(i);
                sftpSettings[i - 1] = _dataObjects.Get_SftpSettings(i);
                activationSettings[i - 1] = _dataObjects.Get_ActivationSettings(i);
                watchedDirectorySettings[i - 1] = _dataObjects.Get_WatchedDirectorySettings(i);
                // Use ftpSettings, sftpSettings, activationSettings, watchedDirectorySettings as needed
            }

            // Use ftpSettings, sftpSettings, activationSettings, watchedDirectorySettings as needed


            // Example usage of FileWatcherService
            //_fileWatcherService.AddFileWatcher(1);

            // Set ShowForm based on some condition
        }
    }

    public IActionResult OnPostGetStarted()
    {
        // Set ShowForm to true to display the form
        instanceSettings = _dataObjects.Get_InstanceSettings();
        ShowForm = true;
        return Page();
    }

    public IActionResult OnPostCreateInstance(string instanceName, string ftpServer, string ftpUsername, string ftpPassword, string ftpDirectory, int ftpPort, bool sftpEnabled, string sftpPrivateKey, string watchDirectory, string fileExtension, bool watcherEnabled, string smtpServer, int smtpPort, string smtpUsername, string smtpPassword, string fromEmail, string toEmail, bool emailEnabled)
    {
        // Logic to create a new instance
        instanceSettings = _dataObjects.Get_InstanceSettings();

        instanceSettings.Id = 1;
        instanceSettings.InstanceCount = ++instanceSettings.InstanceCount;
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
