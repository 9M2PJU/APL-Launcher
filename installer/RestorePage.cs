using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static APL.Setup.Strings;

namespace APL.Setup
{
    internal sealed class RestorePage : Page
    {
        private readonly ScrollViewer scroll;
        private readonly StackPanel formView, runView;
        private readonly CheckBox uninstallToggle;
        private readonly Button restoreButton, retryButton, backButton, doneBack;
        private readonly TextBlock hint, overallText, failText, doneText;
        private readonly ProgressBar overallBar;
        private readonly Border doneCard, failCard;
        private readonly Expander details;
        private readonly TextBox logBox;
        private readonly StepList steps;
        private bool uninstallAsked;

        internal RestorePage(MainWindow main) : base(main, "restore", "RestorePage.xaml")
        {
            scroll = F<ScrollViewer>("Scroll");
            formView = F<StackPanel>("FormView");
            runView = F<StackPanel>("RunView");
            uninstallToggle = F<CheckBox>("UninstallToggle");
            restoreButton = F<Button>("RestoreButton");
            retryButton = F<Button>("RetryButton");
            backButton = F<Button>("BackButton");
            doneBack = F<Button>("DoneBack");
            hint = F<TextBlock>("Hint");
            overallText = F<TextBlock>("OverallText");
            failText = F<TextBlock>("FailText");
            doneText = F<TextBlock>("DoneText");
            overallBar = F<ProgressBar>("OverallBar");
            doneCard = F<Border>("DoneCard");
            failCard = F<Border>("FailCard");
            details = F<Expander>("Details");
            logBox = F<TextBox>("LogBox");
            F<Grid>("DoneMark").Children.Add(Ui.Badge(Status.Ok, 46));
            F<Grid>("FailMark").Children.Add(Ui.Badge(Status.Fail, 24));
            Ui.Rich(F<TextBlock>("ReminderPath"), T("Last thing, on the screen: open **Settings › All Settings › Advanced Features** and set:"));
            F<TextBlock>("SmartHubName").Text = T("Autorun Smart Hub");
            F<TextBlock>("LastAppName").Text = T("Autorun Last App");

            steps = new StepList(F<StackPanel>("Steps"),
                new[] { T("Home button"), T("APL Launcher and the optimization"), T("Samsung settings") },
                new[] { T("Given back to Samsung, key interception off"), T("The optimization is undone from its backup"), T("Two settings to set back on the screen") });

            restoreButton.Click += (s, e) => Run(true);
            retryButton.Click += (s, e) => Run(false);
            backButton.Click += (s, e) => ShowForm();
            doneBack.Click += (s, e) => ShowForm();
            ShowForm();
        }

        private void ShowForm()
        {
            Ui.Show(formView, true);
            Ui.Show(runView, false);
            SetHeader(false);
            UpdateActions();
        }

        internal override void UpdateActions()
        {
            bool busy = Main.Runner.Busy, ready = Main.ScreenReady;
            restoreButton.IsEnabled = !busy && ready;
            retryButton.IsEnabled = !busy && ready;
            if (!Vetro.IsIPv4(Main.Ip)) hint.Text = T("Choose the screen on the Install page first.");
            else if (!Main.VetroChecked) hint.Text = T("Looking for Vetro at {0}…", Main.Ip);
            else if (!ready) hint.Text = T("Vetro must answer at {0}.", Main.Ip);
            else if (busy) hint.Text = T("Wait for the current task to finish ({0}).", Main.Runner.TaskName);
            else hint.Text = T("Screen: {0}", Main.Ip);
        }

        private async void Run(bool confirm)
        {
            if (Main.Runner.Busy || !Main.ScreenReady) return;
            uninstallAsked = uninstallToggle.IsChecked == true;
            if (confirm)
            {
                var bullets = new List<string> { T("Home goes back to Samsung"), T("Key interception is turned off"), T("The optimization is undone") };
                if (uninstallAsked) bullets.Add(T("APL Launcher is uninstalled from the screen"));
                bool go = await Main.Ask(T("Restore the screen?"), T("The screen goes back to Samsung's menus. You can install APL Launcher again at any time."),
                                         uninstallAsked ? T("Restore and uninstall") : T("Restore"), danger: uninstallAsked,
                                         glyph: "", tileColor: "Indigo", bullets: bullets);
                if (!go) return;
            }
            var args = new List<string> { "-TvIp", Main.Ip, "-Unattended" };
            if (uninstallAsked) args.Add("-UninstallLauncher");
            BeginRun();
            ScriptResult r;
            try
            {
                r = await Main.Runner.Run("restore.ps1", args, T("Restoring…"), OnLine, null, true);
            }
            catch (Exception ex)
            {
                Log.Error("restore", ex);
                r = new ScriptResult { Ok = false, Message = ex.Message };
            }
            if (r.Ok) ShowDone(uninstallAsked); else Failed(r.Message);
        }

        internal void BeginRun()
        {
            steps.Reset();
            logBox.Clear();
            details.IsExpanded = false;
            overallBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, null);
            overallBar.Value = 0;
            overallText.Text = T("Starting…");
            Ui.Show(doneCard, false);
            Ui.Show(failCard, false);
            Ui.Show(doneBack, false);
            Ui.Show(formView, false);
            Ui.Show(runView, true);
            SetHeader(true);
            scroll.ScrollToTop();
        }

        private void OnLine(ScriptLine l)
        {
            switch (l.Kind)
            {
                case LineKind.Step:
                    SetStep(l.Number, l.Total);
                    AppendLog(logBox, "» " + l.Text);
                    break;
                case LineKind.Log:
                    if (l.Text.Trim().Length == 0) break;
                    AppendLog(logBox, l.Text);
                    steps.SetLive(l.Text);
                    break;
                case LineKind.Result:
                    if (!l.Ok) AppendLog(logBox, "✗ " + l.Text);
                    break;
            }
        }

        private void SetHeader(bool running)
        {
            F<TextBlock>("PageTitle").Text = running ? T("Restoring the screen") : T("Restore");
            F<TextBlock>("PageSubtitle").Text = running ? T("Keep the screen on. It takes less than a minute.")
                : T("Gives the screen back its Samsung behaviour, in case of trouble or before you stop using APL Launcher.");
        }

        internal void SetStep(int n, int total)
        {
            total = Math.Max(total, steps.Count);
            steps.Start(n);
            double pct = (n - 1) * 100.0 / total + 6;
            if (Main.Offline) overallBar.Value = pct; else Ui.SetProgress(overallBar, pct);
            overallText.Text = T("Step {0} of {1}", n, total);
            Main.SetSideDetail(T("Step {0} of {1}", n, total));
        }

        internal void ShowDone(bool uninstalled)
        {
            steps.AllDone();
            if (Main.Offline) overallBar.Value = 100; else Ui.SetProgress(overallBar, 100);
            overallText.Text = T("Done");
            doneText.Text = uninstalled ? T("Home opens Samsung's menu again, and APL Launcher was uninstalled.")
                                        : T("Home opens Samsung's menu again. APL Launcher stays installed, ready to come back.");
            Ui.Show(doneCard, true);
            Ui.Show(doneBack, true);
            SetHeader(false);
            Reveal(doneCard);
        }

        internal void Failed(string message)
        {
            steps.Fail();
            overallText.Text = T("Stopped");
            failText.Text = string.IsNullOrWhiteSpace(message) ? T("Restore did not finish.") : message;
            Ui.Show(failCard, true);
            details.IsExpanded = true;
            Reveal(failCard);
            UpdateActions();
        }
    }
}
