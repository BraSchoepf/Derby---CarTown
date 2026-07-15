namespace CarSystem
{
    /// <summary>
    /// Abstraccion de input. El CarController no sabe de donde viene el input:
    /// puede ser teclado local, un mando, IA, o valores sincronizados por red
    /// (Photon Fusion). Esto es lo que hace el sistema escalable a multiplayer
    /// sin reescribir la fisica.
    /// </summary>
    public interface IVehicleInput
    {
        float Throttle { get; }   // -1 (retro/freno) a 1 (acelerar)
        float Steer { get; }      // -1 (izquierda) a 1 (derecha)
        bool Handbrake { get; }
    }
}