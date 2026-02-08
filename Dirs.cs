namespace Global
{
    using System;
#if GLOBAL_SYS
    public
 #else
    internal
#endif
   static class Dirs
    {
        public static string ProfilePath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace(@"\", "/");
        }

        public static string ProfilePath(string appName)
        {
            string baseFolder = ProfilePath();
            return $"{baseFolder}/{appName}".Replace(@"\", "/");
        }

        public static string ProfilePath(string orgName, string appName)
        {
            string baseFolder = ProfilePath();
            return $"{baseFolder}/{appName}".Replace(@"\", "/");
        }

        public static string DocumentsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).Replace(@"\", "/");
        }

        public static string DocumentsPath(string name)
        {
            return (DocumentsPath() + @"/" + name).Replace(@"\", "/");
        }

        public static string SpecialFolderPath(Environment.SpecialFolder folder)
        {
            return System.Environment.GetFolderPath(folder).Replace(@"\", "/");
        }

        public static string AppDataFolderPath(string orgName, string appName)
        {
            string baseFolder = SpecialFolderPath(Environment.SpecialFolder.ApplicationData);
            return $"{baseFolder}/{orgName}/{appName}".Replace(@"\", "/");
        }

        public static string AppDataFolderPath(string appName)
        {
            string baseFolder = SpecialFolderPath(Environment.SpecialFolder.ApplicationData);
            return $"{baseFolder}/{appName}".Replace(@"\", "/");
        }
    }

}
