namespace Global
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using Global;
    using static Global.EasyObject;
#if GLOBAL_SYS
    public
 #else
    internal
#endif
    class CscsUtil
    {
        public List<string> SrcList = new List<string> { };
        public List<string> PkgList = new List<string> { };
        public List<string> AsmList = new List<string> { };
        public List<string> ResList = new List<string> { };
        public List<string> DllList = new List<string> { };
        public void DebugDump()
        {
            Log(SrcList, "SrcList");
            Log(PkgList, "PkgList");
            Log(AsmList, "AsmList");
            Log(ResList, "ResList");
            Log(DllList, "DllList");
        }
        private string AdjustPath(string path)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                path = path.Replace(home + @"\", @"$(HOME)\");
            }
            return path;
        }
        public void ParseProject(string projFileName)
        {
            Log(projFileName, "CscsUtil.ParseProject()");
#if false
            SrcList.Clear();
            PkgList.Clear();
            AsmList.Clear();
            ResList.Clear();
            DllList.Clear();
#endif
            string cwd = Directory.GetCurrentDirectory();
            projFileName = Path.GetFullPath(projFileName);
            ParseProjectHelper(projFileName);
            Directory.SetCurrentDirectory(cwd);
            for (int i = 0; i < SrcList.Count; i++)
            {
                string src = SrcList[i];
                ParseSource(src);
                SrcList[i] = AdjustPath(src);
            }
        }
        private void ParseProjectHelper(string projFileName)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                projFileName = projFileName.Replace("$(HOME)", home);
            }
            if (projFileName.StartsWith("$"))
            {
                if (!SrcList.Contains(projFileName))
                    SrcList.Add(projFileName);
                return;
            }
            projFileName = Path.GetFullPath(projFileName);
            if (projFileName.Contains("+")) return;
            if (!SrcList.Contains(projFileName) && !projFileName.Contains(@"\obj\"))
            {
                //SrcList.Add(AdjustPath(projFileName));
                SrcList.Add(projFileName);
            }
            string projDir = Path.GetDirectoryName(projFileName)!;
            Directory.SetCurrentDirectory(projDir);
            string source = File.ReadAllText(projFileName);
            string[] lines = Sys.TextToLines(source).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                string? pat = null;
                Regex? r = null;
                Match? m = null;
                pat = @"^//css_inc[ ]+([^ ;]+)[ ]*;?[ ]*";
                r = new Regex(pat);
                m = r.Match(lines[i]);
                if (m.Success)
                {
                    string srcName = m.Groups[1].Value;
                    if (home != null)
                    {
                        srcName = srcName.Replace("$(HOME)", home);
                    }
                    //if (!srcName.StartsWith("$")) srcName = Path.GetFullPath(srcName);
                    ParseProjectHelper(srcName);
                }
                pat = @"^//css_dir[ ]+([^ ;]+)[ ]*;?[ ]*";
                r = new Regex(pat);
                m = r.Match(lines[i]);
                if (m.Success)
                {
                    string dirName = m.Groups[1].Value;
                    if (home != null)
                    {
                        dirName = dirName.Replace("$(HOME)", home);
                    }
                    //if (!dirName.StartsWith("$")) dirName = Path.GetFullPath(dirName);
                    //ParseProjectHelper(dirName);
                    SearchinDirectory(dirName);
                }
                 Directory.SetCurrentDirectory(projDir);
            }
        }
        private void SearchinDirectory(string dirName)
        {
            string? home = Environment.GetEnvironmentVariable("HOME");
            if (home != null)
            {
                dirName = dirName.Replace("$(HOME)", home);
            }
            dirName = Path.GetFullPath(dirName);
            string[] files = Directory.GetFiles(dirName, "*.cs", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (SrcList.Contains(file)) continue;
                ParseProjectHelper(file);
            }
        }
        private void ParseSource(string srcPath)
        {
            if (srcPath.StartsWith("$")) return;
            string source = File.ReadAllText(srcPath);
            string cwd = Directory.GetCurrentDirectory();
            Echo(cwd, "cwd");
            Directory.SetCurrentDirectory(Path.GetDirectoryName(srcPath)!);
            string[] lines = Sys.TextToLines(source).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                {
                    string pat = @"^//css_nuget[ ]+([^ ;]+)[ ]*;?[ ]*";
                    Regex r = new Regex(pat);
                    Match m = r.Match(lines[i]);
                    if (m.Success)
                    {
                        string pkgName = m.Groups[1].Value;
                        if (!PkgList.Contains(pkgName))
                        {
                            PkgList.Add(pkgName);
                        }
                    }

                }
                {
                    string pat = @"^//css_ref[ ]+([^ ;]+)[ ]*;?[ ]*";
                    Regex r = new Regex(pat);
                    Match m = r.Match(lines[i]);
                    if (m.Success)
                    {
                        string asmName = m.Groups[1].Value;
                        if (!AsmList.Contains(asmName))
                        {
                            AsmList.Add(asmName);
                        }
                    }

                }
                {
                    string pat = @"^//css_embed[ ]+([^ ;]+)[ ]*;?[ ]*";
                    Regex r = new Regex(pat);
                    Match m = r.Match(lines[i]);
                    if (m.Success)
                    {
                        string resName = m.Groups[1].Value;
                        string? home = Environment.GetEnvironmentVariable("HOME");
                        if (home != null)
                        {
                            resName = resName.Replace("$(HOME)", home);
                        }
                        if (!resName.StartsWith("$"))
                        {
                            resName = Path.GetFullPath(resName);
                        }
                        resName = AdjustPath(resName);
                        if (!ResList.Contains(resName))
                        {
                            ResList.Add(resName);
                        }
                    }

                }
                {
                    string pat = @"^//css_native[ ]+([^ ;]+)[ ]*;?[ ]*";
                    Regex r = new Regex(pat);
                    Match m = r.Match(lines[i]);
                    if (m.Success)
                    {
                        string dllName = m.Groups[1].Value;
                        dllName = Path.GetFullPath(dllName);
                        if (!DllList.Contains(dllName))
                        {
                            DllList.Add(dllName);
                        }
                    }
                }
                //Directory.SetCurrentDirectory(cwd);
            }
        }
    }
}
