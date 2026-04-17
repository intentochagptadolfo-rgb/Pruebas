namespace HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;

/// <summary>
/// Identifier of a JOB stored in the Keyence controller (0..999).
/// </summary>
public readonly record struct JobId(int Value)
{
    public static JobId Parse(string value) =>
        int.TryParse(value, out var n) && n is >= 0 and <= 999
            ? new JobId(n)
            : throw new FormatException($"Invalid Keyence JobId: '{value}'.");

    public override string ToString() => Value.ToString("D3");
}
