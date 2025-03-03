using RIoT2.Connector.InfluxDB.Models;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core.Utils;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class TemplateService : ITemplateService
    {

        public void Load(string orchestratorBaseUrl)
        {
            TemplatesLoaded = false;
            var reportGet = Web.GetAsync(orchestratorBaseUrl + "/api/nodes/report/templates");
            var commandGet = Web.GetAsync(orchestratorBaseUrl + "/api/nodes/command/templates");
            var variableGet = Web.GetAsync(orchestratorBaseUrl + "/api/nodes/variable/templates");
            Task.WaitAll(reportGet, commandGet, variableGet);

            if (reportGet.Result.IsSuccessStatusCode)
            {
                var reportTemplateJson = reportGet.Result.Content.ReadAsStringAsync().Result;
                ReportTemplates = Json.Deserialize<List<Template>>(reportTemplateJson);
                TemplatesLoaded = true;
            }

            if (commandGet.Result.IsSuccessStatusCode)
            {
                var commandTemplateJson = commandGet.Result.Content.ReadAsStringAsync().Result;
                CommandTemplates = Json.Deserialize<List<Template>>(commandTemplateJson);
                TemplatesLoaded = true;
            }

            if (variableGet.Result.IsSuccessStatusCode)
            {
                var variableTemplateJson = variableGet.Result.Content.ReadAsStringAsync().Result;
                VariableTemplates = Json.Deserialize<List<Template>>(variableTemplateJson);
                TemplatesLoaded = true;
            }
        }

        public List<Template> ReportTemplates { get; private set; }

        public List<Template> CommandTemplates { get; private set; }

        public List<Template> VariableTemplates { get; private set; }

        public bool TemplatesLoaded { get; private set; }
    }
}
