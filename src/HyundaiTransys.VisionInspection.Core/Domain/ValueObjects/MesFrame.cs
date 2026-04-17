namespace HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

/// <summary>
/// Parsed representation of an MES frame.
/// Raw wire format: STX field1;field2;field3;... ETX
/// </summary>
public sealed record MesFrame(
    string RawPayload,
    IReadOnlyList<string> Fields,
    string ModelCode,
    string SerialNumber,
    DateTimeOffset ReceivedAt)
{
    public const char Stx = (char)0x02;
    public const char Etx = (char)0x03;
    public const char Separator = ';';
}
