using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class MqttMessageHandlerService : IMqttMessageHandlerService
    {
        private readonly ITemplateService _templates;
        private readonly ILogger<MqttMessageHandlerService> _logger;
        public MqttMessageHandlerService(ILogger<MqttMessageHandlerService> logger, ITemplateService templates) 
        {
            _logger = logger;
            _templates = templates;
        }

        public PointData HandleCommand(Command command)
        {
            var snapshot = _templates.Snapshot;
            if (snapshot == null)
            {
                _logger.LogWarning("Templates not loaded. Cannot process command");
                return null;
            }
            var template = snapshot.Commands.FirstOrDefault(x => x.Id == command.Id);
            if(template == null)
                return null;

            switch (command.Value.Type)
            {
                case Core.ValueType.Boolean:
                    return PointData.Measurement(template.Name)
                        .Tag("message", "command")
                        .Tag("device", template.Device)
                        .Tag("node", template.Node)
                        .Tag("id", command.Id)
                        .Field("value", command.Value.GetValue<bool>())
                        .Timestamp(DateTime.UtcNow, WritePrecision.Ms);

                case Core.ValueType.Number:
                    return PointData.Measurement(template.Name)
                        .Tag("message", "command")
                        .Tag("device", template.Device)
                        .Tag("node", template.Node)
                        .Tag("id", command.Id)
                        .Field("value", NumericValue(command.Value))
                        .Timestamp(DateTime.UtcNow, WritePrecision.Ms);

                case Core.ValueType.Entity:
                    return BuildEntityPoint(template.Name, "command", template.Device, template.Node, command.Id, command.Value.GetAsObject(), DateTime.UtcNow);

                default:
                    return null;
            }
        }

        public PointData HandleReport(Report report)
        {
            var snapshot = _templates.Snapshot;
            if (snapshot == null)
            {
                _logger.LogWarning("Templates not loaded. Cannot process report");
                return null;
            }

            var template = snapshot.Reports.FirstOrDefault(x => x.Id == report.Id);
            if (template == default)
                template = snapshot.Variables.FirstOrDefault(x => x.Id == report.Id);

            if (template == default)
                return null;

            var timestamp = DateTimeOffset.FromUnixTimeSeconds(report.TimeStamp).UtcDateTime;
            switch (report.Value.Type)
            {
                case Core.ValueType.Boolean:
                    return PointData.Measurement(template.Name)
                        .Tag("message", "report")
                        .Tag("device", template.Device)
                        .Tag("node", template.Node)
                        .Tag("id", report.Id)
                        .Field("value", report.Value.GetValue<bool>())
                        .Timestamp(timestamp, WritePrecision.Ms);

                case Core.ValueType.Number:
                    return PointData.Measurement(template.Name)
                        .Tag("message", "report")
                        .Tag("device", template.Device)
                        .Tag("node", template.Node)
                        .Tag("id", report.Id)
                        .Field("value", NumericValue(report.Value))
                        .Timestamp(timestamp, WritePrecision.Ms);

                case Core.ValueType.Entity:
                    return BuildEntityPoint(template.Name, "report", template.Device, template.Node, report.Id, report.Value.GetAsObject(), timestamp);

                default:
                    return null;
            }
        }

        private static double NumericValue(ValueModel value)
        {
            using var document = System.Text.Json.JsonDocument.Parse(value.ToJson());
            var number = document.RootElement.GetDouble();
            if (!double.IsFinite(number))
                throw new InvalidDataException("InfluxDB numeric fields must be finite.");
            return number;
        }

        private PointData BuildEntityPoint(string measurement, string message, string device, string node, string id, object entity, DateTime timestamp)
        {
            var fields = EntityFlattener.Flatten(entity).ToList();
            if (!fields.Any())
            {
                _logger.LogWarning("Entity {id} did not contain any boolean or number values to write", id);
                return null;
            }

            var point = PointData.Measurement(measurement)
                .Tag("message", message)
                .Tag("device", device)
                .Tag("node", node)
                .Tag("id", id)
                .Timestamp(timestamp, WritePrecision.Ms);

            foreach (var (path, value) in fields)
                point = AddField(point, path, value);

            return point;
        }

        private static PointData AddField(PointData point, string field, object value)
        {
            return value switch
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
}