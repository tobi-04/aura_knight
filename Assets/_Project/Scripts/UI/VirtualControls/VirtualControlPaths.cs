namespace AuraKnight.UI
{
    /// <summary>
    /// Control paths the on-screen widgets write to. They live on a virtual Gamepad device, so the same
    /// Gameplay action bindings serve touch and physical gamepads.
    /// </summary>
    public static class VirtualControlPaths
    {
        public const string LeftStick = "<Gamepad>/leftStick";
        public const string Jump = "<Gamepad>/buttonSouth";
        public const string Attack = "<Gamepad>/buttonWest";
        public const string Dash = "<Gamepad>/rightShoulder";
        public const string Skill = "<Gamepad>/buttonNorth";
        public const string Slide = "<Gamepad>/buttonEast";
        public const string AuraWind = "<Gamepad>/dpad/left";
        public const string AuraFire = "<Gamepad>/dpad/up";
        public const string AuraWater = "<Gamepad>/dpad/right";
        public const string Pause = "<Gamepad>/start";
        public const string Map = "<Gamepad>/select";

        public static readonly string[] All =
        {
            LeftStick, Jump, Attack, Dash, Skill, Slide, AuraWind, AuraFire, AuraWater, Pause, Map
        };
    }
}
