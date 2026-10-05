namespace AuraKnight.Player
{
    /// <summary>All Leo states. Attack/AirAttack/Hurt/Dead/Swim are registered by later phases.</summary>
    public enum PlayerStateId
    {
        Idle, Run, Jump, Fall, WallSlide, WallJump, Dash, Slide,
        Swim, Attack, AirAttack, Hurt, Dead
    }
}
