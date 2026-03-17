using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static Global.EasyObject;

namespace Global {
#if GLOBAL_SYS
    public
#else
    internal
#endif
    static partial class Sys {
        public static bool SilentFlag = false;
        public static bool SysDebugFlag = false;
        private static void DebugLog(object? x, string? title = null, bool noIndent = false, uint maxDepth = 0u, List<string>? hideKeys = null, bool removeSurrogatePair = false) {
            {
                if (SysDebugFlag) {
                    EasyObject.Log(x, title, noIndent, maxDepth, hideKeys, removeSurrogatePair);
                }
            }
        }
        public static void SetupConsoleUTF8() {
            Global.EasyObject.SetupConsoleEncoding(Encoding.UTF8);
        }
        public static bool IsWindowsPlatform() {
#if NETFRAMEWORK
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
#else
            return (OperatingSystem.IsWindows());
#endif
        }
        public static void Exit(int exitCoed) {
            Console.Error.Write($"Global.Sys.Exit() was called with exitCode: {exitCoed}." + "\n");
            Environment.Exit(exitCoed);
        }
        public static string GetCwd() {
            return Directory.GetCurrentDirectory();
        }
        public static void SetCwd(string path) {
            path = CygpathWindows(path);
            if (!SilentFlag) {
                System.Console.Error.WriteLine($"Sys.SetCwd(): {path}");
            }

            Prepare(path);
            Directory.SetCurrentDirectory(path);
        }
        public static string GetFullPath(string path) {
            path = CygpathWindows(path);
            return Path.GetFullPath(path);
        }
        public static string GetFileName(string path) {
            path = CygpathWindows(path);
            return Path.GetFileName(path);
        }
        public static string GetDirectoryName(string path) {
            path = CygpathWindows(path);
            return Path.GetDirectoryName(path)!;
        }
        public static string GetBaseName(string path) {
            path = CygpathWindows(path);
            return Path.GetFileNameWithoutExtension(Path.GetFileName(path));
        }
        public static Assembly? AssemblyForTypeName(string typeName) {
            Type? type = Type.GetType(typeName);
            if (type == null) {
                return null;
            }

            return type.Assembly;
        }
        public static object? CallAssemblyStaticMethod(Assembly asm, string typeName, string methodName, params object[] args) {
            if (asm == null) {
                return null;
            }

            System.Type? type = asm.GetType(typeName, false);
            if (type == null) {
                return null;
            }

            System.Reflection.MethodInfo? method = type.GetMethod(methodName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (method == null) {
                return null;
            }

            object? methdResult = method.Invoke(null, args);
            return methdResult;
        }
        public static int RunCommand(string exe, params string[] args) {
            string cmd = exe;
            for (int i = 0; i < args.Length; i++) {
                cmd += string.Format(" \"{0}\"", args[i]);
            }
            if (!SilentFlag) {
                System.Console.Error.WriteLine($"RunCommand: {cmd}");
            }

            return _wsystem(cmd);
        }
        public static string GetProcessStdout(Encoding encoding, string exe, params string[] args) {
            string cmdArgs = "";
            for (int i = 0; i < args.Length; i++) {
                if (i != 0) {
                    cmdArgs += " ";
                }

                cmdArgs += string.Format("\"{0}\"", args[i]);
            }
            StringBuilder outputBuilder;
            ProcessStartInfo processStartInfo;
            Process process;
            outputBuilder = new StringBuilder();
            processStartInfo = new ProcessStartInfo()!;
            processStartInfo.StandardOutputEncoding = encoding;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardInput = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = exe;
            processStartInfo.Arguments = cmdArgs;
            process = new Process {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate (object sender, DataReceivedEventArgs e) {
                    if (!SilentFlag) {
                        Console.Error.WriteLine(e.Data);
                    }

                    outputBuilder.Append(e.Data + "\n");
                }
            );
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            string output = outputBuilder.ToString();
            output = output.Trim() + "\n";
            return output;
        }
        public static bool CheckFixedArguments(string programName, int n, string[] args) {
            if (args.Length == n) {
                return true;
            }
            string msg = string.Format("{0} requires {1} argument(s); but {2} argument(s) specified", programName, n, args.Length);
            return false;
        }
        public static List<string> TextToLines(string text) {
            List<string> lines = [];
            using (StringReader sr = new StringReader(text)) {
                string? line;
                while ((line = sr.ReadLine()) != null) {
                    lines.Add(line);
                }
            }
            return lines;
        }
        public static string? FindExePath(string exe) {
            string cwd = "";
            return FindExePath(exe, cwd);
        }
        public static string? FindExePath(string exe, string cwd) {
            cwd = CygpathWindows(cwd);
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (Path.IsPathRooted(exe)) {
                if (!File.Exists(exe)) {
                    return null;
                }

                return Path.GetFullPath(exe);
            }
            string PATH = Environment.GetEnvironmentVariable("PATH") ?? "";
            PATH = $"{cwd};{PATH}";
            foreach (string test in PATH.Split(';')) {
                string path = test.Trim();
                if (!string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, exe))) {
                    return Path.GetFullPath(Path.Combine(path, exe));
                }

                string baseName = Path.GetFileNameWithoutExtension(exe);
                if (!string.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, $"{baseName}.bin", exe))) {
                    return Path.GetFullPath(Path.Combine(path, $"{baseName}.bin", exe));
                }
            }
            return null;
        }
        public static string? FindExePath(string exe, Assembly assembly) {
            int bit = IntPtr.Size * 8;
            string cwd = AssemblyDirectory(assembly);
            string? result = FindExePath(exe, cwd);
            if (result == null) {
                result = FindExePath(exe, $"{cwd}\\{bit}bit");
                if (result == null) {
                    cwd = Path.Combine(cwd, "assets");
                    result = FindExePath(exe, $"{cwd}\\{bit}bit");
                }
            }
            return result;
        }
        public static string AssemblyDirectory(Assembly assembly) {
#pragma warning disable SYSLIB0012
            string codeBase = assembly.CodeBase!;
#pragma warning restore SYSLIB0012
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path)!;
        }
        public static string GuidString() {
            return Guid.NewGuid().ToString("D");
        }
        public static uint GetACP() {
            return NativeMethods.GetACP();
        }
        public static uint SessionId() {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                return NativeMethods.WTSGetActiveConsoleSessionId();
            }
            return 0;
        }
        public static string RandomString(Random r, string[] chars, int length) {
            if (chars.Length == 0 || length < 0) {
                throw new ArgumentException();
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++) {
                int idx = r.Next(0, chars.Length);
                sb.Append(chars[idx]);
            }
            return sb.ToString();
        }
        public static string CygpathWindows(string path) {
            path = path.Replace(@"\", "/");
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
                return path;
            }

            List<string>? m = FindFirstMatch(
                path,
                "^/([a-zA-z])[/]?$",
                "^/([a-zA-z])/(.+)$",
                "^/mnt/([a-zA-z])[/]?$",
                "^/mnt/([a-zA-z])/(.+)$"
                );
            if (m != null) {
                if (m.Count == 2) {
                    path = $"{m[1].ToUpper()}:/";
                } else if (m.Count == 3) {
                    path = $"{m[1].ToUpper()}:/{m[2]}";
                }
            }
            path = path.Replace('/', Path.DirectorySeparatorChar);
            return path;
        }
        public static string[] ExpandWildcard(string path) {
            path = CygpathWindows(path);
            string dir = Path.GetDirectoryName(path)!;
            if (string.IsNullOrEmpty(dir)) {
                dir = ".";
            }

            string fname = Path.GetFileName(path);
            string[] files = Directory.GetFileSystemEntries(dir, fname);
            List<string> result = [];
            for (int i = 0; i < files.Length; i++) {
                result.Add(Path.GetFullPath(files[i]));
            }
            return result.ToArray();
        }
        public static string[] ExpandWildcardList(params string[] pathList) {
            pathList = (string[])pathList.Clone();
            for (int i = 0; i < pathList.Length; i++) {
                pathList[i] = CygpathWindows(pathList[i]);
            }
            List<string> result = [];
            for (int i = 0; i < pathList.Length; i++) {
                string[] files = ExpandWildcard(pathList[i]);
                result.AddRange(files.ToList());
            }
            return result.ToArray();
        }
        public static string GetStringFromUrl(string url) {
#pragma warning disable SYSLIB0014
            HttpWebRequest? request = WebRequest.Create(url) as HttpWebRequest;
#pragma warning restore SYSLIB0014
            HttpWebResponse response = (HttpWebResponse)request!.GetResponse();
            WebHeaderCollection header = response.Headers;
            using StreamReader reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8);
            return reader.ReadToEnd();
        }
        public static List<string> GetMacAddressList() {
            List<string> list = NetworkInterface
                       .GetAllNetworkInterfaces()
                       .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                       .Select(nic => string.Join("-", SplitStringByLengthList(nic.GetPhysicalAddress().ToString().ToLower(), 2)))
                       .ToList();
            return list;
        }
        public static IEnumerable<string> SplitStringByLengthLazy(string str, int maxLength) {
            for (int index = 0; index < str.Length; index += maxLength) {
                yield return str.Substring(index, System.Math.Min(maxLength, str.Length - index));
            }
        }
        public static List<string> SplitStringByLengthList(string str, int maxLength) {
            return SplitStringByLengthLazy(str, maxLength).ToList();
        }
        public static byte[] ReadFileHeadBytes(string path, int maxSize) {
            path = CygpathWindows(path);
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
        public static bool IsBinaryFile(string path) {
            byte[] head = ReadFileHeadBytes(path, 8000);
            for (int i = 0; i < head.Length; i++) {
                if (head[i] == 0) {
                    return true;
                }
            }
            return false;
        }
        public static bool LaunchProcess(string exePath, string[] args, Dictionary<string, string>? vars = null) {
            exePath = CygpathWindows(exePath);
            string argList = "";
            for (int i = 0; i < args.Length; i++) {
                if (i > 0) {
                    argList += " ";
                }

                if (args[i].Contains(" ")) {
                    argList += $"\"{args[i]}\"";
                } else {
                    argList += args[i];
                }
            }
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = argList;
            if (vars != null) {
                foreach (string key in vars.Keys) {
                    process.StartInfo.EnvironmentVariables[key] = vars[key];
                }
            }
            bool result = process.Start();
            return result;
        }
        public static void FreeHGlobal(IntPtr x) {
            Marshal.FreeHGlobal(x);
        }
        public static IntPtr StringToWideAddr(string s) {
            return Marshal.StringToHGlobalUni(s);
        }
        public static string WideAddrToString(IntPtr s) {
            return Marshal.PtrToStringUni(s)!;
        }
        public static IntPtr StringToUTF8Addr(string s) {
            int len = Encoding.UTF8.GetByteCount(s);
            byte[] buffer = new byte[len + 1];
            Encoding.UTF8.GetBytes(s, 0, s.Length, buffer, 0);
            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);
            return nativeUtf8;
        }
        public static string UTF8AddrToString(IntPtr s) {
            int len = 0;
            while (Marshal.ReadByte(s, len) != 0) {
                ++len;
            }

            byte[] buffer = new byte[len];
            Marshal.Copy(s, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }
        public static IntPtr ReassignThreadLocalStringPointer(ThreadLocal<IntPtr> ptr, string s) {
            if (ptr.Value != IntPtr.Zero) {
                Sys.FreeHGlobal(ptr.Value);
                ptr.Value = IntPtr.Zero;
            }
            ptr.Value = Sys.StringToUTF8Addr(s);
            return ptr.Value;
        }
        public static string DateTimeString(DateTime x) {
            return x.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz");
        }
        public static string DateTimeStringSafe(DateTime x) {
            // result string can be used as part of file name/path.
            return x.ToString("yyyy-MM-ddTHH-mm-ss.fffffffzzz")
                .Replace("T", "+")
                .Replace(":", "")
                ;
        }
        public static string DateString(DateTime x) {
            return x.ToString("yyyy-MM-dd");
        }
        public static string DateStringCompact(DateTime x) {
            return x.ToString("yyyyMMdd");
        }
        public static int RunToConsole(string exePath, string[] args, Dictionary<string, string>? vars = null) {
            exePath = CygpathWindows(exePath);
            string argList = "";
            for (int i = 0; i < args.Length; i++) {
                if (i > 0) {
                    argList += " ";
                }

                if (args[i].Contains(" ")) {
                    argList += $"\"{args[i]}\"";
                } else {
                    argList += args[i];
                }
            }
            Process process = new Process();
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = exePath;
            process.StartInfo.Arguments = argList;
            if (vars != null) {
                Dictionary<string, string>.KeyCollection keys = vars.Keys;
                foreach (string key in keys) {
                    process.StartInfo.EnvironmentVariables[key] = vars[key];
                }
            }
            process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { Console.Error.WriteLine(e.Data); };
            process.Start();
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) { process.Kill(); }!;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();
            return process.ExitCode;
        }
        public static void Sleep(int milliseconds) {
            Thread.Sleep(milliseconds);
        }
        public static string AssemblyName(Assembly assembly) {
            return System.Reflection.AssemblyName.GetAssemblyName(assembly.Location).Name!;
        }
        public static int FreeTcpPort() {
            // https://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }
        public static string FullName(dynamic x) {
            if (x is null) {
                return "null";
            }
            string fullName = ((object)x).GetType().FullName!;
            return fullName.Split('`')[0];
        }
        public static string[] ResourceNames(Assembly assembly) {
            return assembly.GetManifestResourceNames();
        }
        public static Stream? ResourceAsStream(Assembly assembly, string resName) {
            string resourceName = resName.Contains(":") ? resName.Replace(":", ".") : $"{AssemblyName(assembly)}.{resName}";
            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) {
                Console.Error.WriteLine($"Resoucde '{resourceName}' not found!");
                Console.Error.WriteLine($"Available resouce names are: ");
                string[] names = ResourceNames(assembly);
                foreach (string name in names) {
                    Console.Error.WriteLine($"  {name}");
                }
                Environment.Exit(1);
            }
            return stream;
        }
        public static string? StreamAsText(Stream? stream) {
            if (stream is null) {
                return null;
            }

            long pos = stream.Position;
            StreamReader streamReader = new StreamReader(stream);
            string text = streamReader.ReadToEnd();
            text = text.Replace("\r\n", "\n");
            stream.Position = pos;
            return text;
        }
        public static string? ResourceAsText(Assembly assembly, string resName) {
            Stream? stream = ResourceAsStream(assembly, resName);
            return StreamAsText(stream);
        }
        public static byte[]? StreamAsBytes(Stream? stream) {
            if (stream is null) {
                return null;
            }

            long pos = stream.Position;
            byte[] bytes = new byte[(int)stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);
            stream.Position = pos;
            return bytes;
        }
        public static byte[]? ResourceAsBytes(Assembly assembly, string resName) {
            Stream? stream = ResourceAsStream(assembly, resName);
            return StreamAsBytes(stream);
        }
        public static Assembly? LoadFromResource(Assembly assembly, string name) {
            byte[]? bytes = ResourceAsBytes(assembly, name);
            if (bytes == null) {
                return null;
            }

            return Assembly.Load(bytes);
        }
        public static dynamic CreateInstanceFromResource(Assembly thisAssemby, string resName, string className) {
            Assembly? assembly = LoadFromResource(thisAssemby, resName);
            if (assembly == null) {
                Console.Error.WriteLine($"Failed to load assembly from resouce '{resName}'");
                Environment.Exit(1);
            }
            Type? classType = assembly!.GetType(className);
            if (classType == null) {
                Console.Error.WriteLine($"Failed to find class '{className}' from resouce '{resName}'");
                Environment.Exit(1);
            }
            return Activator.CreateInstance(classType!)!;
        }
        public static byte[]? ToUtf8Bytes(string s) {
            if (s is null) {
                return null;
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
            return bytes;
        }
        public static void Prepare(string dirPath) {
            dirPath = CygpathWindows(dirPath);
            Directory.CreateDirectory(dirPath);
        }
        public static void PrepareForFile(string filePath) {
            filePath = CygpathWindows(filePath);
            Prepare(Path.GetDirectoryName(filePath)!);
        }
        public static void DownloadBinaryFromUrl(string url, string destinationPath) {
            destinationPath = CygpathWindows(destinationPath);
            PrepareForFile(destinationPath);
#pragma warning disable SYSLIB0014
            WebRequest objRequest = System.Net.HttpWebRequest.Create(url);
#pragma warning restore SYSLIB0014
            WebResponse objResponse = objRequest.GetResponse();
            byte[] buffer = new byte[32768];
            using Stream input = objResponse.GetResponseStream();
            using FileStream output = new FileStream(destinationPath, FileMode.CreateNew);
            int bytesRead;
            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) {
                output.Write(buffer, 0, bytesRead);
            }
        }
        public static List<string>? FindFirstMatch(string s, params string[] patterns) {
            foreach (string pattern in patterns) {
                Regex r = new Regex(pattern);
                Match m = r.Match(s);
                if (m.Success) {
                    List<string> groups = [];
                    for (int i = 0; i < m.Groups.Count; i++) {
                        groups.Add(m.Groups[i].Value);
                    }
                    return groups;
                }
            }
            return null;
        }
        public static Dictionary<string, string> QueryParameterDictionary(string url) {
            Uri uri = new Uri(url);
            string queryString = uri.Query;
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);
            Dictionary<string, string> dict = [];
            foreach (string? key in queryParameters.AllKeys) {
                dict[key!] = queryParameters[key!]!;
            }
            return dict;
        }
        public static string? FindQueryParameter(string url, string name) {
            Dictionary<string, string> dict = QueryParameterDictionary(url);
            if (dict.ContainsKey(name)) {
                return dict[name];
            }
            return null;
        }
        public static void WriteTextFileUtf8(string fileName, string content) {
            fileName = CygpathWindows(fileName);
            using StreamWriter sw = new StreamWriter(fileName, false, Encoding.GetEncoding("UTF-8"));
            sw.Write(content.Replace("\r\n", "\n"));
        }
        public static void DumpObjectAsJson(
            object? x,
            bool compact = false,
            string newline = "\n",
            bool keyAsSymbol = false,
            bool removeSurrogatePair = false) {
            string json = EasyObject.FromObject(x)
                .ToJson(
                indent: !compact,
                keyAsSymbol: keyAsSymbol,
                removeSurrogatePair: removeSurrogatePair
                );
            Console.Write(json + newline);
        }
        public static bool CanConvertAllToSjis(string text) {
            // Shift_JIS (または CP932) のエンコーディングを取得
            // "shift_jis" は純粋なJIS規格、"cp932" はWindows拡張を含むSJIS
            Encoding sjis = Encoding.GetEncoding("shift_jis");
            // 文字列をSJISのバイト配列に変換
            byte[] encodedBytes = sjis.GetBytes(text);
            // バイト配列を文字列に戻す
            string decodedText = sjis.GetString(encodedBytes);
            // 変換前と後で一致するか確認（不一致＝表せない文字がある）
            return text == decodedText;
        }
        public static string RemoveStringSuffix(string input, string suffix) {
            if (input.EndsWith(suffix)) {
                return input.Remove(input.Length - suffix.Length, suffix.Length);
            }
            return input;
        }
        public static void WorkAroundTlsSecurity() {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
        public static async Task<string> GetResponseString(
            string baseUrl,
            Dictionary<string, string>? queryParameters
            ) {
            if (queryParameters == null) {
                queryParameters = [];
            }
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(
                $"{baseUrl}?{await new FormUrlEncodedContent(queryParameters).ReadAsStringAsync()}"
                );
            string contents = await response.Content.ReadAsStringAsync();
            return contents;
        }
        public static string RemoveSurrogatePair(string str, string replaceSurrogate = "★") {
            DebugLog(replaceSurrogate, "replaceSurrogate(2)");
            DebugLog(str, "Original string");
            if (replaceSurrogate == "") {
                DebugLog("No surrogate pair replacement specified; returning original string.", "RemoveSurrogatePair");
                return str;
            }
            // https://teratail.com/questions/53520 絵文字の判別方法
            str = Regex.Replace(str, @"[\uD800-\uDFFF]", "{ddbea68e-d93f-4e85-92b5-83b1ace6d50f}");
            str = str.Replace("{ddbea68e-d93f-4e85-92b5-83b1ace6d50f}{ddbea68e-d93f-4e85-92b5-83b1ace6d50f}", replaceSurrogate);
            str = str.Replace("{ddbea68e-d93f-4e85-92b5-83b1ace6d50f}", replaceSurrogate);
            DebugLog(str, "String after surrogate pair replacement");
            return str;
        }
        public static string AdjustFileName(string fileName, string replaceSurrogate = "★") {
            fileName = fileName
                .Replace("'", "’")
                .Replace("\"", "”")
                .Replace(":", "：")
                .Replace("/", "／")
                .Replace("\\", "＼")
                .Replace("　", " ")
                .Replace("|", "￤")
                .Replace("#", "＃")
                .Replace("?", "？")
                .Replace("[", "⁅")
                .Replace("]", "⁆")
                .Replace("(", "｟")
                .Replace(")", "｠")
                ;
            DebugLog(replaceSurrogate, "replaceSurrogate(1)");
            fileName = RemoveSurrogatePair(fileName, replaceSurrogate);
            return fileName;
        }
        public static string AdjustMetaData(string metadata, string replaceSurrogate = "★") {
            metadata = metadata
                .Replace("'", "’")
                .Replace("\"", "”")
                .Replace("\\", "＼")
                ;
            metadata = RemoveSurrogatePair(metadata, replaceSurrogate);
            return metadata;
        }
        public static string GetEnv(string name, string fallback = "") {
            return Environment.GetEnvironmentVariable(name) ?? fallback;
        }
        public static void SetEnv(string name, string value) {
            Environment.SetEnvironmentVariable(name, value);
        }
        public static string HomeFile(params string[] relatives) {
            string home = GetEnv("HOME", "");
            if (home == "") {
                //Crash("Global.Sys.HomeFile(): $HOME not set");
                home = Dirs.ProfilePath();
            }
            string result = home;
            foreach (string x in relatives) {
                string relative = x;
                relative = AdjustFileName(relative);
                result = Path.Combine(result, relative);
            }
            PrepareForFile(result);
            return result;
        }
        public static string HomeFolder(params string[] relatives) {
            string home = GetEnv("HOME", "");
            if (home == "") {
                //Crash("Global.Sys.HomeFolder(): $HOME not set");
                home = Dirs.ProfilePath();
            }
            string result = home;
            foreach (string x in relatives) {
                string relative = x;
                relative = AdjustFileName(relative);
                result = Path.Combine(result, relative);
            }
            Prepare(result);
            return result;
        }
        public static void Crash(object? message = null, int exitCode = 1) {
            ShowDetail = false;
            DebugLog("[!! PROGRAM CRASHED !!]");
            if (message != null && !(message is Exception)) {
                DebugLog(message, "Message");
            }
            if (message is Exception e) {
                string trace = e.ToString();
                List<string> lines = TextToLines(trace);
                List<string> lines2 = lines.Select(x => $"      {x}").ToList();
                string trace2 = "\n" + string.Join("\n", lines2);
                DebugLog(trace2, "Stack Trace");
            } else {
                string trace = Environment.StackTrace;
                List<string> lines = TextToLines(trace);
                string trace2 = "\n" + string.Join("\n", lines);
                DebugLog(trace2, "Stack Trace");
            }
            DebugLog($"[!! ABORTING...WITH EXIT CODE {exitCode} !!]");
            Environment.Exit(exitCode);
        }
        public static Process? OpenUrl(string url) {
            ProcessStartInfo pi = new ProcessStartInfo() {
                FileName = url,
                UseShellExecute = true,
            };
            return Process.Start(pi);
        }
        public static string LimitStringLength(string s, int limit, string ellipsis = "...") {
            UTF32Encoding enc = new UTF32Encoding();
            byte[] byteUtf32 = enc.GetBytes(s);
            if (byteUtf32.Length <= limit * 4) {
                return s;
            }
            ArraySegment<byte> segment = new ArraySegment<byte>(byteUtf32, 0, limit * 4);
            byteUtf32 = segment.ToArray();
            string decodedString = enc.GetString(byteUtf32);
            return decodedString + ellipsis;
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
        public enum LoadLibraryFlags : uint {
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
        internal static class NativeMethods {
            [DllImport("kernel32.dll")]
            internal static extern uint WTSGetActiveConsoleSessionId();
            [DllImport("kernel32.dll")]
            internal static extern uint GetACP();
        }
    }
}
