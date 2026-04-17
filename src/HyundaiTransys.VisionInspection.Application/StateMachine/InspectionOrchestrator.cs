using HyundaiTransys.VisionInspection.Core.Abstractions;
using HyundaiTransys.VisionInspection.Core.Domain.Entities;
using HyundaiTransys.VisionInspection.Core.Domain.Enums;
using HyundaiTransys.VisionInspection.Core.Domain.ValueObjects;
using HyundaiTransys.VisionInspection.Core.Messaging;
using Microsoft.Extensions.Logging;
using Stateless;

namespace HyundaiTransys.VisionInspection.Application.StateMachine;

/// <summary>
/// Drives the production flow: MES frame → JOB change → Trigger → Evaluate → Persist.
/// Implements <see cref="IInspectionOrchestrator"/> using the Stateless library.
/// </summary>
public sealed class InspectionOrchestrator : IInspectionOrchestrator
{
    private readonly IMesClient _mes;
    private readonly IKeyenceClient _keyence;
    private readonly IJobResolver _jobResolver;
    private readonly IInspectionRepository _inspections;
    private readonly IImageStorage _images;
    private readonly ILogger<InspectionOrchestrator> _logger;

    private readonly StateMachine<SystemState, SystemTrigger> _fsm;
    private readonly StateMachine<SystemState, SystemTrigger>.TriggerWithParameters<MesMessage> _tMesReceived;
    private readonly StateMachine<SystemState, SystemTrigger>.TriggerWithParameters<InspectionResult> _tResult;

    private InspectionRecord? _current;
    private MesMessage? _currentMessage;

    public InspectionOrchestrator(
        IMesClient mes,
        IKeyenceClient keyence,
        IJobResolver jobResolver,
        IInspectionRepository inspections,
        IImageStorage images,
        ILogger<InspectionOrchestrator> logger)
    {
        _mes = mes;
        _keyence = keyence;
        _jobResolver = jobResolver;
        _inspections = inspections;
        _images = images;
        _logger = logger;

        _fsm = new StateMachine<SystemState, SystemTrigger>(SystemState.Idle);
        _tMesReceived = _fsm.SetTriggerParameters<MesMessage>(SystemTrigger.MesFrameReceived);
        _tResult = _fsm.SetTriggerParameters<InspectionResult>(SystemTrigger.ImageCaptured);

        ConfigureStateMachine();

        _fsm.OnTransitioned(t =>
        {
            _logger.LogInformation("FSM {From} → {To} ({Trigger})", t.Source, t.Destination, t.Trigger);
            StateChanged?.Invoke(this, t.Destination);
        });
    }

    public SystemState CurrentState => _fsm.State;
    public InspectionRecord? CurrentInspection => _current;

    public event EventHandler<SystemState>? StateChanged;
    public event EventHandler<InspectionRecord>? InspectionCompleted;

    public Task StartAsync(CancellationToken ct = default)
    {
        _mes.MessageReceived += OnMesMessageReceived;
        _keyence.ResultReceived += OnKeyenceResultReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _mes.MessageReceived -= OnMesMessageReceived;
        _keyence.ResultReceived -= OnKeyenceResultReceived;
        return Task.CompletedTask;
    }

    public async Task RetestAsync(CancellationToken ct = default)
    {
        if (_fsm.CanFire(SystemTrigger.Retest))
            await _fsm.FireAsync(SystemTrigger.Retest);
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        if (_fsm.CanFire(SystemTrigger.Reset))
            await _fsm.FireAsync(SystemTrigger.Reset);
    }

    public ValueTask DisposeAsync()
    {
        _mes.MessageReceived -= OnMesMessageReceived;
        _keyence.ResultReceived -= OnKeyenceResultReceived;
        return ValueTask.CompletedTask;
    }

    // ---------- state machine wiring ----------

    private void ConfigureStateMachine()
    {
        _fsm.Configure(SystemState.Idle)
            .Permit(SystemTrigger.MesFrameReceived, SystemState.Parsing);

        _fsm.Configure(SystemState.Parsing)
            .OnEntryFromAsync(_tMesReceived, HandleParsingAsync)
            .Permit(SystemTrigger.FrameParsed, SystemState.JobSwitching)
            .Permit(SystemTrigger.Error, SystemState.Faulted);

        _fsm.Configure(SystemState.JobSwitching)
            .OnEntryAsync(HandleJobSwitchAsync)
            .Permit(SystemTrigger.JobReady, SystemState.Triggering)
            .Permit(SystemTrigger.Error, SystemState.Faulted);

        _fsm.Configure(SystemState.Triggering)
            .OnEntryAsync(HandleTriggerAsync)
            .Permit(SystemTrigger.ImageCaptured, SystemState.Evaluating)
            .Permit(SystemTrigger.Error, SystemState.Faulted);

        _fsm.Configure(SystemState.Evaluating)
            .OnEntryFromAsync(_tResult, HandleEvaluateAsync)
            .Permit(SystemTrigger.ResultOk, SystemState.Idle)
            .Permit(SystemTrigger.ResultNg, SystemState.NgAlert);

        _fsm.Configure(SystemState.NgAlert)
            .Permit(SystemTrigger.Retest, SystemState.Triggering)
            .Permit(SystemTrigger.Reset, SystemState.Idle);

        _fsm.Configure(SystemState.Faulted)
            .Permit(SystemTrigger.Reset, SystemState.Idle);
    }

    // ---------- handlers ----------

    private async Task HandleParsingAsync(MesMessage msg)
    {
        _currentMessage = msg;
        _current = new InspectionRecord
        {
            ModelCode = msg.Frame.ModelCode,
            SerialNumber = msg.Frame.SerialNumber,
            JobId = 0,
            Result = InspectionResult.Unknown,
            RawMesFrame = msg.Frame.RawPayload
        };
        await _inspections.AddAsync(_current);
        await _fsm.FireAsync(SystemTrigger.FrameParsed);
    }

    private async Task HandleJobSwitchAsync()
    {
        try
        {
            var job = await _jobResolver.ResolveAsync(_currentMessage!.Frame);
            _current!.JobId = job.Value;
            await _inspections.UpdateAsync(_current);

            var ack = await _keyence.ChangeJobAsync(job);
            if (!ack.Success) throw new InvalidOperationException($"Keyence JOB change rejected: {ack.ErrorMessage}");

            await _fsm.FireAsync(SystemTrigger.JobReady);
        }
        catch (Exception ex)
        {
            await FaultAsync(ex, "JOB switch failure");
        }
    }

    private async Task HandleTriggerAsync()
    {
        try
        {
            var ack = await _keyence.TriggerAsync();
            if (!ack.Success) throw new InvalidOperationException($"Keyence trigger rejected: {ack.ErrorMessage}");
            // Wait for async ResultReceived → OnKeyenceResultReceived fires ImageCaptured.
        }
        catch (Exception ex)
        {
            await FaultAsync(ex, "Trigger failure");
        }
    }

    private async Task HandleEvaluateAsync(InspectionResult result)
    {
        if (_current is null) return;

        _current.Result = result;
        _current.CompletedAt = DateTimeOffset.UtcNow;
        await _inspections.UpdateAsync(_current);

        InspectionCompleted?.Invoke(this, _current);
        if (_mes.IsConnected && _currentMessage is not null)
        {
            try { await _mes.SendAsync(new MesAck(_currentMessage.CorrelationId, true)); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to ACK MES."); }
        }

        await _fsm.FireAsync(result == InspectionResult.Ok ? SystemTrigger.ResultOk : SystemTrigger.ResultNg);
    }

    // ---------- event bridges ----------

    private async void OnMesMessageReceived(object? sender, MesMessage msg)
    {
        try
        {
            if (_fsm.CanFire(SystemTrigger.MesFrameReceived))
                await _fsm.FireAsync(_tMesReceived, msg);
            else
                _logger.LogWarning("MES frame dropped in state {State}.", _fsm.State);
        }
        catch (Exception ex)
        {
            await FaultAsync(ex, "MES dispatch failure");
        }
    }

    private async void OnKeyenceResultReceived(object? sender, KeyenceResponse response)
    {
        try
        {
            if (response.ImageBytes is not null && _current is not null)
            {
                _current.ImagePath = await _images.SaveAsync(
                    response.ImageBytes, response.Result, _current.SerialNumber, DateTimeOffset.UtcNow);
            }
            if (_fsm.CanFire(SystemTrigger.ImageCaptured))
                await _fsm.FireAsync(_tResult, response.Result);
        }
        catch (Exception ex)
        {
            await FaultAsync(ex, "Keyence result handling failure");
        }
    }

    private async Task FaultAsync(Exception ex, string context)
    {
        _logger.LogError(ex, "{Context}: {Message}", context, ex.Message);
        if (_current is not null)
        {
            _current.ErrorMessage = $"{context}: {ex.Message}";
            try { await _inspections.UpdateAsync(_current); } catch { /* best-effort */ }
        }
        if (_fsm.CanFire(SystemTrigger.Error))
            await _fsm.FireAsync(SystemTrigger.Error);
    }
}
