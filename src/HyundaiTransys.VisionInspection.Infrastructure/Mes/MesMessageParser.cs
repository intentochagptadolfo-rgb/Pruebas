using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

namespace HyundaiTransys.VisionInspection.Infrastructure.Mes;

/// <summary>
/// Wire format (stripped of STX/ETX by the transport layer):
///   field0;field1;field2;...
/// Convention (adjust to the real MES spec):
///   field0 = MODEL_CODE
///   field1 = SERIAL_NUMBER
///   field2+ = variant flags / options
/// </summary>
public sealed class MesMessageParser : IMesMessageParser
{
    public MesFrame Parse(string rawPayload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawPayload);

        var fields = rawPayload.Split(MesFrame.Separator, StringSplitOptions.None);
        if (fields.Length < 2)
        {
            throw new FormatException(
                $"MES frame must contain at least 2 fields (MODEL_CODE;SERIAL_NUMBER). Got '{rawPayload}'.");
        }

        return new MesFrame(
            RawPayload: rawPayload,
            Fields: fields,
            ModelCode: fields[0].Trim(),
            SerialNumber: fields[1].Trim(),
            ReceivedAt: DateTimeOffset.UtcNow);
    }
}
