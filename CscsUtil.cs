#if USE_CSCS_UTIL
namespace Global
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    //using Global;
    using static Global.EasyObject;
#if GLOBAL_SYS
    public
#else
    internal
#endif
    class CscsUtil
    {
        private bool generateDllProject = false;
        public readonly string projDir;
        public readonly string? home;
        public string? OutType;
        public List<string> SrcList = new List<string> { };
        public List<string> PkgList = new List<string> { };
        public List<string> AsmList = new List<string> { };
        public List<string> ResList = new List<string> { };
        public List<string> DllList = new List<string> { };
        public List<string> DefList = new List<string> { };
        public CscsUtil(string projFileName)
        {
            Log(projFileName, "projFileName");
            projDir = Path.GetDirectoryName(Path.GetFullPath(projFileName))!;
            Log(projDir, "projDir");
            home = FindHome(new DirectoryInfo(projDir));
            Log(home, "home");
        }
        public void DebugDump()
        {
            Log(OutType, "OutType");
            Log(SrcList, "SrcList");
            Log(PkgList, "PkgList");
            Log(AsmList, "AsmList");
            Log(ResList, "ResList");
            Log(DllList, "DllList");
            Log(DefList, "DefList");
        }
        public static string? FindHome(DirectoryInfo dir)
        {
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Name == ".bashrc" || file.Name == ".profile")
                {
                    return dir.FullName;
                }
            }
            DirectoryInfo? parent = dir.Parent;
            if (parent == null)
            {
                return null;
            }
            return FindHome(parent);
        }
        private string AdjustPath(string path)
        {
            if (home != null)
            {
                path = path.Replace(home + @"\", @"$(HOME)\");
            }
            return path;
        }
        public void ParseProject(string projFileName, bool generateDllProject)
        {
            this.generateDllProject = generateDllProject;
            this.OutType = this.generateDllProject ? "Library" : "Exe";
            Log(projFileName, "CscsUtil.ParseProject()");
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
                SrcList.Add(projFileName);
            }
            string projDir = Path.GetDirectoryName(projFileName)!;
            Directory.SetCurrentDirectory(projDir);
            string source = File.ReadAllText(projFileName);
            string[] lines = Sys.TextToLines(source).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                List<string>? m = null;
                m = Sys.FindFirstMatch(lines[i],
                    @"^//[+]#gui[ ]*;?[ ]*"
                    );
                if (m != null)
                {
                    if (!this.generateDllProject)
                    {
                        this.OutType = "WinExe";
                    }
                }
                m = Sys.FindFirstMatch(lines[i],
                    @"^//css_inc[ ]+([^ ;]+)[ ]*;?[ ]*",
                    @"^//[+]#inc[ ]+([^ ;]+)[ ]*;?[ ]*"
                    );
                if (m != null)
                {
                    string srcName = m[1];
                    if (home != null)
                    {
                        srcName = srcName.Replace("$(HOME)", home);
                    }
                    ParseProjectHelper(srcName);
                }
                m = Sys.FindFirstMatch(lines[i],
                    @"^//css_dir[ ]+([^ ;]+)[ ]*;?[ ]*",
                    @"^//[+]#dir[ ]+([^ ;]+)[ ]*;?[ ]*"
                    );
                if (m != null)
                {
                    string dirName = m[1];
                    if (home != null)
                    {
                        dirName = dirName.Replace("$(HOME)", home);
                    }
                    SearchinDirectory(dirName);
                }
                 Directory.SetCurrentDirectory(projDir);
            }
        }
        private void SearchinDirectory(string dirName)
        {
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
            Log(cwd, "cwd");
            Directory.SetCurrentDirectory(Path.GetDirectoryName(srcPath)!);
            string[] lines = Sys.TextToLines(source).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                {
                    var m = Sys.FindFirstMatch(lines[i],
                        @"^//css_nuget[ ]+([^ ;]+)[ ]*;?[ ]*",
                        @"^//[+]#nuget[ ]+([^ ;]+)[ ]*;?[ ]*"
                        );
                    if (m != null)
                    {
                        string pkgName = m[1];
                        if (!PkgList.Contains(pkgName))
                        {
                            PkgList.Add(pkgName);
                        }
                    }
                }
                {
                    var m = Sys.FindFirstMatch(lines[i],
                        @"^//css_ref[ ]+([^ ;]+)[ ]*;?[ ]*",
                        @"^//[+]#ref[ ]+([^ ;]+)[ ]*;?[ ]*"
                        );
                    if (m != null)
                    {
                        string asmName = m[1];
                        if (!asmName.StartsWith("$"))
                        {
                            asmName = Path.GetFullPath(asmName);
                        }
                        asmName = AdjustPath(asmName);
                        if (!AsmList.Contains(asmName))
                        {
                            AsmList.Add(asmName);
                        }
                    }
                }
                {
                    var m = Sys.FindFirstMatch(lines[i],
                        @"^//css_embed[ ]+([^ ;]+)[ ]*;?[ ]*",
                        @"^//[+]#embed[ ]+([^ ;]+)[ ]*;?[ ]*"
                        );
                    if (m != null)
                    {
                        string resName = m[1];
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
                    var m = Sys.FindFirstMatch(lines[i],
                        @"^//css_native[ ]+([^ ;]+)[ ]*;?[ ]*",
                        @"^//[+]#native[ ]+([^ ;]+)[ ]*;?[ ]*"
                        );
                    if (m != null)
                    {
                        string dllName = m[1];
                        dllName = Path.GetFullPath(dllName);
                        if (!DllList.Contains(dllName))
                        {
                            DllList.Add(dllName);
                        }
                    }
                }
                {
                    var m = Sys.FindFirstMatch(lines[i],
                        @"^//css_def[ ]+([^ ;]+)[ ]*;?[ ]*",
                        @"^//[+]#def[ ]+([^ ;]+)[ ]*;?[ ]*"
                        );
                    if (m != null)
                    {
                        string defName = m[1];
                        if (!DefList.Contains(defName))
                        {
                            DefList.Add(defName);
                        }
                    }

                }
            }
        }
    }
}
#endif
