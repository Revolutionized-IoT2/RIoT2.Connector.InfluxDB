namespace RIoT2.Connector.InfluxDB.Models;

public sealed record TemplateSnapshot(
    IReadOnlyList<Template> Reports,
    IReadOnlyList<Template> Commands,
    IReadOnlyList<Template> Variables);
