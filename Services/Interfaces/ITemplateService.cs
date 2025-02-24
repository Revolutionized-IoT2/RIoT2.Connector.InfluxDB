using RIoT2.Connector.InfluxDB.Models;
using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface ITemplateService
    {
        void Load(string orchestratorBaseUrl);
        List<Template> ReportTemplates { get; }
        List<Template> CommandTemplates { get; }
        List<Template> VariableTemplates { get; }
    }
}
