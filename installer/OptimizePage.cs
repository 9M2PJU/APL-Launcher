using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static APL.Setup.Strings;

namespace APL.Setup
{
    internal sealed class OptimizePage : Page
    {
        private readonly ScrollViewer scroll;
        private readonly Border noScreenCard, readCard, youTubeCard, runCard, doneCard, failCard, footer;
        private readonly TextBlock noScreenText, readTitle, readText, keptHeader, runText, runPercent, failText, footerHint;
        private readonly Button goInstall, readButton, optimizeButton, readAgain, retryButton, backButton, againButton;
        private readonly Control readSpinner;
        private readonly Expander readDetails, keptExpander, details;
        private readonly TextBox readLog, logBox;
        private readonly StackPanel planView, runView, appsList, areasList, alsoList, summaryList;
        private readonly WrapPanel keptList;
        private readonly CheckBox youTubeCheck;
        private readonly ProgressBar runBar;
        private readonly Grid doneMark, failMark;

        private object plan;
        private string state = "start";     // start | reading | plan | run | done | fail
        private bool readFailed;
        private readonly List<KeyValuePair<CheckBox, string>> appChecks = new List<KeyValuePair<CheckBox, string>>();
        // What was asked, for the summary
        private int askedApps, askedComponents;
        private bool askedYouTube;

        internal OptimizePage(MainWindow main) : base(main, "optimize", "OptimizePage.xaml")
        {
            scroll = F<ScrollViewer>("Scroll");
            noScreenCard = F<Border>("NoScreenCard");
            readCard = F<Border>("ReadCard");
            youTubeCard = F<Border>("YouTubeCard");
            runCard = F<Border>("RunCard");
            doneCard = F<Border>("DoneCard");
            failCard = F<Border>("FailCard");
            footer = F<Border>("Footer");
            noScreenText = F<TextBlock>("NoScreenText");
            readTitle = F<TextBlock>("ReadTitle");
            readText = F<TextBlock>("ReadText");
            keptHeader = F<TextBlock>("KeptHeader");
            runText = F<TextBlock>("RunText");
            runPercent = F<TextBlock>("RunPercent");
            failText = F<TextBlock>("FailText");
            footerHint = F<TextBlock>("FooterHint");
            goInstall = F<Button>("GoInstall");
            readButton = F<Button>("ReadButton");
            optimizeButton = F<Button>("OptimizeButton");
            readAgain = F<Button>("ReadAgain");
            retryButton = F<Button>("RetryButton");
            backButton = F<Button>("BackButton");
            againButton = F<Button>("AgainButton");
            readSpinner = F<Control>("ReadSpinner");
            readDetails = F<Expander>("ReadDetails");
            keptExpander = F<Expander>("KeptExpander");
            details = F<Expander>("Details");
            readLog = F<TextBox>("ReadLog");
            logBox = F<TextBox>("LogBox");
            planView = F<StackPanel>("PlanView");
            runView = F<StackPanel>("RunView");
            appsList = F<StackPanel>("AppsList");
            areasList = F<StackPanel>("AreasList");
            alsoList = F<StackPanel>("AlsoList");
            summaryList = F<StackPanel>("SummaryList");
            keptList = F<WrapPanel>("KeptList");
            youTubeCheck = F<CheckBox>("YouTubeCheck");
            runBar = F<ProgressBar>("RunBar");
            doneMark = F<Grid>("DoneMark");
            failMark = F<Grid>("FailMark");
            doneMark.Children.Add(Ui.Badge(Status.Ok, 46));
            failMark.Children.Add(Ui.Badge(Status.Fail, 24));

            goInstall.Click += (s, e) => Main.Navigate("install");
            readButton.Click += (s, e) => ReadPlan();
            readAgain.Click += (s, e) => ReadPlan();
            againButton.Click += (s, e) => ReadPlan();
            optimizeButton.Click += (s, e) => Apply();
            retryButton.Click += (s, e) => Apply(false);
            backButton.Click += (s, e) => SetState("plan");
            youTubeCheck.Checked += (s, e) => UpdateActions();
            youTubeCheck.Unchecked += (s, e) => UpdateActions();
            SetState("start");
        }

        private void SetState(string s)
        {
            state = s;
            bool start = s == "start" || s == "reading";
            F<TextBlock>("PageTitle").Text = s == "run" ? T("Optimizing the screen") : T("Optimize");
            F<TextBlock>("PageSubtitle").Text = s == "run" ? T("Keep the screen on. It takes a minute or two.")
                : T("Removes what the screen doesn't need, so it starts faster and stays quiet. Nothing is deleted from the system, and Restore undoes it.");
            Ui.Show(noScreenCard, start && !Main.ScreenReady && s != "reading");
            Ui.Show(readCard, start && (Main.ScreenReady || s == "reading" || readFailed));
            Ui.Show(planView, s == "plan");
            Ui.Show(footer, s == "plan");
            Ui.Show(runView, s == "run" || s == "done" || s == "fail");
            Ui.Show(runCard, s == "run" || s == "fail");
            Ui.Show(F<Control>("RunSpinner"), s == "run");
            runText.Margin = new Thickness(s == "run" ? 12 : 0, 0, 12, 0);   // no spinner, no gap
            Ui.Show(doneCard, s == "done");
            Ui.Show(failCard, s == "fail");
            Ui.Show(againButton, s == "done");
            Ui.Show(readSpinner, s == "reading");
            Ui.Show(readButton, s != "reading");
            Ui.Show(readDetails, s == "reading" || readFailed);
            if (s == "reading")
            {
                readTitle.Text = T("Reading the screen…");
                readText.Text = T("About 30 seconds. Nothing is changed.");
                readText.Foreground = Ui.Brush("Text2");
            }
            else if (!readFailed)
            {
                readTitle.Text = T("Read the screen");
                readText.Text = T("Lists what is installed and what can go. About 30 seconds; nothing is changed.");
                readText.Foreground = Ui.Brush("Text2");
            }
            readButton.Content = readFailed ? T("Try again") : T("Read the screen");
            if (!Main.ScreenReady)
                noScreenText.Text = !Vetro.IsIPv4(Main.Ip) ? T("Choose the screen on the Install page first.")
                                  : !Main.VetroChecked ? T("Looking for Vetro at {0}…", Main.Ip)
                                  : T("Vetro does not answer at {0}. Check the screen on the Install page.", Main.Ip);
            UpdateActions();
        }

        internal override void OnShown()
        {
            // Opening the page reads the screen by itself, once, when the screen answers.
            if (state == "start" && plan == null && !readFailed && Main.ScreenReady && !Main.Runner.Busy && !Main.Offline) ReadPlan();
        }

        internal void OnScreenChanged()
        {
            if (state == "start") SetState("start");
            if (state == "start" && Main.ShownPage == Key) OnShown();
        }

        internal override void UpdateActions()
        {
            bool busy = Main.Runner.Busy;
            bool ready = Main.ScreenReady;
            readButton.IsEnabled = !busy && ready;
            readAgain.IsEnabled = !busy && ready;
            againButton.IsEnabled = !busy && ready;
            retryButton.IsEnabled = !busy && ready;
            optimizeButton.IsEnabled = !busy && ready && plan != null;
            if (plan == null) return;
            int apps = appChecks.Count(c => c.Key.IsChecked == true);
            int comps = Json.List(plan, "silence").Count;
            string hint = T("{0} to uninstall · {1} to turn off", Count(apps, "app", "apps"), Count(comps, "component", "components"));
            if (!ready) hint = T("Vetro must answer on the screen.");
            else if (busy) hint = T("Wait for the current task to finish ({0}).", Main.Runner.TaskName);
            footerHint.Text = hint;
        }

        private static string Count(int n, string one, string many)
        {
            return n == 1 ? T("1 " + one) : T("{0} " + many, n);
        }

        // ================= reading (optimize.ps1 -Plan) =================

        internal async void ReadPlan()
        {
            if (Main.Offline || Main.Runner.Busy || !Main.ScreenReady) return;
            readFailed = false;
            readLog.Clear();
            readDetails.IsExpanded = false;
            SetState("reading");
            object answer = null;
            ScriptResult r;
            try
            {
                r = await Main.Runner.Run("optimize.ps1", new[] { "-TvIp", Main.Ip, "-Plan" }, T("Reading the screen…"), l =>
                {
                    if (l.Kind == LineKind.Plan) answer = l.Json;
                    else if (l.Kind == LineKind.Log && l.Text.Trim().Length > 0) AppendLog(readLog, l.Text);
                    else if (l.Kind == LineKind.Result && !l.Ok) AppendLog(readLog, "✗ " + l.Text);
                });
            }
            catch (Exception ex)
            {
                Log.Error("plan", ex);
                r = new ScriptResult { Ok = false, Message = ex.Message };
            }
            if (answer != null && r.Ok) { ShowPlan(answer); return; }
            readFailed = true;
            SetState("start");
            readTitle.Text = T("The screen could not be read");
            readText.Text = string.IsNullOrWhiteSpace(r.Message) ? T("The script gave no list.") : r.Message;
            readText.Foreground = Ui.Brush("Red");
            readDetails.IsExpanded = true;
        }

        /// <summary>Fills the plan view from APL-PLAN. Also used by --shot.</summary>
        internal void ShowPlan(object json)
        {
            plan = json;
            readFailed = false;

            appsList.Children.Clear();
            appChecks.Clear();
            var apps = Json.List(json, "uninstall");
            if (apps.Count == 0)
                appsList.Children.Add(Ui.Text(T("No downloaded app to uninstall."), "Secondary", new Thickness(44, 4, 0, 0)));
            foreach (var a in apps)
            {
                string id = Json.Str(a, "appId") ?? "", name = Json.Str(a, "name");
                if (appsList.Children.Count > 0) appsList.Children.Add(Ui.Separator(new Thickness(44, 0, 0, 0)));
                var texts = new StackPanel();
                texts.Children.Add(new TextBlock { Text = string.IsNullOrWhiteSpace(name) ? id : name, FontSize = 14 });
                texts.Children.Add(new TextBlock { Text = id, FontSize = 12, Foreground = Ui.Brush("Text3"), TextTrimming = TextTrimming.CharacterEllipsis });
                var cb = new CheckBox { Content = texts, IsChecked = true, Margin = new Thickness(5, 9, 0, 9) };
                cb.Checked += (s, e) => UpdateActions();
                cb.Unchecked += (s, e) => UpdateActions();
                appChecks.Add(new KeyValuePair<CheckBox, string>(cb, id));
                appsList.Children.Add(cb);
            }

            areasList.Children.Clear();
            var silence = Json.List(json, "silence");
            if (silence.Count == 0)
                areasList.Children.Add(Ui.Text(T("Nothing to turn off."), "Secondary", new Thickness(44, 0, 0, 0)));
            foreach (var group in silence.GroupBy(x => Json.Str(x, "area") ?? ""))
            {
                var items = group.ToList();
                areasList.Children.Add(Ui.Text(AreaName(group.Key) + "  ·  " + items.Count, "Section",
                                               new Thickness(44, areasList.Children.Count > 0 ? 10 : 0, 0, 8)));
                var wrap = new WrapPanel { Margin = new Thickness(44, 0, 0, 0) };
                foreach (var it in items)
                {
                    string n = Json.Str(it, "name");
                    wrap.Children.Add(Ui.Pill(string.IsNullOrWhiteSpace(n) ? Json.Str(it, "appId") : n));
                }
                areasList.Children.Add(wrap);
            }

            var yt = Json.List(json, "youtube");
            Ui.Show(youTubeCard, yt.Count > 0);
            youTubeCheck.IsChecked = false;

            alsoList.Children.Clear();
            int dns = Json.Int(json, "dnsCount");
            var also = new List<string>
            {
                T("Ads, tracking and Bixby turned off. Alexa stays on the microphone button."),
                dns > 0 ? T("Ad filter on the screen: {0} Samsung ad and tracking addresses blocked.", dns) : T("Ad filter on the screen for Samsung's ad and tracking addresses."),
                T("Installation leftovers cleaned up, to free some storage."),
                T("Smart Hub no longer opens by itself at power-on.")
            };
            foreach (string s in also)
            {
                var b = MainWindow.Bullet(s, "Teal");
                b.Margin = new Thickness(36, 3, 0, 3);
                alsoList.Children.Add(b);
            }

            keptList.Children.Clear();
            var kept = Json.List(json, "kept").Select(k => k as string).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().OrderBy(k => k, StringComparer.CurrentCultureIgnoreCase).ToList();
            foreach (string k in kept) keptList.Children.Add(Ui.Pill(k));
            keptHeader.Text = kept.Count > 0 ? T("Kept ({0})", kept.Count) : T("Kept");
            keptExpander.IsExpanded = false;

            SetState("plan");
            scroll.ScrollToTop();
        }

        private static string AreaName(string area)
        {
            switch ((area ?? "").Trim().ToLowerInvariant())
            {
                case "": return T("Other");
                case "ads": case "ad": case "advertising": return T("Ads");
                case "tracking": case "analytics": case "privacy": return T("Tracking and analytics");
                case "voice": case "bixby": return T("Voice and Bixby");
                case "samsung": case "services": return T("Samsung services");
                case "ambient": return T("Ambient mode");
                case "store": case "hotel": case "shop": return T("Store and hotel modes");
                case "diagnostics": case "diagnostic": case "logs": return T("Diagnostics");
                case "smartthings": case "iot": return "SmartThings";
                case "sharing": return T("Sharing");
                case "media": return T("Media");
                case "other": case "misc": return T("Other");
            }
            // A label from Vetro: shown as it comes, with a capital letter.
            return char.ToUpper(area[0], CultureInfo.CurrentCulture) + area.Substring(1);
        }

        // ================= applying =================

        private async void Apply(bool confirm = true)
        {
            if (plan == null || Main.Runner.Busy || !Main.ScreenReady) return;
            var chosen = appChecks.Where(c => c.Key.IsChecked == true).Select(c => c.Value).Where(id => id.Length > 0).ToList();
            askedApps = chosen.Count;
            askedComponents = Json.List(plan, "silence").Count;
            askedYouTube = youTubeCard.Visibility == Visibility.Visible && youTubeCheck.IsChecked == true;

            if (confirm && !await Main.Ask(T("Optimize the screen?"), T("It takes a minute or two. Restore undoes it, and uninstalled apps can come back from the App Store."),
                                           T("Optimize"), glyph: "", tileColor: "Orange", bullets: ConfirmBullets()))
                return;

            var args = new List<string> { "-TvIp", Main.Ip, "-Unattended", "-Uninstall", string.Join(";", chosen) };
            if (askedYouTube) args.Add("-RemoveYouTube");
            BeginRun();
            ScriptResult r;
            try
            {
                r = await Main.Runner.Run("optimize.ps1", args, T("Optimizing…"), OnLine, null, true);
            }
            catch (Exception ex)
            {
                Log.Error("optimize", ex);
                r = new ScriptResult { Ok = false, Message = ex.Message };
            }
            if (r.Ok) ShowDone(); else Failed(r.Message);
        }

        internal List<string> ConfirmBullets()
        {
            var b = new List<string>();
            b.Add(askedApps == 0 ? T("No app uninstalled") : askedApps == 1 ? T("Uninstall 1 downloaded app") : T("Uninstall {0} downloaded apps", askedApps));
            if (askedComponents > 0) b.Add(askedComponents == 1 ? T("Turn off 1 Samsung component") : T("Turn off {0} Samsung components", askedComponents));
            if (askedYouTube) b.Add(T("Uninstall Samsung's YouTube"));
            b.Add(T("Ads, tracking and Bixby off, ad filter, cleanup"));
            return b;
        }

        /// <summary>Test hook (--shot): what the confirmation will list.</summary>
        internal void SetAsked(int apps, int components, bool youtube)
        {
            askedApps = apps;
            askedComponents = components;
            askedYouTube = youtube;
        }

        internal void BeginRun()
        {
            logBox.Clear();
            details.IsExpanded = false;
            runBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
            runBar.Value = 0;
            runText.Text = T("Starting…");
            runPercent.Text = "";
            SetState("run");
            scroll.ScrollToTop();
        }

        private void OnLine(ScriptLine l)
        {
            switch (l.Kind)
            {
                case LineKind.Progress:
                    SetProgress(l.Percent, l.Text);
                    break;
                case LineKind.Log:
                    if (l.Text.Trim().Length > 0) AppendLog(logBox, l.Text);
                    break;
                case LineKind.Result:
                    if (!l.Ok) AppendLog(logBox, "✗ " + l.Text);
                    break;
            }
        }

        internal void SetProgress(int percent, string text)
        {
            if (Main.Offline) runBar.Value = percent; else Ui.SetProgress(runBar, percent);
            if (!string.IsNullOrWhiteSpace(text)) runText.Text = text;
            runPercent.Text = Percent(percent);
            Main.SetSideDetail(Percent(percent) + (string.IsNullOrWhiteSpace(text) ? "" : " · " + text));
            AppendLog(logBox, "» " + Percent(percent) + " " + text);
        }

        internal void ShowDone()
        {
            summaryList.Children.Clear();
            var lines = new List<string>
            {
                askedApps == 0 ? T("No app uninstalled") : askedApps == 1 ? T("1 downloaded app uninstalled") : T("{0} downloaded apps uninstalled", askedApps),
                askedComponents == 1 ? T("1 Samsung component turned off") : T("{0} Samsung components turned off", askedComponents)
            };
            if (askedYouTube) lines.Add(T("Samsung's YouTube uninstalled"));
            lines.Add(T("Ads, tracking and Bixby turned off"));
            lines.Add(T("Ad filter on"));
            lines.Add(T("Installation leftovers cleaned up"));
            lines.Add(T("Smart Hub no longer opens by itself"));
            foreach (string s in lines)
            {
                var g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
                g.ColumnDefinitions.Add(new ColumnDefinition());
                var check = Ui.Glyph("", 13, Ui.Brush("Green"));
                check.HorizontalAlignment = HorizontalAlignment.Left;
                check.Margin = new Thickness(2, 0, 0, 0);
                g.Children.Add(check);
                var tb = Ui.Text(s, "Body");
                Grid.SetColumn(tb, 1);
                g.Children.Add(tb);
                summaryList.Children.Add(g);
            }
            plan = null;
            SetState("done");
        }

        internal void Failed(string message)
        {
            failText.Text = string.IsNullOrWhiteSpace(message) ? T("The optimization did not finish.") : message;
            SetState("fail");
            details.IsExpanded = true;
        }
    }
}
