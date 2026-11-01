using System.Diagnostics.Metrics;
using RIoT2.Core.Diagnostics;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// The connector's instruments (meter <see cref="MeterNames.Connector"/>, design 7.4 phase 4). The
    /// spool metrics of the design come with the spool (connector SDK). Nothing is measured while no
    /// listener is attached.
    /// </summary>
    public static class ConnectorMetrics
    {
        public const string MessagesName = "riot2.connector.messages";
        public const string WritesName = "riot2.connector.writes";
        public const string QueueDepthName = "riot2.connector.queue_depth";

        /// <summary><see cref="Messages"/> results.</summary>
        public const string Queued = "queued";
        public const string Ignored = "ignored";
        public const string Invalid = "invalid";
        public const string Failed = "failed";

        /// <summary><see cref="Writes"/> outcomes.</summary>
        public const string Written = "written";
        public const string Discarded = "discarded";

        private static readonly Meter Meter = new(MeterNames.Connector);
        private static readonly List<WeakReference<InfluxDBService>> Writers = [];

        /// <summary>
        /// Reports and commands received, by <c>type</c> (<c>report</c> or <c>command</c>) and
        /// <c>result</c>: <c>queued</c> for InfluxDB, <c>ignored</c> (no template or no writable value),
        /// <c>invalid</c> (unreadable) or <c>failed</c> (could not be queued).
        /// </summary>
        public static readonly Counter<long> Messages = Meter.CreateCounter<long>(MessagesName, "{message}",
            "Reports and commands received, by type and result (queued, ignored, invalid or failed).");

        /// <summary>Points sent to InfluxDB, by <c>outcome</c>: <c>written</c>, <c>failed</c> or <c>discarded</c> (at shutdown).</summary>
        public static readonly Counter<long> Writes = Meter.CreateCounter<long>(WritesName, "{point}",
            "Points sent to InfluxDB, by outcome: written, failed or discarded at shutdown.");

        static ConnectorMetrics()
        {
            Meter.CreateObservableGauge(QueueDepthName, ObserveQueue, "{point}", "Points waiting to be written to InfluxDB.");
        }

        public static void Message(string type, string result) =>
            Messages.Add(1, new KeyValuePair<string, object>("type", type), new KeyValuePair<string, object>("result", result));

        public static void Write(string outcome, int points)
        {
            if (points > 0)
                Writes.Add(points, new KeyValuePair<string, object>("outcome", outcome));
        }

        /// <summary>Measures <paramref name="writer"/>'s queue in <c>riot2.connector.queue_depth</c> while it lives.</summary>
        public static void Track(InfluxDBService writer)
        {
            lock (Writers)
            {
                Writers.RemoveAll(reference => !reference.TryGetTarget(out _));
                Writers.Add(new WeakReference<InfluxDBService>(writer));
            }
        }

        private static IEnumerable<Measurement<int>> ObserveQueue()
        {
            lock (Writers)
            {
                var depth = 0;
                foreach (var reference in Writers)
                {
                    if (reference.TryGetTarget(out var writer))
                        depth += writer.QueueDepth;
                }
                return [new Measurement<int>(depth)];
            }
        }
    }
}
