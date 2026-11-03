using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace APL.Setup
{
    internal sealed class VetroInfo
    {
        public string Ip;
        public string Version;
        public string Model;
        /// <summary>False when /status says Vetro's root agent is not connected yet (the scripts need it).</summary>
        public bool AgentReady = true;
    }

    /// <summary>
    /// The only network traffic of the app itself: GET http://&lt;ip&gt;:8099/status.
    /// No proxy (a corporate proxy cannot reach the screen and must not see local addresses).
    /// </summary>
    internal static class Vetro
    {
        internal const int Port = 8099;
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
        {
            Timeout = Timeout.InfiniteTimeSpan   // each call brings its own deadline
        };

        internal static bool IsIPv4(string text)
        {
            if (text == null) return false;
            var m = Regex.Match(text.Trim(), @"^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$");
            if (!m.Success) return false;
            for (int i = 1; i <= 4; i++) if (int.Parse(m.Groups[i].Value) > 255) return false;
            return true;
        }

        internal static string Url(string ip) { return "http://" + ip.Trim() + ":" + Port; }

        /// <summary>Null when nothing answers like Vetro within the deadline.</summary>
        internal static async Task<VetroInfo> StatusAsync(string ip, int timeoutMs, CancellationToken cancel)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                cts.CancelAfter(timeoutMs);
                try
                {
                    using (var resp = await Http.GetAsync(Url(ip) + "/status", HttpCompletionOption.ResponseContentRead, cts.Token).ConfigureAwait(false))
                    {
                        if (resp.StatusCode != HttpStatusCode.OK) return null;
                        string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                        object json;
                        if (body.Length > 200000 || !Json.TryParse(body, out json) || !(json is Dictionary<string, object>)) return null;
                        var info = new VetroInfo { Ip = ip.Trim(), Version = Json.Str(json, "version") };
                        object agent = Json.Get(json, "agent");
                        if (agent is Dictionary<string, object> && Json.Get(agent, "connected") is bool)
                            info.AgentReady = Json.Bool(agent, "connected");
                        info.Model = Json.Str(json, "model") ?? Json.Str(json, "modelName") ?? Json.Str(json, "deviceName");
                        return info;
                    }
                }
                catch { return null; }
            }
        }

        /// <summary>The /24 networks of this PC's active private IPv4 addresses (Wi-Fi and Ethernet first).</summary>
        internal static List<string> LocalPrefixes()
        {
            var ordered = new List<KeyValuePair<int, string>>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up) continue;
                    if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback || nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;
                    var props = nic.GetIPProperties();
                    bool gateway = props.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));
                    string desc = (nic.Description + " " + nic.Name).ToLowerInvariant();
                    bool virtualNic = desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("vethernet") || desc.Contains("wsl");
                    foreach (var ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                        byte[] b = ua.Address.GetAddressBytes();
                        bool isPrivate = b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168);
                        if (!isPrivate) continue;
                        string prefix = b[0] + "." + b[1] + "." + b[2] + ".";
                        int rank = (gateway ? 0 : 2) + (virtualNic ? 1 : 0);
                        if (!ordered.Any(p => p.Value == prefix)) ordered.Add(new KeyValuePair<int, string>(rank, prefix));
                    }
                }
            }
            catch (Exception ex) { Log.Error("network interfaces", ex); }
            return ordered.OrderBy(p => p.Key).Select(p => p.Value).ToList();
        }

        /// <summary>Asks every address of the local /24 networks, 64 at a time, ~600 ms each.</summary>
        internal static async Task<List<VetroInfo>> ScanAsync(IProgress<double> progress, CancellationToken cancel)
        {
            var targets = new List<string>();
            foreach (string prefix in LocalPrefixes().Take(4))
                for (int i = 1; i <= 254; i++) targets.Add(prefix + i);
            var found = new List<VetroInfo>();
            if (targets.Count == 0) return found;
            int done = 0;
            using (var gate = new SemaphoreSlim(64))
            {
                var tasks = targets.Select(async ip =>
                {
                    await gate.WaitAsync(cancel).ConfigureAwait(false);
                    try
                    {
                        var info = await StatusAsync(ip, 600, cancel).ConfigureAwait(false);
                        if (info != null) lock (found) found.Add(info);
                    }
                    finally
                    {
                        gate.Release();
                        int n = Interlocked.Increment(ref done);
                        if (progress != null) progress.Report((double)n / targets.Count);
                    }
                }).ToList();
                try { await Task.WhenAll(tasks).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }
            return found.OrderBy(f => f.Ip.Length).ThenBy(f => f.Ip, StringComparer.Ordinal).ToList();
        }
    }
}
