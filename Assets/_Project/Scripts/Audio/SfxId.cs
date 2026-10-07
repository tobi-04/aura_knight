namespace AuraKnight.Audio
{
    /// <summary>
    /// Every sound effect the game can play. Values are serialized in <see cref="SfxLibrary"/>, so never renumber:
    /// append new ids at the end. <see cref="None"/> means "no sound" in mappings.
    /// </summary>
    public enum SfxId
    {
        None = 0,
        Footstep = 1,
        Jump = 2,
        Land = 3,
        Dash = 4,
        Slide = 5,
        WallSlide = 6,
        SwordSwing = 7,
        SwordHit = 8,
        PlayerHurt = 9,
        PlayerDie = 10,
        AuraWind = 11,
        AuraFire = 12,
        AuraWater = 13,
        AuraUnlock = 14,
        SkillWind = 15,
        SkillFire = 16,
        SkillWater = 17,
        Coin = 18,
        Chest = 19,
        Altar = 20,
        UiTap = 21,
        UiBack = 22,
        BossRoar = 23,
        EnemyHit = 24,
        EnemyDie = 25,
        Respawn = 26
    }
}
