namespace Global
{
    using System;
    using System.IO.Compression;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Reflection;
    using System.Text;
#if GLOBAL_SYS
    public
 #else
    internal
#endif
    class Installer
    {
        public static string InstallZipFromURL(string url, string targetDir, string baseName)
        {
            string guid = Sys.GuidString();
            string zipPath = Path.Combine(targetDir, baseName + ".zip");
            SafeDownload(url, zipPath);
            string installDir = Path.Combine(targetDir, baseName);
            SafeZipExtract(zipPath, installDir);
            return installDir;
        }
        public static string? InstallResourceDll(Assembly assembly, string targetDir, string name)
        {
            string guid = Sys.GuidString();
            var dllBytes = Sys.ResourceAsBytes(assembly, name);
            if (dllBytes == null) return null;
#pragma warning disable SYSLIB0021
            SHA256 crypto = new SHA256CryptoServiceProvider();
#pragma warning restore SYSLIB0021
            byte[] hashValue = crypto.ComputeHash(dllBytes);
            string sha256 = String.Join("", hashValue.Select(x => x.ToString("x2")).ToArray());
            string dllName = $"{Path.GetFileNameWithoutExtension(name.Replace(":", "-"))}-{sha256}.dll";
            var dllPath = Path.Combine(targetDir, dllName);
            SafeFileWrite(dllPath, dllBytes);
            return dllPath;
        }
        public static string? InstallResourceZip(Assembly assembly, string targetDir, string name)
        {
            string guid = Sys.GuidString();
            var zipBytes = Sys.ResourceAsBytes(assembly, name);
            if (zipBytes == null) return null;
#pragma warning disable SYSLIB0021
            SHA256 crypto = new SHA256CryptoServiceProvider();
#pragma warning restore SYSLIB0021
            byte[] hashValue = crypto.ComputeHash(zipBytes);
            string sha256 = String.Join("", hashValue.Select(x => x.ToString("x2")).ToArray());
            string zipName = $"{Path.GetFileNameWithoutExtension(name.Replace(":", "-"))}-{sha256}";
            var extractPath = Path.Combine(targetDir, zipName);
            string zipPath = Path.Combine(targetDir, $"{zipName}.zip");
            SafeFileWrite(zipPath, zipBytes);
            SafeZipExtract(zipPath, extractPath);
            return extractPath;
        }
        public static void SafeDownload(string url, string filePath)
        {
            if (File.Exists(filePath)) return;
            string guid = Sys.GuidString();
            EasyObject.Log($"Donloading to {filePath}...");
            Sys.DownloadBinaryFromUrl(url, $"{filePath}.{guid}");
            try
            {
                File.Move($"{filePath}.{guid}", filePath);
            }
            catch (Exception)
            {
                ;
            }
        }
        public static void SafeFileWrite(string filePath, byte[] contents)
        {
            if (File.Exists(filePath)) return;
            //Console.Error.WriteLine($"[Log] Writing to {filePath}...");
            string guid = Sys.GuidString();
            Sys.PrepareForFile(filePath);
            File.WriteAllBytes($"{filePath}.{guid}", contents);
            try
            {
                File.Move($"{filePath}.{guid}", filePath);
            }
            catch (Exception)
            {
                ;
            }
        }
        public static void SafeFileWrite(string filePath, string text)
        {
            SafeFileWrite(filePath, Encoding.UTF8.GetBytes(text));
        }
        public static void SafeZipExtract(string zipPath, string dirPath)
        {
            if (Directory.Exists(dirPath)) return;
            EasyObject.Log($"Extracting to {dirPath}...");
            string guid = Sys.GuidString();
            ZipFile.ExtractToDirectory(zipPath, $"{dirPath}.{guid}");
            try
            {
                Directory.Move($"{dirPath}.{guid}", dirPath);
            }
            catch (Exception)
            {
                ;
            }
        }
    }
}
