namespace FolderOrganizer.BackEnd
{
    class ConfigKeys
    {
        internal struct SystemRequirements
        {
            public const string MinPythonVersion = "MinPythonVersion";
            public const string TerminationTimeout = "TerminationTimeout";
            public const string TerminationTimeoutAttempts = "TerminationTimeoutAttempts";
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
