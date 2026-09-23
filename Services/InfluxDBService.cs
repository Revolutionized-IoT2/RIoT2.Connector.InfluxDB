using InfluxDB.Client;
using InfluxDB.Client.Writes;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using System.Threading.Channels;

namespace RIoT2.Connector.InfluxDB.Services
{
    public sealed class InfluxDBService : IInfluxDBService, IHostedService, IAsyncDisposable
    {
        private readonly InfluxDBClient _client;
        private readonly IWriteApiAsync _writer;
        private readonly string _bucket;
        private readonly string _org;
        private readonly ILogger<InfluxDBService> _logger;
        private readonly Channel<PointData> _queue = Channel.CreateBounded<PointData>(new BoundedChannelOptions(1000)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        private readonly CancellationTokenSource _shutdown = new();
        private Task _worker = Task.CompletedTask;
        private int _started;
        private int _disposed;

        public InfluxDBService(IConnectorConfigurationService configuration, ILogger<InfluxDBService> logger)
        {
            _client = new InfluxDBClient(configuration.Configuration.InfluxHost, configuration.Configuration.InfluxToken);
            _writer = _client.GetWriteApiAsync();
            _bucket = configuration.Configuration.InfluxBucket;
            _org = configuration.Configuration.InfluxOrganization;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (Interlocked.Exchange(ref _started, 1) != 0)
                throw new InvalidOperationException("The InfluxDB writer has already been started.");
            _worker = ProcessWritesAsync();
            return Task.CompletedTask;
        }

        public void Write(PointData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (Volatile.Read(ref _started) == 0)
                throw new InvalidOperationException("The InfluxDB writer has not been started.");
            if (!_queue.Writer.TryWrite(data))
                throw new InvalidOperationException("The InfluxDB write queue is full or stopping; the point was not accepted.");
        }

        private async Task ProcessWritesAsync()
        {
            try
            {
                while (await _queue.Reader.WaitToReadAsync(_shutdown.Token))
                {
                    var batch = new List<PointData>(100);
                    while (batch.Count < 100 && _queue.Reader.TryRead(out var point))
                        batch.Add(point);
                    try
                    {
                        await _writer.WritePointsAsync(batch, _bucket, _org, _shutdown.Token);
                    }
                    catch (Exception) when (_shutdown.IsCancellationRequested)
                    {
                        _logger.LogWarning("InfluxDB shutdown interrupted a batch of {Count} points; delivery is unknown and is not replayed", batch.Count);
                        break;
                    }
                    catch (Exception error)
                    {
                        _logger.LogError(error, "InfluxDB write failed for {Count} points; no automatic retry", batch.Count);
                    }
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                // Shutdown cancels an idle reader as well as in-flight HTTP.
            }
            finally
            {
                var abandoned = 0;
                while (_queue.Reader.TryRead(out _))
                    abandoned++;
                if (abandoned > 0)
                    _logger.LogWarning("Discarded {Count} queued InfluxDB points during shutdown; no automatic replay", abandoned);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _queue.Writer.TryComplete();
            try
            {
                await _worker.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _shutdown.Cancel();
                await _worker;
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try { await StopAsync(timeout.Token); }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested)
                {
                    _logger.LogWarning("InfluxDB writer exceeded its five-second disposal deadline");
                }
            }
            finally
            {
                _client.Dispose();
                _shutdown.Dispose();
            }
        }
    }
}