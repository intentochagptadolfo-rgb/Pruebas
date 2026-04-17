namespace HyundaiTransys.VisionInspection.Core.Domain.Enums;

public enum SystemState
{
    Idle,
    Parsing,
    JobSwitching,
    Triggering,
    Evaluating,
    NgAlert,
    Faulted
}

public enum SystemTrigger
{
    MesFrameReceived,
    FrameParsed,
    JobReady,
    ImageCaptured,
    ResultOk,
    ResultNg,
    Retest,
    Reset,
    Error
}
