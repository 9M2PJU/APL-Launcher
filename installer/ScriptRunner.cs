using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace APL.Setup
{
    internal enum LineKind { Log, Check, Plan, Step, Progress, Result }

    /// <summary>One line of a script's output; the APL-* markers are parsed, everything else is log text.</summary>
    internal sealed class ScriptLine
    {
        public LineKind Kind;
        public string Text = "";
        public object Json;
        public int Number, Total, Percent;
        public bool Ok;
        public bool IsError;

        internal static ScriptLine Parse(string raw, bool stderr)
        {
            var l = new ScriptLine { Text = raw, IsError = stderr };
            if (stderr || !raw.StartsWith("APL-", StringComparison.Ordinal)) return l;
            string marker = Head(raw, out string rest);
            switch (marker)
            {
                case "APL-CHECK":
                case "APL-PLAN":
                    object j;
                    if (!APL.Setup.Json.TryParse(rest, out j)) return l;
                    l.Kind = marker == "APL-CHECK" ? LineKind.Check : LineKind.Plan;
                    l.Json = j;
                    l.Text = "";
                    break;
                case "APL-STEP":
                    string n = Head(rest, out string afterN), total = Head(afterN, out string text);
                    if (!int.TryParse(n, out l.Number) || !int.TryParse(total, out l.Total)) return l;
                    l.Kind = LineKind.Step;
                    l.Text = text;
                    break;
                case "APL-PROGRESS":
                    string p = Head(rest, out string ptext);
                    if (!int.TryParse(p, out l.Percent)) return l;
                    l.Kind = LineKind.Progress;
                    l.Percent = Math.Max(0, Math.Min(100, l.Percent));
                    l.Text = ptext;
                    break;
                case "APL-RESULT":
                    string word = Head(rest, out string msg);
                    l.Kind = LineKind.Result;
                    l.Ok = word == "ok";
                    l.Text = msg;
                    break;
            }
            return l;
        }

        private static string Head(string s, out string rest)
        {
            s = s ?? "";
            int sp = s.IndexOf(' ');
            if (sp < 0) { rest = ""; return s.Trim(); }
            rest = s.Substring(sp + 1).Trim();
            return s.Substring(0, sp).Trim();
        }
    }

    internal sealed class ScriptResult
    {
        public bool Ok;
        public string Message = "";
        public int ExitCode;
    }

    /// <summary>
    /// Runs the PowerShell scripts with no window and no questions (APL_GUI=1), one at a time.
    /// Output is read as UTF-8 line by line and handed to the UI thread in order.
    /// </summary>
    internal sealed class ScriptRunner
    {
        private readonly Dispatcher ui;
        private Process current;

        internal string Root;
        /// <summary>What is running now ("Installing…"), null when idle.</summary>
        internal string Running { get; private set; }
        internal event Action Changed;
        internal bool Busy { get { return Running != null; } }
        /// <summary>True while a script that changes the screen runs (quitting then needs a confirmation).</summary>
        internal bool ChangesScreen { get; private set; }

        /// <summary>The running task inside a sentence: "Installing…" → "installing".</summary>
        internal string TaskName
        {
            get
            {
                string t = (Running ?? "").TrimEnd('…', '.', ' ');
                return t.Length == 0 ? t : char.ToLower(t[0], Strings.Culture) + t.Substring(1);
            }
        }

        internal ScriptRunner(Dispatcher dispatcher) { ui = dispatcher; }

        internal static string PowerShellExe
        {
            get
            {
                string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe");
                return File.Exists(p) ? p : "powershell.exe";
            }
        }

        /// <summary>Quotes one argument the way powershell.exe splits its command line (CommandLineToArgvW rules).</summary>
        internal static string Arg(string a)
        {
            a = a ?? "";
            if (a.Length > 0 && a.IndexOfAny(new[] { ' ', '\t', '"', '\n' }) < 0) return a;
            var sb = new StringBuilder("\"");
            int slashes = 0;
            foreach (char c in a)
            {
                if (c == '\\') { slashes++; continue; }
                if (c == '"') { sb.Append('\\', slashes * 2 + 1); sb.Append('"'); slashes = 0; continue; }
                sb.Append('\\', slashes); slashes = 0;
                sb.Append(c);
            }
            sb.Append('\\', slashes * 2);
            return sb.Append('"').ToString();
        }

        internal Task<ScriptResult> Run(string script, IEnumerable<string> args, string title, Action<ScriptLine> onLine,
                                        IEnumerable<string> secrets = null, bool changesScreen = false)
        {
            if (Busy) throw new InvalidOperationException("A task is already running.");
            if (Root == null) throw new InvalidOperationException("The scripts are not ready yet.");
            string path = Path.Combine(Root, "scripts", script);
            if (!File.Exists(path)) throw new FileNotFoundException("Script not found: " + path);

            var hide = (secrets ?? Enumerable.Empty<string>()).Where(s => !string.IsNullOrEmpty(s) && s.Length >= 4).ToList();
            var psi = new ProcessStartInfo(PowerShellExe)
            {
                Arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass -File " + Arg(path) + " " + string.Join(" ", args.Select(Arg)),
                WorkingDirectory = Root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // Closed right away: a tool that would ask for a password fails instead of waiting forever.
                RedirectStandardInput = true,
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false)
            };
            psi.EnvironmentVariables["APL_GUI"] = "1";
            psi.EnvironmentVariables["APL_LANG"] = Strings.French ? "fr" : "en";

            Running = title;
            ChangesScreen = changesScreen;
            Fire();
            Log.Info("run " + script);   // never the arguments: they hold the keys

            var done = new TaskCompletionSource<ScriptResult>();
            var result = new ScriptResult();
            bool sawResult = false;
            string lastError = null;
            Action<string, bool> receive = (raw, err) =>
            {
                if (raw == null) return;
                foreach (string s in hide) raw = raw.Replace(s, "••••");
                var line = ScriptLine.Parse(raw.TrimEnd(), err);
                if (line.Kind == LineKind.Result)
                {
                    sawResult = true;
                    result.Ok = line.Ok;
                    result.Message = line.Text;
                }
                if (err && line.Text.Trim().Length > 0) lastError = line.Text.Trim();
                ui.BeginInvoke(new Action(() => { try { onLine(line); } catch (Exception ex) { Log.Error("script line", ex); } }));
            };

            Task.Run(() =>
            {
                try
                {
                    using (var p = new Process { StartInfo = psi })
                    {
                        p.OutputDataReceived += (s, e) => receive(e.Data, false);
                        p.ErrorDataReceived += (s, e) => receive(e.Data, true);
                        p.Start();
                        current = p;
                        p.StandardInput.Close();
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.WaitForExit();   // also waits for the last output lines
                        result.ExitCode = p.ExitCode;
                    }
                    if (!sawResult) result.Ok = result.ExitCode == 0;
                    else if (result.ExitCode != 0) result.Ok = false;
                    if (!result.Ok && string.IsNullOrWhiteSpace(result.Message))
                        result.Message = lastError ?? Strings.T("The script stopped unexpectedly (code {0}).", result.ExitCode);
                }
                catch (Exception ex)
                {
                    Log.Error("run " + script, ex);
                    result.Ok = false;
                    result.Message = ex.Message;
                }
                current = null;
                Log.Info("end " + script + ": " + (result.Ok ? "ok" : "fail, code " + result.ExitCode));
                ui.BeginInvoke(new Action(() =>
                {
                    Running = null;
                    ChangesScreen = false;
                    Fire();
                    done.SetResult(result);
                }));
            });
            return done.Task;
        }

        /// <summary>Interactive tools keep their own console window (they ask questions).</summary>
        internal void OpenConsole(string script, string title)
        {
            if (Busy) return;
            string path = Path.Combine(Root, "scripts", script);
            var psi = new ProcessStartInfo(PowerShellExe)
            {
                Arguments = "-NoLogo -NoProfile -ExecutionPolicy Bypass -File " + Arg(path),
                WorkingDirectory = Root,
                UseShellExecute = true
            };
            Log.Info("console " + script);
            var p = Process.Start(psi);
            if (p == null) return;
            Running = title;
            Fire();
            p.EnableRaisingEvents = true;
            p.Exited += (s, e) => ui.BeginInvoke(new Action(() =>
            {
                Running = null;
                Fire();
            }));
        }

        /// <summary>When the window closes in the middle of a task: stop the script and what it started.</summary>
        internal void StopAll()
        {
            foreach (var p in new[] { current })
            {
                try
                {
                    if (p == null || p.HasExited) continue;
                    var kill = new ProcessStartInfo("taskkill.exe", "/T /F /PID " + p.Id) { UseShellExecute = false, CreateNoWindow = true };
                    Process.Start(kill)?.WaitForExit(5000);
                }
                catch { }
            }
        }

        /// <summary>Test hook (--shot): looks busy without running anything.</summary>
        internal void Fake(string title)
        {
            Running = title;
            Fire();
        }

        private void Fire()
        {
            var h = Changed;
            if (h != null) h();
        }
    }
}
