using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WarlockTools
{
    enum ActionKind
    {
        UnpackPack,
        PackFolder,
        Bin2Xml,
        Xml2Bin,
        Xr2Xml,
        Xml2Xr,
        Md2Txt,
        Txt2Md
    }

    sealed class JobResult
    {
        public bool Ok;
        public string Message;
        public string OutputPath;
    }

    static class ToolRunner
    {
        public const string SkipExt = ".wav.ogg.ogm.mp3.avi.dds.png.tga.jpg.fev.fsb";

        public static string GuiDir;
        public static string ToolsDir;
        public static string Squeezer;
        public static string XrConvert;
        public static string TxmlConvert;

        public static bool FindTools(out string error)
        {
            error = null;
            string here = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(here))
                here = Environment.CurrentDirectory;
            here = Path.GetFullPath(here);

            string parent = Path.GetDirectoryName(here.TrimEnd(Path.DirectorySeparatorChar));
            foreach (string cand in Candidates(here, parent))
            {
                if (LooksLikeTools(cand))
                {
                    Bind(cand, here);
                    return true;
                }
            }

            error = UiLang.T("ToolsMissing");
            return false;
        }

        static IEnumerable<string> Candidates(string guiDir, string parent)
        {
            yield return guiDir;
            foreach (string name in new[] { "not use", "notuse", "Not Use", "NOT USE" })
                yield return Path.Combine(guiDir, name);
            if (!string.IsNullOrEmpty(parent))
            {
                yield return parent;
                foreach (string name in new[] { "not use", "notuse" })
                    yield return Path.Combine(parent, name);
            }
        }

        static bool LooksLikeTools(string dir)
        {
            return File.Exists(Path.Combine(dir, "Squeezer.exe"))
                && File.Exists(Path.Combine(dir, "XRconvert_Final.exe"))
                && File.Exists(Path.Combine(dir, "TXMLConvert.exe"));
        }

        static void Bind(string toolsDir, string guiDir)
        {
            ToolsDir = Path.GetFullPath(toolsDir);
            GuiDir = Path.GetFullPath(guiDir);
            Squeezer = Path.Combine(ToolsDir, "Squeezer.exe");
            XrConvert = Path.Combine(ToolsDir, "XRconvert_Final.exe");
            TxmlConvert = Path.Combine(ToolsDir, "TXMLConvert.exe");
        }

        public static string GameRoot()
        {
            return Path.GetFullPath(Path.Combine(GuiDir, ".."));
        }

        public static string ExtractRoot()
        {
            return Path.Combine(GuiDir, "_extract");
        }

        public static ActionKind GuessAction(string path)
        {
            if (Directory.Exists(path))
                return ActionKind.PackFolder;
            string ext = Path.GetExtension(path);
            if (ext.Equals(".pack", StringComparison.OrdinalIgnoreCase))
                return ActionKind.UnpackPack;
            if (ext.Equals(".binary", StringComparison.OrdinalIgnoreCase))
                return ActionKind.Bin2Xml;
            if (ext.Equals(".xr", StringComparison.OrdinalIgnoreCase))
                return ActionKind.Xr2Xml;
            if (ext.Equals(".md", StringComparison.OrdinalIgnoreCase))
                return ActionKind.Md2Txt;
            if (ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                return ActionKind.Txt2Md;
            if (ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
            {
                string stem = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path));
                if (File.Exists(stem + ".xr"))
                    return ActionKind.Xml2Xr;
                return ActionKind.Xml2Bin;
            }
            return ActionKind.UnpackPack;
        }

        public static string SuggestOutput(string input, ActionKind kind)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";
            string dir = Directory.Exists(input) ? input : (Path.GetDirectoryName(input) ?? ToolsDir);
            string name = Directory.Exists(input)
                ? new DirectoryInfo(input).Name
                : Path.GetFileNameWithoutExtension(input);

            switch (kind)
            {
                case ActionKind.UnpackPack:
                    return Path.Combine(ExtractRoot(), name);
                case ActionKind.PackFolder:
                    return Path.Combine(Directory.GetParent(input.TrimEnd('\\')) != null
                        ? Directory.GetParent(input.TrimEnd('\\')).FullName
                        : dir, name + ".pack");
                case ActionKind.Bin2Xml:
                case ActionKind.Xr2Xml:
                    return Path.Combine(dir, name + ".xml");
                case ActionKind.Xml2Bin:
                    return Path.Combine(dir, name + ".binary");
                case ActionKind.Xml2Xr:
                    return Path.Combine(dir, name + ".xr");
                case ActionKind.Md2Txt:
                    return Path.Combine(dir, name + "_dec.txt");
                case ActionKind.Txt2Md:
                    return Path.Combine(dir, name.EndsWith("_dec", StringComparison.OrdinalIgnoreCase)
                        ? name.Substring(0, name.Length - 4) + ".md"
                        : name + ".md");
                default:
                    return "";
            }
        }

        public static JobResult Run(ActionKind kind, string input, string output, Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(input) || (!File.Exists(input) && !Directory.Exists(input)))
                return Fail(UiLang.T("InputMissing"));
            if (string.IsNullOrWhiteSpace(output))
                return Fail(UiLang.T("NeedOutput"));

            try
            {
                switch (kind)
                {
                    case ActionKind.UnpackPack:
                        return Unpack(input, output, log);
                    case ActionKind.PackFolder:
                        return Pack(input, output, log);
                    case ActionKind.Bin2Xml:
                        return RunTool(TxmlConvert, "\\tUTF8 " + Q(Rel(input)) + " " + Q(Rel(output)), output, log);
                    case ActionKind.Xml2Bin:
                        return RunTool(TxmlConvert, "\\tbin " + Q(Rel(input)) + " " + Q(Rel(output)), output, log);
                    case ActionKind.Xr2Xml:
                        return RunTool(XrConvert, "-t:text " + Q(Rel(input)) + " " + Q(Rel(output)), output, log);
                    case ActionKind.Xml2Xr:
                        return RunTool(XrConvert, "-t:bin " + Q(Rel(input)) + " " + Q(Rel(output)), output, log);
                    case ActionKind.Md2Txt:
                        log("WarlockMd unpack " + input);
                        WarlockMd.UnpackFile(input, output);
                        return new JobResult { Ok = true, Message = output, OutputPath = output };
                    case ActionKind.Txt2Md:
                        log("WarlockMd pack " + input);
                        WarlockMd.PackFile(input, output);
                        return new JobResult { Ok = true, Message = output, OutputPath = output };
                    default:
                        return Fail("unknown action");
                }
            }
            catch (Exception ex)
            {
                return Fail(ex.Message);
            }
        }

        static JobResult Unpack(string pack, string dest, Action<string> log)
        {
            Directory.CreateDirectory(dest);
            return RunTool(Squeezer, "/e " + Q(Rel(pack)) + " " + Q(Rel(dest)), dest, log);
        }

        static JobResult Pack(string folder, string pack, Action<string> log)
        {
            string parent = Path.GetDirectoryName(pack);
            if (!string.IsNullOrEmpty(parent))
                Directory.CreateDirectory(parent);
            string args = "/d " + Q(Rel(folder)) + " " + Q(Rel(pack))
                + " " + Q(SkipExt) + " " + Q(SkipExt) + " 5";
            return RunTool(Squeezer, args, pack, log);
        }

        static JobResult RunTool(string exe, string args, string output, Action<string> log)
        {
            log(Path.GetFileName(exe) + " " + args);
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = ToolsDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.GetEncoding(866),
                StandardErrorEncoding = Encoding.GetEncoding(866)
            };

            var sb = new StringBuilder();
            using (var p = new Process { StartInfo = psi })
            {
                p.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    sb.AppendLine(e.Data);
                    log(e.Data);
                };
                p.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null) return;
                    sb.AppendLine(e.Data);
                    log(e.Data);
                };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                if (!p.WaitForExit(60 * 60 * 1000))
                {
                    try { p.Kill(); } catch { }
                    return Fail("timeout");
                }
                p.WaitForExit();

                bool exists = File.Exists(output) || Directory.Exists(output);
                if (p.ExitCode != 0 && !exists)
                    return Fail(UiLang.Tf("ExitCode", p.ExitCode) + Environment.NewLine + sb);
                if (!exists)
                    return Fail("no output");
                return new JobResult { Ok = true, Message = output, OutputPath = output };
            }
        }

        static JobResult Fail(string msg)
        {
            return new JobResult { Ok = false, Message = msg };
        }

        public static string Rel(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            string full = Path.GetFullPath(path);
            string root = ToolsDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var from = new Uri(root);
            var to = new Uri(full);
            if (!string.Equals(from.Scheme, to.Scheme, StringComparison.OrdinalIgnoreCase))
                return full;
            string rel = Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString()).Replace('/', '\\');
            if (string.IsNullOrEmpty(rel))
                return ".";
            if (!rel.StartsWith(".", StringComparison.Ordinal))
                rel = ".\\" + rel;
            return rel;
        }

        public static string Q(string s)
        {
            if (s == null) return "\"\"";
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }
    }
}
