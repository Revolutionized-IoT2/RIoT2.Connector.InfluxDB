using InfluxDB.Client;
using InfluxDB.Client.Core.Exceptions;
using InfluxDB.Client.Writes;
using RIoT2.Connector.Sdk;

namespace RIoT2.Connector.InfluxDB.Services
{
    /// <summary>
    /// Writes the connector's batches to InfluxDB (the reference <see cref="IConnectorSink"/>, design 7.3). Data InfluxDB
    /// refuses (400, 413, 422) is rejected; anything else that fails (network, timeouts, 401/403/404, 429, 5xx) is
    /// transient, so the batch is retried once InfluxDB or its settings are fixed. Until the templates are loaded,
    /// every batch is transient too: messages wait for the orchestrator instead of being dropped.
    /// </summary>
    public sealed class InfluxSink : IConnectorSink, IDisposable
    {
        private readonly InfluxDBClient _client;
        private readonly IWriteApiAsync _writer;
        private readonly string _bucket;
        private readonly string _organization;
        private readonly InfluxPointMapper _mapper;
        private readonly ILogger<InfluxSink> _logger;

        public InfluxSink(InfluxConnectorOptions settings, ILogger<InfluxSink> logger)
        {
            ArgumentNullException.ThrowIfNull(settings);
            _client = new InfluxDBClient(settings.InfluxHost, settings.InfluxToken);
            _writer = _client.GetWriteApiAsync();
            _bucket = settings.InfluxBucket;
            _organization = settings.InfluxOrganization;
            _logger = logger;
            _mapper = new InfluxPointMapper(logger);
        }

        public async Task WriteAsync(IReadOnlyList<ConnectorMessage> batch, ITemplateCatalog templates, CancellationToken cancellationToken)
        {
            var snapshot = templates.Snapshot
                ?? throw new TransientSinkException("The templates are not loaded yet; waiting for the orchestrator's configuration command.");
            var points = new List<PointData>(batch.Count);
            foreach (var message in batch)
            {
                PointData? point = null;
                try
                {
                    point = _mapper.Map(message, snapshot);
                }
                // A message the mapper can't handle is skipped on its own; letting it escape would drop the whole batch.
                catch (Exception error) when (error is not OperationCanceledException)
                {
                    _logger.LogWarning(error, "Could not map a {Kind} from node {Node}", message.Kind, message.NodeId);
                }
                if (point == null)
                    ConnectorMetrics.RecordIgnored(message);
                else
                    points.Add(point);
            }
            if (points.Count == 0)
                return;

            try
            {
                await _writer.WritePointsAsync(points, _bucket, _organization, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpException error) when (error.Status is 400 or 413 or 422)
            {
                throw new SinkRejectedException($"InfluxDB refused {points.Count} points: HTTP {error.Status} {error.Message}", error);
            }
            catch (Exception error) when (error is InfluxException or HttpRequestException or IOException or OperationCanceledException)
            {
                throw new TransientSinkException($"Writing {points.Count} points to InfluxDB failed: {error.Message}", error);
            }
        }

        /// <summary>InfluxDB's <c>/ping</c> (for <c>/health</c>); false when it doesn't answer.</summary>
        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _client.PingAsync().WaitAsync(cancellationToken);
            }
            catch (Exception error) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug(error, "InfluxDB ping failed");
                return false;
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
