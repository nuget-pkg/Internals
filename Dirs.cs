namespace Global
{
    using System;
    using System.IO;
#if GLOBAL_SYS
    public
 #else
    internal
#endif
   static class Dirs
    {
        public static string ProfilePath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string ProfilePath(string appName)
        {
            string baseFolder = ProfilePath();
            return $"{baseFolder}/{appName}".Replace('/', Path.DirectorySeparatorChar);
        }

        public static string ProfilePath(string orgName, string appName)
        {
            string baseFolder = ProfilePath();
            return $"{baseFolder}/{appName}".Replace('/', Path.DirectorySeparatorChar);
        }

        public static string DocumentsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string DocumentsPath(string name)
        {
            return (DocumentsPath() + @"/" + name).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string SpecialFolderPath(Environment.SpecialFolder folder)
        {
            return System.Environment.GetFolderPath(folder).Replace('/', Path.DirectorySeparatorChar);
        }

        public static string AppDataFolderPath(string orgName, string appName)
        {
            string baseFolder = SpecialFolderPath(Environment.SpecialFolder.ApplicationData);
            return $"{baseFolder}/{orgName}/{appName}".Replace('/', Path.DirectorySeparatorChar);
        }

        public static string AppDataFolderPath(string appName)
        {
            string baseFolder = SpecialFolderPath(Environment.SpecialFolder.ApplicationData);
            return $"{baseFolder}/{appName}".Replace('/', Path.DirectorySeparatorChar);
        }
    }

}
