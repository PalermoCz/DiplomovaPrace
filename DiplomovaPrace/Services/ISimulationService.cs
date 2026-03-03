namespace DiplomovaPrace.Services;

/// <summary>
/// Rozhraní pro ovládání simulace z UI komponent.
/// Implementace je současně registrována jako IHostedService pro automatický lifecycle.
/// </summary>
public interface ISimulationService
{
    bool IsRunning { get; }
    long CurrentTick { get; }
    void Start();
    void Stop();
}
