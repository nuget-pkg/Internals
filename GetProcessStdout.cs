using System;
using System.Diagnostics;
using System.Text;

namespace Global {
#if GLOBAL_SYS
    public
#else
    internal
#endif
    static partial class Sys {
        public static string GetProcessStdout(Encoding encoding, bool debug, string exe, params string[] args) {
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
            processStartInfo = new ProcessStartInfo {
                StandardOutputEncoding = encoding,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                FileName = exe,
                Arguments = cmdArgs
            };
            process = new Process {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate (object sender, DataReceivedEventArgs e) {
                    if (debug) {
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
    }
}
