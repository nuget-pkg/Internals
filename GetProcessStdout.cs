namespace Global
{
    using System;
    using System.Diagnostics;
    using System.Text;
#if GLOBAL_SYS
    public
 #else
    internal
 #endif
    static partial class Sys
    {
        public static string GetProcessStdout(Encoding encoding, bool debug, string exe, params string[] args)
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
                    if (debug)
                    {
                        Console.Error.WriteLine(e.Data);
                    }
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
    }
}
