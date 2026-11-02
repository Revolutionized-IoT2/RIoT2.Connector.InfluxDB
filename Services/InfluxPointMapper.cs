using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using RIoT2.Connector.Sdk;
using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// Maps a received report or command to an InfluxDB point: the template's name is the measurement; <c>message</c>,
    /// <c>device</c>, <c>node</c> and <c>id</c> are tags; booleans and numbers are the <c>value</c> field, and an entity's
    /// booleans and numbers are fields named by their path. Reports keep their own time (Unix seconds), commands use the
    /// time the connector received them. Text values are not written. The line protocol for every golden message is
    /// pinned by RIoT2.Tests (<c>Golden/influx-line-protocol.txt</c>).
    /// </summary>
    public sealed class InfluxPointMapper
    {
        private readonly ILogger _logger;

        public InfluxPointMapper(ILogger logger) => _logger = logger;

        /// <summary>The point, or <c>null</c> when the message has no template or nothing to write.</summary>
        public PointData? Map(ConnectorMessage message, TemplateSnapshot templates)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(templates);
            return message.Kind == ConnectorMessageKind.Command
                ? MapCommand(Command.Create(message.Json), message.ReceivedUtc.UtcDateTime, templates)
                : MapReport(Report.Create(message.Json), templates);
        }

        private PointData? MapCommand(Command? command, DateTime receivedUtc, TemplateSnapshot templates)
        {
            if (command == null)
                return null;
            var template = templates.FindCommand(command.Id);
            return template == null ? null : Point(template, "command", command.Id, command.Value, receivedUtc);
        }

        private PointData? MapReport(Report? report, TemplateSnapshot templates)
        {
            if (report == null)
                return null;
            var template = templates.FindReport(report.Id);
            if (template == null)
                return null;
            if (report.TimeStamp < MinUnixSeconds || report.TimeStamp > MaxUnixSeconds)
                throw new InvalidDataException($"Report {report.Id} has a time stamp out of range: {report.TimeStamp} (Unix seconds expected).");
            return Point(template, "report", report.Id, report.Value, DateTimeOffset.FromUnixTimeSeconds(report.TimeStamp).UtcDateTime);
        }

        private static readonly long MinUnixSeconds = DateTimeOffset.MinValue.ToUnixTimeSeconds();
        private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        private PointData? Point(ConnectorTemplate template, string message, string id, ValueModel? value, DateTime timestamp)
        {
            if (value == null)
                return null;
            switch (value.Type)
            {
                case Core.ValueType.Boolean:
                    return Tags(template, message, id).Field("value", value.GetValue<bool>()).Timestamp(timestamp, WritePrecision.Ms);
                case Core.ValueType.Number:
                    return Tags(template, message, id).Field("value", NumericValue(value)).Timestamp(timestamp, WritePrecision.Ms);
                case Core.ValueType.Entity:
                    var fields = EntityFlattener.Flatten(value.GetAsObject()).ToList();
                    if (fields.Count == 0)
                    {
                        _logger.LogWarning("Entity {Id} did not contain any boolean or number values to write", id);
                        return null;
                    }
                    var point = Tags(template, message, id).Timestamp(timestamp, WritePrecision.Ms);
                    foreach (var (path, field) in fields)
                        point = AddField(point, path, field);
                    return point;
                default:
                    return null;
            }
        }

        private static PointData Tags(ConnectorTemplate template, string message, string id) =>
            PointData.Measurement(template.Name)
                .Tag("message", message)
                .Tag("device", template.Device)
                .Tag("node", template.Node)
                .Tag("id", id);

        private static double NumericValue(ValueModel value)
        {
            using var document = System.Text.Json.JsonDocument.Parse(value.ToJson());
            var number = document.RootElement.GetDouble();
            if (!double.IsFinite(number))
                throw new InvalidDataException("InfluxDB numeric fields must be finite.");
            return number;
        }

        private static PointData AddField(PointData point, string field, object value) => value switch
        {
            bool b => point.Field(field, b),
            int i => point.Field(field, i),
            long l => point.Field(field, l),
            float f => point.Field(field, (double)f),
            double d => point.Field(field, d),
            decimal m => point.Field(field, (double)m),
            _ => point
        };
    }
}
