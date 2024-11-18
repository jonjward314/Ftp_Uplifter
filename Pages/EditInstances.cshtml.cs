using FTP_UpLifter.Pages.Shared;
using JW_Utils.JW_DataObjects;
using JW_Utils.JW_HostedServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FTP_UpLifter.Pages
{
    public class EditInstancesModel : PageModel
    {
        public CreateInstanceFormModel CreateInstanceForm { get; set; }
        private readonly ILogger<IndexModel> _logger;
        private readonly DataObjects _dataObjects;
        private readonly IFileWatcherService _fileWatcherService;
        public bool ShowForm { get; private set; }

        [BindProperty]
        public InstanceSettings instanceSettings { get; private set; }
        [BindProperty]
        public int SelectedInstance { get; set; }

        public EditInstancesModel(ILogger<IndexModel> logger, DataObjects dataObjects, IFileWatcherService fileWatcherService)
        {
            _logger = logger;
            _dataObjects = dataObjects;
            _fileWatcherService = fileWatcherService;
        }

        public void OnGet()
        {
            instanceSettings = _dataObjects.Get_InstanceSettings();
        }

        public IActionResult OnPostInstanceChangePost()
        {
            instanceSettings.ActiveInstance = SelectedInstance;
            _dataObjects.Save_InstanceSettings(instanceSettings);
            return RedirectToPage();
        }

        public IActionResult OnPostAddInstancePost()
        {
            instanceSettings = _dataObjects.Get_InstanceSettings();
            _dataObjects.InitializeAuxilaryDataStore(instanceSettings.InstanceCount + 1);
            instanceSettings.InstanceCount++;
            instanceSettings.ActiveInstance = instanceSettings.InstanceCount;
            _dataObjects.Save_InstanceSettings(instanceSettings);
            return RedirectToPage();
        }
        public IActionResult OnPostDeleteInstancePost()
        {
            return RedirectToPage();
        }
    }
}