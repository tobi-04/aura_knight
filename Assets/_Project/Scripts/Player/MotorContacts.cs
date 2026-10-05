namespace AuraKnight.Player
{
    /// <summary>Surfaces that stopped a <see cref="KinematicMotor2D.Move"/> call.</summary>
    public struct MotorContacts
    {
        public bool Below;
        public bool Above;
        public bool Left;
        public bool Right;

        public bool Horizontal => Left || Right;
    }
}
