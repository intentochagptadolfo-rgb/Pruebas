using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using HyundaiTransys.VisionInspection.Core.Messaging;

namespace HyundaiTransys.VisionInspection.Infrastructure.Keyence;

/// <summary>
/// Encodes commands and decodes responses for the Keyence IV4 TCP no-protocol mode.
/// The exact ASCII syntax should be validated against the camera's Communication Spec.
/// Sample (placeholder) wire format used here:
///   Change JOB : "PW,<nnn>\r\n"     response: "PW,OK\r\n" / "PW,NG\r\n"
///   Trigger    : "T1\r\n"           response: "T1,OK\r\n" followed asynchronously by
///                                    "RS,OK\r\n" or "RS,NG\r\n"
///   Status     : "S0\r\n"           response: "S0,<state>\r\n"
/// </summary>
public static class KeyenceProtocol
{
    public const string Terminator = "\r\n";

    public static string Encode(KeyenceCommand command) => command switch
    {
        ChangeJobCommand c => $"PW,{c.JobId}{Terminator}",
        TriggerCommand    => $"T1{Terminator}",
        GetStatusCommand  => $"S0{Terminator}",
        _ => throw new NotSupportedException($"Unsupported Keyence command: {command.GetType().Name}")
    };

    public static KeyenceResponse DecodeAck(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return new KeyenceResponse(false, InspectionResult.Unknown, line, null, "Empty response.");
        }
        var ok = line.Contains(",OK", StringComparison.OrdinalIgnoreCase);
        return new KeyenceResponse(ok, InspectionResult.Unknown, line, null, ok ? null : line);
    }

    public static KeyenceResponse DecodeResult(string line)
    {
        if (line.Contains(",OK", StringComparison.OrdinalIgnoreCase))
            return new KeyenceResponse(true, InspectionResult.Ok, line, null, null);
        if (line.Contains(",NG", StringComparison.OrdinalIgnoreCase))
            return new KeyenceResponse(true, InspectionResult.Ng, line, null, null);
        return new KeyenceResponse(false, InspectionResult.Unknown, line, null, $"Unparseable result: {line}");
    }
}
