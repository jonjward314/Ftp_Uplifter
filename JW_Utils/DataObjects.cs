
namespace JW_Utils.JW_DataObjects
{
    using LiteDB;
    using System.Security.Cryptography;
    using System.Text;

    

    /// <summary>
    /// Interface for data objects operations.
    /// </summary>
    public interface IDataObjects
    {
        SftpSettings Get_SftpSettings(int record);
        void Save_SftpSettings(SftpSettings settings);
        FtpSettings Get_FtpSettings(int record);
        void Save_FtpSettings(FtpSettings settings);
        WatchedDirectorySettings Get_WatchedDirectorySettings(int record);
        void Save_WatchedDirectorySettings(WatchedDirectorySettings settings);
        EmailSettings Get_EmailSettings(int record);
        void Save_EmailSettings(EmailSettings settings);
        ActivationSettings Get_ActivationSettings(int record);
        void Save_ActivationSettings(ActivationSettings settings);
        InstanceSettings Get_InstanceSettings();
        void Save_InstanceSettings(InstanceSettings settings);
        void Set_Password(string password);
        void Set_Path(string filename);
        public void InitializeAuxilaryDataStore(int index);
    }
    /// <summary>
    /// Implementation of IDataObjects interface for managing data objects.
    /// </summary>
    public class DataObjects : IDataObjects
    {
        private string _datastorePassword;
        private string _datastorePath;

        public DataObjects() { }

        public DataObjects(string filepath)
        {
            _datastorePath = filepath;
            _datastorePassword = HashUtils.ComputeSha256Hash("password");
        }

        public DataObjects(string filepath, string password)
        {
            _datastorePath = filepath;
            _datastorePassword = password;
        }

        public void Set_Password(string password)
        {
            _datastorePassword = password;
        }

        public void Set_Path(string path)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var dataDirectory = Path.Combine(baseDirectory, "Data");

            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            _datastorePath = Path.Combine(dataDirectory, path);
        }

        private LiteDatabase GetDatabase()
        {
            return new LiteDatabase($"Filename={_datastorePath};Password={_datastorePassword}");
        }

        private T GetSettings<T>(int record, string collectionName) where T : new()
        {
            try
            {
                using (var db = GetDatabase())
                {
                    var collection = db.GetCollection<T>(collectionName);
                    return collection.FindById(record) ?? new T();
                }
            }
            catch (Exception ex)
            {
                throw new IndexOutOfRangeException($"Error retrieving {typeof(T).Name}", ex);
            }
        }

        private void SaveSettings<T>(T settings, string collectionName) where T : class, new()
        {
            try
            {
                using (var db = GetDatabase())
                {
                    var collection = db.GetCollection<T>(collectionName);
                    var idProperty = typeof(T).GetProperty("Id");
                    if (idProperty == null)
                    {
                        throw new ApplicationException($"The type {typeof(T).Name} does not contain a property named 'Id'.");
                    }

                    var idValue = new BsonValue(idProperty.GetValue(settings));
                    if (!collection.Exists(Query.EQ("_id", idValue)))
                    {
                        collection.Insert(settings);
                    }
                    else
                    {
                        collection.Update(settings);
                    }
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IndexOutOfRangeException($"Index out of range while saving {typeof(T).Name}. Ensure the array is properly initialized.", ex);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error saving {typeof(T).Name}", ex);
            }
        }

        private void InitializeSettings<T>(string collectionName, T defaultRecord) where T : class, new()
        {
            try
            {
                using (var db = GetDatabase())
                {
                    var collection = db.GetCollection<T>(collectionName);
                    collection.EnsureIndex("Id");

                    // Check if the collection is empty and insert the default record if it is
                    if (!collection.Exists(Query.All()))
                    {
                        collection.Insert(defaultRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error initializing {typeof(T).Name}", ex);
            }
        }

        private void DeleteSettings<T>(int record, string collectionName) where T : class, new()
        {
            try
            {
                using (var db = GetDatabase())
                {
                    var collection = db.GetCollection<T>(collectionName);
                    if (!collection.Delete(record))
                    {
                        throw new KeyNotFoundException($"No record found with ID {record} in {collectionName}.");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error deleting record from {collectionName}: {ex.Message}", ex);
            }
        }
        private void InitializePrimaryDataStore()
        {
            InitializeSettings("SftpSettings", new SftpSettings());
            InitializeSettings("FtpSettings", new FtpSettings());
            InitializeSettings("WatchedDirectorySettings", new WatchedDirectorySettings());
            InitializeSettings("EmailSettings", new EmailSettings());
            InitializeSettings("ActivationSettings", new ActivationSettings());
            InitializeSettings("InstanceSettings", new InstanceSettings());
        }
        public void InitializeAuxilaryDataStore(int index)
        {
            InitializeSettings("SftpSettings", new SftpSettings(index));
            InitializeSettings("FtpSettings", new FtpSettings(index));
            InitializeSettings("WatchedDirectorySettings", new WatchedDirectorySettings(index));
            InitializeSettings("EmailSettings", new EmailSettings(index));
            InitializeSettings("ActivationSettings", new ActivationSettings(index));
        }
        public SftpSettings Get_SftpSettings(int record) => GetSettings<SftpSettings>(record, "SftpSettings");

        public void Save_SftpSettings(SftpSettings settings) => SaveSettings(settings, "SftpSettings");

        public FtpSettings Get_FtpSettings(int record) => GetSettings<FtpSettings>(record, "FtpSettings");

        public void Save_FtpSettings(FtpSettings settings) => SaveSettings(settings, "FtpSettings");

        public WatchedDirectorySettings Get_WatchedDirectorySettings(int record) => GetSettings<WatchedDirectorySettings>(record, "WatchedDirectorySettings");
        public void Save_WatchedDirectorySettings(WatchedDirectorySettings settings) => SaveSettings(settings, "WatchedDirectorySettings");

        public EmailSettings Get_EmailSettings(int record) => GetSettings<EmailSettings>(record, "EmailSettings");

        public void Save_EmailSettings(EmailSettings settings) => SaveSettings(settings, "EmailSettings");

        public ActivationSettings Get_ActivationSettings(int record) => GetSettings<ActivationSettings>(record, "ActivationSettings");

        public void Save_ActivationSettings(ActivationSettings settings) => SaveSettings(settings, "ActivationSettings");

        public InstanceSettings Get_InstanceSettings() => GetSettings<InstanceSettings>(1, "InstanceSettings");

        public void Save_InstanceSettings(InstanceSettings settings) => SaveSettings(settings, "InstanceSettings");

        public void Delete_SftpSettings(int record) => DeleteSettings<SftpSettings>(record, "SftpSettings");
        public void Delete_FtpSettings(int record) => DeleteSettings<FtpSettings>(record, "FtpSettings");
        public void Delete_WatchedDirectorySettings(int record) => DeleteSettings<WatchedDirectorySettings>(record, "WatchedDirectorySettings");
        public void Delete_EmailSettings(int record) => DeleteSettings<EmailSettings>(record, "EmailSettings");
        public void Delete_ActivationSettings(int record) => DeleteSettings<ActivationSettings>(record, "ActivationSettings");
        public void Delete_InstanceSettings() => DeleteSettings<InstanceSettings>(1, "InstanceSettings"); // Assuming you use a static ID for instance settings.

    }

    /// <summary>
    /// Class representing FTP settings.
    /// </summary>
    public class FtpSettings
    {
        public FtpSettings() { }
        public FtpSettings(int index)
        {
            Id = index;
        }
        public int Id { get; set; } = 1;
        public string FtpServer { get; set; } = "ftp.example.com";
        public string FtpUsername { get; set; } = "anonymous";
        public string FtpPassword { get; set; } = "password";
        public int Port { get; set; } = 21;
        public string DestinationFolder { get; set; } = "/uploads";
        public string FileExtension { get; set; } = ".txt";
        public string Info { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class representing Watched Directory settings.
    /// </summary>

    public class WatchedDirectorySettings
    {
        public WatchedDirectorySettings() { }
        public WatchedDirectorySettings(int index)
        {
            Id = index;
        }
        public string Name { get; set; } = "File Watcher Name";
        public int Id { get; set; } = 1;
        public string DirectoryPath { get; set; } = @"C:\Temp";
        public string FileExtension { get; set; } = ".*";
        public string Info { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class representing Email settings.
    /// </summary>
    public class EmailSettings
    {
        public EmailSettings() { }
        public EmailSettings(int index)
        {
            Id = index;
        }
        public int Id { get; set; } = 1;
        public string EmailServer { get; set; } = "smtp.example.com";
        public int EmailPort { get; set; } = 587;
        public string EmailUsername { get; set; } = "username";
        public string EmailPassword { get; set; } = "password";
        public string EmailFrom { get; set; } = "Sender_Address@email.com";
        public string EmailTo { get; set; } = "Reciever_Address@email.com";
        public string Info { get; set; } = string.Empty;
    }

    /// <summary>
    /// Class representing Activation settings.
    /// </summary>
    public class ActivationSettings
    {
        public ActivationSettings() { }
        public ActivationSettings(int index)
        {
            Id = index;
        }
        public int Id { get; set; }
        public bool IsEnabled_FileWatcher { get; set; } = false;
        public bool IsEnabled_Sftp { get; set; } = false;
        public bool IsEnabled_EmailNotifications { get; set; } = false;
        public bool SettingsChangedSinceNoti { get; set; } = false;
    }

    /// <summary>
    /// Class representing Instance settings.
    /// </summary>
    public class InstanceSettings
    {
        public InstanceSettings() { }
        public int Id { get; set; } = 0;
        public int InstanceCount { get; set; } = 0;
        public int ActiveInstance { get; set; } = 0;
    }
    /// <summary>
    /// Class representing SFTP settings.
    /// </summary>
    public class SftpSettings
    {
        public SftpSettings() { }
        public SftpSettings(int index)
        {
            Id = index;
        }
        public int Id { get; set; } = 1;
        public string Host { get; set; } = "sftp.example.com";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "username";
        public string Password { get; set; } = "password";
        public string SshKeyPath { get; set; } = "password";
        public string RemotePath { get; set; } = "password";
        public string Info { get; set; } = string.Empty;
    }

    public static class HashUtils
    {
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}   