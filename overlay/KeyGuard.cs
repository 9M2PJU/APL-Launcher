using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace APL
{
    /// <summary>
    /// Vetro's input helper restarts by itself now and then. A key held at the moment it takes the
    /// remote back stays down for the screen, which never hears the release: the app gets that key
    /// every 108 ms until another arrow is pressed, and every later press of that key sticks too.
    /// The helper writes a token in /tmp as it starts. That wakes this class, which polls nothing
    /// the rest of the time.
    /// </summary>
    internal sealed class KeyGuard
    {
        private const string TokenFolder = "/tmp", TokenFiles = "vetro-input-*";
        private const int WatchMs = 20000, EveryMs = 1000, IdleMs = 1000, IdleLimitMs = 15000;
        private const int MaxReleases = 3, ReleaseWindowMinutes = 10;

        private readonly List<DateTime> releases = new List<DateTime>();
        private FileSystemWatcher tokens;
        private DateTime releasedAt = DateTime.MinValue;
        private long watchUntil;
        private int watching;

        public void Start()
        {
            try
            {
                tokens = new FileSystemWatcher(TokenFolder, TokenFiles);
                tokens.NotifyFilter = NotifyFilters.FileName;
                tokens.Created += delegate { Watch(); };
                tokens.EnableRaisingEvents = true;
            }
            catch (Exception ex) { Program.Note("key guard: " + ex.Message); }
            Watch();    // the helper may have restarted before this app did
        }

        private async void Watch()
        {
            Interlocked.Exchange(ref watchUntil, DateTime.UtcNow.AddMilliseconds(WatchMs).Ticks);
            if (Interlocked.Exchange(ref watching, 1) == 1) return;
            try
            {
                while (DateTime.UtcNow.Ticks < Interlocked.Read(ref watchUntil))
                {
                    await Task.Delay(EveryMs);
                    int code = RemoteKeys.StuckKey(await Vetro.KeyEvents(), releasedAt);
                    if (code >= 0 && await RemoteIsIdle()) await Release(code);
                }
            }
            catch (Exception ex) { Program.Note("key guard: " + ex.Message); }
            finally { Interlocked.Exchange(ref watching, 0); }
        }

        /// <summary>The release restarts the helper: it must not catch another press in turn.</summary>
        private static async Task<bool> RemoteIsIdle()
        {
            DateTime limit = DateTime.UtcNow.AddMilliseconds(IdleLimitMs);
            while (DateTime.UtcNow < limit)
            {
                List<Vetro.KeyEvent> events = await Vetro.KeyEvents();
                if (events.Count == 0) return false;
                Vetro.KeyEvent last = events[events.Count - 1];
                if (!last.Down && (DateTime.UtcNow - last.Time).TotalMilliseconds >= IdleMs) return true;
                await Task.Delay(100);
            }
            return false;
        }

        private async Task Release(int code)
        {
            DateTime now = DateTime.UtcNow;
            releasedAt = now;
            releases.RemoveAll(t => (now - t).TotalMinutes >= ReleaseWindowMinutes);
            if (releases.Count >= MaxReleases)
            {
                Program.Note("key " + code + " stuck: too many releases, leaving it");
                return;
            }
            releases.Add(now);
            Program.Note("key " + code + " stuck: releasing it");
            await Vetro.ReleaseKey(code);
        }
    }
}
