using RIoT2.Connector.InfluxDB.Models;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core.Utils;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _loadGate = new(1, 1);
        private TemplateSnapshot _snapshot;

        public TemplateService(HttpClient http) => _http = http;

        public TemplateSnapshot Snapshot => Volatile.Read(ref _snapshot);
        public bool TemplatesLoaded => Snapshot != null;
        public List<Template> ReportTemplates => Snapshot?.Reports.ToList() ?? [];
        public List<Template> CommandTemplates => Snapshot?.Commands.ToList() ?? [];
        public List<Template> VariableTemplates => Snapshot?.Variables.ToList() ?? [];

        public async Task LoadAsync(string orchestratorBaseUrl, CancellationToken cancellationToken = default)
        {
            await _loadGate.WaitAsync(cancellationToken);
            try
            {
                var root = orchestratorBaseUrl.TrimEnd('/');
                var reports = LoadListAsync(root + "/api/nodes/report/templates", cancellationToken);
                var commands = LoadListAsync(root + "/api/nodes/command/templates", cancellationToken);
                var variables = LoadListAsync(root + "/api/nodes/variable/templates", cancellationToken);
                await Task.WhenAll(reports, commands, variables);
                Volatile.Write(ref _snapshot, new TemplateSnapshot(
                    (await reports).AsReadOnly(), (await commands).AsReadOnly(), (await variables).AsReadOnly()));
            }
            finally
            {
                _loadGate.Release();
            }
        }

        private async Task<List<Template>> LoadListAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var templates = Json.Deserialize<List<Template>>(await response.Content.ReadAsStringAsync(cancellationToken));
            if (templates == null || templates.Any(t => t == null || string.IsNullOrWhiteSpace(t.Id) || string.IsNullOrWhiteSpace(t.Name)))
                throw new InvalidDataException($"Invalid template response from {url}.");
            return templates;
        }
    }
}
