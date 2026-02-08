namespace Global
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using static Global.EasyObject;

#if GLOBAL_SYS
    public
 #else
    internal
#endif
    static partial class Sys
    {
        public static string GetCwd()
        {
            return Directory.GetCurrentDirectory();
        }
        public static void SetCwd(string path)
        {
            Log($"Sys.SetCwd(): {path}");
            Directory.SetCurrentDirectory(path);
        }
        public static string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }
        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }
        public static string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path)!;
        }
        public static string GetBaseName(string path)
        {
            return Path.GetFileNameWithoutExtension(Path.GetFileName(path));
        }
        public static Assembly? AssemblyForTypeName(string typeName)
        {
            Type? type = Type.GetType(typeName);
            if (type == null) return null;
            return type.Assembly;
        }
        public static object? CallAssemblyStaticMethod(Assembly asm, string typeName, string methodName, params object[] args)
        {
            if (asm == null) return null;
            System.Type? type = asm.GetType(typeName, false);
            if (type == null) return null;
            System.Reflection.MethodInfo? method = type.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (method == null) return null;
            object? methdResult = method.Invoke(null, args);
            return methdResult;
        }
        public static int RunCommand(string exe, params string[] args)
        {
            string cmd = exe;
            for (int i = 0; i < args.Length; i++)
            {
                cmd += String.Format(" \"{0}\"", args[i]);
            }
            Echo(cmd, "RunCommand");
            return _wsystem(cmd);
        }
        public static string GetProcessStdout(Encoding encoding, string exe, params string[] args)
        {
            string cmdArgs = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0) cmdArgs += " ";
                cmdArgs += String.Format("\"{0}\"", args[i]);
            }
            StringBuilder outputBuilder;
            ProcessStartInfo processStartInfo;
            Process process;
            outputBuilder = new StringBuilder();
            processStartInfo = new ProcessStartInfo();
            processStartInfo.StandardOutputEncoding = encoding;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = exe;
            processStartInfo.Arguments = cmdArgs;
            process = new Process();
            process.StartInfo = processStartInfo;
            // enable raising events because Process does not raise events by default
            process.EnableRaisingEvents = true;
            // attach the event handler for OutputDataReceived before starting the process
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate (object sender, DataReceivedEventArgs e)
                {
                    Console.Error.WriteLine(e.Data);
                    // append the new data to the data already read-in
                    outputBuilder.Append(e.Data + "\n");
                }
            );
            // start the process
            // then begin asynchronously reading the output
            // then wait for the process to exit
            // then cancel asynchronously reading the output
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            // use the output
            string output = outputBuilder.ToString();
            output = output.Trim() + "\n";
            return output;
        }
        public static bool CheckFixedArguments(string programName, int n, string[] args)
        {
            if (args.Length == n) return true;
            string msg = String.Format("{0} requires {1} argument(s); but {2} argument(s) specified", programName, n, args.Length);
            Log(args, msg);
            return false;
        }
        public static List<string> TextToLines(string text)
        {
            List<string> lines = new List<string>();
            using (StringReader sr = new StringReader(text))
            {
                string? line;
                while ((line = sr.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
        public static bool DebugFlag = false;
        public static string? FindExePath(string exe)
        {
            string cwd = "";
            return FindExePath(exe, cwd);
        }
        public static string? FindExePath(string exe, string cwd)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (Path.IsPathRooted(exe))
            {
                if (!File.Exists(exe)) return null;
                return Path.GetFullPath(exe);
            }
            var PATH = Environment.GetEnvironmentVariable("PATH") ?? "";
            PATH = $"{cwd};{PATH}";
            foreach (string test in PATH.Split(';'))
            {
                string path = test.Trim();
#if false
                if (!String.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, "dotnet4", exe)))
                    return Path.GetFullPath(Path.Combine(path, "dotnet4", exe));
#endif
                if (!String.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, exe)))
                    return Path.GetFullPath(Path.Combine(path, exe));
                string baseName = Path.GetFileNameWithoutExtension(exe);
                //baseName += ".bin";
                if (!String.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, $"{baseName}.bin", exe)))
                    return Path.GetFullPath(Path.Combine(path, $"{baseName}.bin", exe));
            }
            return null;
        }
        public static string? FindExePath(string exe, Assembly assembly)
        {
            int bit = IntPtr.Size * 8;
            string cwd = AssemblyDirectory(assembly);
            string? result = FindExePath(exe, cwd);
            if (result == null)
            {
                result = FindExePath(exe, $"{cwd}\\{bit}bit");
                if (result == null)
                {
                    cwd = Path.Combine(cwd, "assets");
                    result = FindExePath(exe, $"{cwd}\\{bit}bit");
                }
            }
            return result;
        }
        public static string AssemblyDirectory(Assembly assembly)
        {
#pragma warning disable SYSLIB0012
            string codeBase = assembly.CodeBase!;
#pragma warning restore SYSLIB0012
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path)!;
        }
        public static string GuidString()
        {
            return Guid.NewGuid().ToString("D");
        }
        public static uint GetACP()
        {
            return NativeMethods.GetACP();
        }
        public static uint SessionId()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return NativeMethods.WTSGetActiveConsoleSessionId();
            }
            return 0;
        }
        public static string RandomString(Random r, string[] chars, int length)
        {
            if (chars.Length == 0 || length < 0)
            {
                throw new ArgumentException();
            }
            var sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                int idx = r.Next(0, chars.Length);
                sb.Append(chars[idx]);
            }
            return sb.ToString();
        }
        public static string[] ExpandWildcard(string path)
        {
            string dir = Path.GetDirectoryName(path)!;
            if (string.IsNullOrEmpty(dir)) dir = ".";
            string fname = Path.GetFileName(path);
            string[] files = Directory.GetFiles(dir, fname);
            List<string> result = new List<string>();
            for (int i = 0; i < files.Length; i++)
            {
                result.Add(Path.GetFullPath(files[i]));
            }
            return result.ToArray();
        }
        public static string[] ExpandWildcardList(params string[] pathList)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < pathList.Length; i++)
            {
                string[] files = ExpandWildcard(pathList[i]);
                result.AddRange(files.ToList());
            }
            return result.ToArray();
        }
        public static string GetStringFromUrl(string url)
        {
#pragma warning disable SYSLIB0014
            HttpWebRequest? request = WebRequest.Create(url) as HttpWebRequest;
#pragma warning restore SYSLIB0014
            HttpWebResponse response = (HttpWebResponse)request!.GetResponse();
            WebHeaderCollection header = response.Headers;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
        public static List<string> GetMacAddressList()
        {
            var list = NetworkInterface
                       .GetAllNetworkInterfaces()
                       .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                       .Select(nic => String.Join("-", SplitStringByLengthList(nic.GetPhysicalAddress().ToString().ToLower(), 2)))
                       .ToList();
            return list;
        }
        public static IEnumerable<string> SplitStringByLengthLazy(string str, int maxLength)
        {
            for (int index = 0; index < str.Length; index += maxLength)
            {
                yield return str.Substring(index, System.Math.Min(maxLength, str.Length - index));
            }
        }
        public static List<string> SplitStringByLengthList(string str, int maxLength)
        {
            return SplitStringByLengthLazy(str, maxLength).ToList();
        }
        public static byte[] ReadFileHeadBytes(string path, int maxSize)
        {
            System.IO.FileStream fs = new System.IO.FileStream(
                path,
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            byte[] array = new byte[maxSize];
            int size = fs.Read(array, 0, array.Length);
            fs.Close();
            byte[] result = new byte[size];
            Array.Copy(array, 0, result, 0, result.Length);
            return result;
        }
        public static bool IsBinaryFile(string path)
        {
            byte[] head = ReadFileHeadBytes(path, 8000);
            for (int i = 0; i < head.Length; i++)
            {
                if (head[i] == 0) return true;
            }
            return false;
        }
        public static bool LaunchProcess(string exePath, string[] args, Dictionary<string, string>? vars = null)
        {
            string argList = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) argList += " ";
                if (args[i].Contains(" "))
                    argList += $"\"{args[i]}\"";
                else
                    argList += args[i];
            }
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = argList;
            if (vars != null)
            {
                foreach (string key in vars.Keys)
                {
                    process.StartInfo.EnvironmentVariables[key] = vars[key];
                }
            }
            bool result = process.Start();
            return result;
        }
        public static void FreeHGlobal(IntPtr x)
        {
            Marshal.FreeHGlobal(x);
        }
        public static IntPtr StringToWideAddr(string s)
        {
            return Marshal.StringToHGlobalUni(s);
        }
        public static string WideAddrToString(IntPtr s)
        {
            return Marshal.PtrToStringUni(s)!;
        }
        public static IntPtr StringToUTF8Addr(string s)
        {
            int len = Encoding.UTF8.GetByteCount(s);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }
        public static string UTF8AddrToString(IntPtr s)
        {
            int len = 0;
            while (Marshal.ReadByte(s, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(s, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
        public static IntPtr ReassignThreadLocalStringPointer(ThreadLocal<IntPtr> ptr, string s)
        {
            if (ptr.Value != IntPtr.Zero)
            {
                Sys.FreeHGlobal(ptr.Value);
                ptr.Value = IntPtr.Zero;
            }
            ptr.Value = Sys.StringToUTF8Addr(s);
            return ptr.Value;
        }
        public static string DateTimeString(DateTime x)
        {
            return x.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
        }
        public static int RunToConsole(string exePath, string[] args, Dictionary<string, string>? vars = null)
        {
            string argList = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) argList += " ";
                if (args[i].Contains(" "))
                    argList += $"\"{args[i]}\"";
                else
                    argList += args[i];
            }
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = argList;
            if (vars != null)
            {
                var keys = vars.Keys;
                foreach (var key in keys)
                {
                    process.StartInfo.EnvironmentVariables[key] = vars[key];
                }
            }
            process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { Console.Error.WriteLine(e.Data); };
            process.Start();
            Console.CancelKeyPress += (delegate (object sender, ConsoleCancelEventArgs e) { process.Kill(); })!;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();
            return process.ExitCode;
        }
        public static void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
        public static string AssemblyName(Assembly assembly)
        {
            return System.Reflection.AssemblyName.GetAssemblyName(assembly.Location).Name!;
        }
        public static int FreeTcpPort()
        {
            // https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        public static string FullName(dynamic x)
        {
            if (x is null) return "null";
            string fullName = ((object)x).GetType().FullName!;
            return fullName.Split('`')[0];
        }
        //public static string ToJson(object x, bool indent = false, bool display = false)
        public static string[] ResourceNames(Assembly assembly)
        {
            return assembly.GetManifestResourceNames();
        }
        public static Stream? ResourceAsStream(Assembly assembly, string name)
        {
            string resourceName = name.Contains(":") ? name.Replace(":", ".") : $"{AssemblyName(assembly)}.{name}";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            return stream;
        }
        public static string? StreamAsText(Stream? stream)
        {
            if (stream is null) return null; // "";
            long pos = stream.Position;
            var streamReader = new StreamReader(stream);
            var text = streamReader.ReadToEnd();
            stream.Position = pos;
            return text;
        }
        public static string? ResourceAsText(Assembly assembly, string name)
        {
            string resourceName = name.Contains(":") ? name.Replace(":", ".") : $"{AssemblyName(assembly)}.{name}";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            return StreamAsText(stream);
        }
        public static byte[]? StreamAsBytes(Stream? stream)
        {
            if (stream is null) return null;
            long pos = stream.Position;
            byte[] bytes = new byte[(int)stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            stream.Position = pos;
            return bytes;
        }
        public static byte[]? ResourceAsBytes(Assembly assembly, string name)
        {
            string resourceName = name.Contains(":") ? name.Replace(":", ".") : $"{AssemblyName(assembly)}.{name}";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            return StreamAsBytes(stream);
        }
        public static EasyObject? StreamAsJson(Stream stream)
        {
            string? json = StreamAsText(stream);
            return EasyObject.FromJson(json);
        }
        public static EasyObject ResourceAsEasyObject(Assembly assembly, string name)
        {
            string? json = ResourceAsText(assembly, name);
            return EasyObject.FromJson(json)!;
        }
        public static byte[]? ToUtf8Bytes(string s)
        {
            if (s is null) return null;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            return bytes;
        }
        public static void Prepare(string dirPath)
        {
            Directory.CreateDirectory(dirPath);
        }
        public static void PrepareForFile(string filePath)
        {
            Prepare(Path.GetDirectoryName(filePath)!);
        }
        public static void DownloadBinaryFromUrl(string url, string destinationPath)
        {
            PrepareForFile(destinationPath);
#pragma warning disable SYSLIB0014
            WebRequest objRequest = System.Net.HttpWebRequest.Create(url);
#pragma warning restore SYSLIB0014
            var objResponse = objRequest.GetResponse();
            byte[] buffer = new byte[32768];
            using (Stream input = objResponse.GetResponseStream())
            {
                using (FileStream output = new FileStream(destinationPath, FileMode.CreateNew))
                {
                    int bytesRead;
                    while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
        [DllImport("msvcrt", CharSet = CharSet.Unicode)]
        internal static extern int _wsystem(string lpCommandLine);
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibraryW(string lpFileName);
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryExW(string dllToLoad, IntPtr hFile, LoadLibraryFlags flags);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = false)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        [System.Flags]
        public enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008,
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
            LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000
        }
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll")]
            internal static extern uint WTSGetActiveConsoleSessionId();
            [DllImport("kernel32.dll")]
            internal static extern uint GetACP();
        }
    }
}
