using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Player intents, independent of device. "Pressed/Released" members are edge-triggered and only
    /// valid when read from Update (the controller latches them into input buffers); "Held"/Move may be polled anywhere.
    /// </summary>
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool JumpHeld { get; }
        bool JumpPressed { get; }
        bool JumpReleased { get; }
        bool DashPressed { get; }
        bool AttackPressed { get; }
        bool SkillPressed { get; }
        bool SlideRequested { get; }
        bool AuraWindPressed { get; }
        bool AuraFirePressed { get; }
        bool AuraWaterPressed { get; }
        bool AuraNextPressed { get; }
        bool AuraPrevPressed { get; }
        bool PausePressed { get; }
        bool MapPressed { get; }
    }
}
