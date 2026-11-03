using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace APL.Setup
{
    /// <summary>The numbered steps of a running script (install: 5, restore: 3) with their live state.</summary>
    internal sealed class StepList
    {
        private sealed class Row
        {
            public Grid Mark;
            public TextBlock Title, Detail;
            public string Description;
            public Status State;
        }

        private readonly List<Row> rows = new List<Row>();

        internal StepList(StackPanel host, string[] titles, string[] descriptions)
        {
            host.Children.Clear();
            for (int i = 0; i < titles.Length; i++)
            {
                if (i > 0) host.Children.Add(Ui.Separator(new Thickness(38, 0, 0, 0)));
                var g = new Grid { MinHeight = 54 };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
                g.ColumnDefinitions.Add(new ColumnDefinition());
                var mark = new Grid { Width = 24, Height = 24, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
                g.Children.Add(mark);
                var texts = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 8, 0, 8) };
                var title = Ui.Text(titles[i], "Label");
                var detail = Ui.Text(descriptions[i], "Secondary", new Thickness(0, 1, 0, 0));
                detail.TextWrapping = TextWrapping.NoWrap;
                detail.TextTrimming = TextTrimming.CharacterEllipsis;
                texts.Children.Add(title);
                texts.Children.Add(detail);
                Grid.SetColumn(texts, 1);
                g.Children.Add(texts);
                host.Children.Add(g);
                rows.Add(new Row { Mark = mark, Title = title, Detail = detail, Description = descriptions[i] });
            }
            Reset();
        }

        internal int Count { get { return rows.Count; } }

        internal void Reset()
        {
            for (int i = 0; i < rows.Count; i++) Set(i + 1, Status.Pending);
        }

        /// <summary>Step n (1-based) starts: the ones before it are done.</summary>
        internal void Start(int n)
        {
            for (int i = 1; i < n && i <= rows.Count; i++) if (rows[i - 1].State != Status.Ok) Set(i, Status.Ok);
            Set(n, Status.Busy);
        }

        internal void Set(int n, Status s, string detail = null)
        {
            if (n < 1 || n > rows.Count) return;
            var r = rows[n - 1];
            r.State = s;
            r.Mark.Children.Clear();
            r.Mark.Children.Add(Ui.Badge(s, 24, n.ToString()));
            r.Title.Foreground = Ui.Brush(s == Status.Pending ? "Text2" : "Text");
            // A failed step keeps the last thing it said: the closest hint to what went wrong.
            if (detail != null) r.Detail.Text = detail;
            else if (s != Status.Fail) r.Detail.Text = r.Description;
        }

        /// <summary>The step that is running (or failed), 0 if none.</summary>
        internal int Current
        {
            get
            {
                for (int i = 0; i < rows.Count; i++) if (rows[i].State == Status.Busy || rows[i].State == Status.Fail) return i + 1;
                return 0;
            }
        }

        internal void SetLive(string text)
        {
            int n = Current;
            if (n > 0 && rows[n - 1].State == Status.Busy && !string.IsNullOrWhiteSpace(text)) rows[n - 1].Detail.Text = text.Trim();
        }

        internal void Fail(string detail = null)
        {
            int n = Current;
            if (n == 0) n = 1;
            Set(n, Status.Fail, detail);
        }

        internal void AllDone()
        {
            for (int i = 1; i <= rows.Count; i++) Set(i, Status.Ok);
        }
    }
}
