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
using System.Text;

namespace Global {
#if GLOBAL_SYS
    public static partial class Sys {
#else
    internal static partial class EasySystem {
#endif
        public static void SetupConsoleUTF8() {
            Global.EasyObject.SetupConsoleEncoding(Encoding.UTF8);
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
        public static bool CheckFixedArguments(string programName, int n, string[] args) {
            if (args.Length == n) {
                return true;
            }
            string msg = string.Format("{0} requires {1} argument(s); but {2} argument(s) specified", programName, n, args.Length);
            return false;
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
                }
                else {
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
        public static void WriteTextFileUtf8(string fileName, string content) {
            fileName = CygpathWindows(fileName);
            using StreamWriter sw = new StreamWriter(fileName, false, Encoding.GetEncoding("UTF-8"));
            sw.Write(content.Replace("\r\n", "\n"));
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
        public static void WorkAroundTlsSecurity() {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
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
