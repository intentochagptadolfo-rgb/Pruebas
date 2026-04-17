using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

namespace HyundaiTransys.VisionInspection.Core.Abstractions;

public interface IMesMessageParser
{
    /// <summary>
    /// Parses a raw payload (already stripped of STX/ETX) into a domain frame.
    /// Throws <see cref="FormatException"/> if the layout is invalid.
    /// </summary>
    MesFrame Parse(string rawPayload);
}
