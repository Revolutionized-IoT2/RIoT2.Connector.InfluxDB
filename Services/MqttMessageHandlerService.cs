using InfluxDB3.Client.Write;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class MqttMessageHandlerService : IMqttMessageHandlerService
    {
        private readonly ITemplateService _templates;
        public MqttMessageHandlerService(ITemplateService templates) 
        {
            _templates = templates;
        }

        public PointData HandleCommand(Command command)
        {
            var template = _templates.CommandTemplates.FirstOrDefault(x => x.Id == command.Id);
            if(template == null)
                return null;

            switch (command.Value.Type)
            {
                case Core.ValueType.Boolean:
                    return PointData.Measurement(template.Name)
                        .SetTag("message", "command")
                        .SetTag("device", template.Device)
                        .SetTag("node", template.Node)
                        .SetTag("id", command.Id)
                        .SetField("value", command.Value.GetValue<bool>())
                        .SetTimestamp(DateTime.UtcNow);

                case Core.ValueType.Number:
                    return PointData.Measurement(template.Name)
                        .SetTag("message", "command")
                        .SetTag("device", template.Device)
                        .SetTag("node", template.Node)
                        .SetTag("id", command.Id)
                        .SetField("value", (command.Value.ToJson().Contains('.')) ? command.Value.GetValue<double>() : command.Value.GetValue<int>())
                        .SetTimestamp(DateTime.UtcNow);

                case Core.ValueType.Entity: //TODO implement later: traverse object and exctact booleans and numbers
                default:
                    return null;
            }
        }

        public PointData HandleReport(Report report)
        {
            var template = _templates.ReportTemplates.FirstOrDefault(x => x.Id == report.Id);
            if (template == default)
                template = _templates.VariableTemplates.FirstOrDefault(x => x.Id == report.Id);

            if (template == default)
                return null;

            switch (report.Value.Type)
            {
                case Core.ValueType.Boolean:
                    return PointData.Measurement(template.Name)
                        .SetTag("message", "report")
                        .SetTag("device", template.Device)
                        .SetTag("node", template.Node)
                        .SetTag("id", report.Id)
                        .SetField("value", report.Value.GetValue<bool>())
                        .SetTimestamp(DateTime.UtcNow);

                case Core.ValueType.Number:
                    return PointData.Measurement(template.Name)
                        .SetTag("message", "report")
                        .SetTag("device", template.Device)
                        .SetTag("node", template.Node)
                        .SetTag("id", report.Id)
                        .SetField("value", (report.Value.ToJson().Contains('.')) ? report.Value.GetValue<double>() : report.Value.GetValue<int>())
                        .SetTimestamp(DateTime.UtcNow);

                case Core.ValueType.Entity: //TODO implement later: traverse object and exctact booleans and numbers
                default:
                    return null;
            }
        }
    }
}