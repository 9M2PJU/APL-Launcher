using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using static APL.Setup.Strings;

namespace APL.Setup
{
    internal static class Theme
    {
        internal static void Load(Application app)
        {
            app.Resources.MergedDictionaries.Add((ResourceDictionary)Ui.LoadXaml("Styles.xaml"));
            // Hyperlinks: web addresses open in the browser, "apl:<page>" moves inside the app.
            EventManager.RegisterClassHandler(typeof(Hyperlink), Hyperlink.RequestNavigateEvent,
                new RequestNavigateEventHandler((s, e) => { Ui.Follow(e.Uri.OriginalString); e.Handled = true; }));
        }
    }

    internal enum Status { None, Ok, Warn, Fail, Busy, Pending }

    /// <summary>Small helpers to build the dynamic parts of the pages with the shared styles.</summary>
    internal static class Ui
    {
        /// <summary>Handles "apl:…" links (set by the main window).</summary>
        internal static Action<string> InternalLink;

        internal static object LoadXaml(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (s == null) throw new InvalidOperationException("Missing resource " + name);
                return XamlReader.Load(s);
            }
        }

        internal static T Find<T>(FrameworkElement root, string name) where T : class
        {
            var o = root.FindName(name) as T;
            if (o == null) throw new InvalidOperationException("Missing element " + name + " (" + typeof(T).Name + ")");
            return o;
        }

        internal static T Res<T>(string key) where T : class { return (T)Application.Current.FindResource(key); }
        internal static Brush Brush(string key) { return Res<Brush>("Brush." + key); }

        internal static void Follow(string target)
        {
            if (string.IsNullOrEmpty(target)) return;
            if (target.StartsWith("apl:", StringComparison.Ordinal)) { if (InternalLink != null) InternalLink(target.Substring(4)); return; }
            Open(target);
        }

        internal static void Open(string urlOrFile)
        {
            try { Process.Start(new ProcessStartInfo(urlOrFile) { UseShellExecute = true }); }
            catch (Exception ex) { Log.Error("open " + urlOrFile, ex); }
        }

        internal static BitmapImage AppIcon()
        {
            var img = new BitmapImage();
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("icon.png"))
            {
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = s;
                img.EndInit();
            }
            img.Freeze();
            return img;
        }

        // ---------- translation of the XAML pages (written in English) ----------

        internal static void Translate(object o)
        {
            var tb = o as TextBlock;
            if (tb != null) { TranslateInlines(tb.Inlines); return; }
            var cc = o as ContentControl;
            if (cc != null && cc.Content is string) cc.Content = T((string)cc.Content);
            var hc = o as HeaderedContentControl;
            if (hc != null && hc.Header is string) hc.Header = T((string)hc.Header);
            var box = o as TextBox;
            if (box != null && box.Tag is string) box.Tag = T((string)box.Tag);
            var fe = o as FrameworkElement;
            if (fe != null && fe.ToolTip is string) fe.ToolTip = T((string)fe.ToolTip);
            var d = o as DependencyObject;
            if (d == null || o is TextBox) return;
            foreach (object child in LogicalTreeHelper.GetChildren(d)) Translate(child);
        }

        private static void TranslateInlines(InlineCollection inlines)
        {
            foreach (var inl in new System.Collections.Generic.List<Inline>(inlines))   // a copy: setting Run.Text can rebuild the collection
            {
                var run = inl as Run;
                if (run != null) { run.Text = T(run.Text); continue; }
                var span = inl as Span;
                if (span != null) TranslateInlines(span.Inlines);
            }
        }

        // ---------- rich text: **bold** and [link](https://… or apl:page) ----------

        internal static void Rich(TextBlock tb, string text)
        {
            tb.Inlines.Clear();
            foreach (Match m in Regex.Matches(text, @"\*\*(.+?)\*\*|\[(.+?)\]\((.+?)\)|[^*\[]+|[*\[]"))
            {
                if (m.Groups[1].Success)
                    tb.Inlines.Add(new Run(m.Groups[1].Value) { FontWeight = FontWeights.SemiBold, Foreground = Brush("Text") });
                else if (m.Groups[2].Success)
                    tb.Inlines.Add(Link(m.Groups[2].Value, m.Groups[3].Value));
                else
                    tb.Inlines.Add(new Run(m.Value));
            }
        }

        internal static Hyperlink Link(string text, string target)
        {
            var h = new Hyperlink(new Run(text));
            try { h.NavigateUri = new Uri(target, UriKind.RelativeOrAbsolute); } catch { }
            return h;
        }

        // ---------- element factories ----------

        internal static TextBlock Text(string text, string style, Thickness? margin = null)
        {
            var tb = new TextBlock { Style = Res<Style>("Text." + style) };
            if (text.Contains("**") || text.Contains("](")) Rich(tb, text); else tb.Text = text;
            if (margin.HasValue) tb.Margin = margin.Value;
            return tb;
        }

        internal static TextBlock Glyph(string glyph, double size, Brush color)
        {
            return new TextBlock { Text = glyph, FontFamily = Res<FontFamily>("Font.Icons"), FontSize = size, Foreground = color,
                                   HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }

        /// <summary>White glyph on a colored rounded square.</summary>
        internal static Border Tile(string glyph, string color, double size = 30)
        {
            return new Border
            {
                Width = size, Height = size, CornerRadius = new CornerRadius(size * 0.27), Background = Brush(color),
                VerticalAlignment = VerticalAlignment.Top, Child = Glyph(glyph, size * 0.47, Brushes.White)
            };
        }

        internal static Control Spinner(double size = 18, Brush color = null)
        {
            return new Control { Template = Res<ControlTemplate>("Spinner"), Width = size, Height = size,
                                 Foreground = color ?? Brush("Text2"), VerticalAlignment = VerticalAlignment.Center, Focusable = false };
        }

        /// <summary>Round status mark: green check, orange !, red cross, spinner, or an empty ring with a number.</summary>
        internal static FrameworkElement Badge(Status s, double size = 22, string number = null)
        {
            if (s == Status.Busy) return Spinner(size - 2);
            if (s == Status.Pending || s == Status.None)
            {
                var ring = new Border
                {
                    Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), BorderThickness = new Thickness(1.5),
                    BorderBrush = Brush("InputBorder"), VerticalAlignment = VerticalAlignment.Center
                };
                if (number != null)
                    ring.Child = new TextBlock { Text = number, FontSize = size * 0.5, FontWeight = FontWeights.SemiBold, Foreground = Brush("Text3"),
                                                 HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                return ring;
            }
            string color = s == Status.Ok ? "Green" : s == Status.Warn ? "Orange" : "Red";
            var b = new Border { Width = size, Height = size, CornerRadius = new CornerRadius(size / 2), Background = Brush(color), VerticalAlignment = VerticalAlignment.Center };
            if (s == Status.Warn)
                b.Child = new TextBlock { Text = "!", FontSize = size * 0.62, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                                          HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                                          Margin = new Thickness(0, 0, 0, size * 0.04) };
            else
            {
                var g = Glyph(s == Status.Ok ? "" : "", size * (s == Status.Ok ? 0.52 : 0.44), Brushes.White);
                g.FontWeight = FontWeights.Bold;
                b.Child = g;
            }
            return b;
        }

        internal static Ellipse Dot(string color, double size = 8)
        {
            return new Ellipse { Width = size, Height = size, Fill = Brush(color), VerticalAlignment = VerticalAlignment.Center };
        }

        internal static Border Separator(Thickness margin)
        {
            return new Border { Style = Res<Style>("Separator"), Margin = margin };
        }

        internal static Border Pill(string text)
        {
            return new Border
            {
                Background = Brush("Pill"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 5, 10, 6), Margin = new Thickness(0, 0, 6, 6),
                Child = new TextBlock { Text = text, FontSize = 13, Foreground = Brush("Text"), TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 320 }
            };
        }

        internal static Button Button(string text, string style, Action click)
        {
            var b = new Button { Content = text, Style = Res<Style>("Button." + style) };
            if (click != null) b.Click += (s, e) => click();
            return b;
        }

        /// <summary>Glides a progress bar to its new value instead of jumping.</summary>
        internal static void SetProgress(ProgressBar bar, double value)
        {
            var anim = new DoubleAnimation(value, TimeSpan.FromMilliseconds(350)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            bar.BeginAnimation(RangeBase.ValueProperty, anim);
        }

        internal static void Show(UIElement e, bool visible) { e.Visibility = visible ? Visibility.Visible : Visibility.Collapsed; }

    }
}
