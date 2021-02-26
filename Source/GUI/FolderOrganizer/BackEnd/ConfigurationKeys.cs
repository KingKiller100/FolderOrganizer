namespace FolderOrganizer.BackEnd
{
    class ConfigurationKeys
    {
        internal struct SystemRequirements
        {
            public const string MinPythonVersion = "MinPythonVersion";
        }

        internal struct RuntimeInfo
        {
            public const string ProcessID = "pid";
        }

        internal struct Paths
        {
            public const string SourcePath = "Source";
            public const string DestinationPath = "Destination";
        }
    }
}
