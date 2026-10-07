#if UNITY_EDITOR
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>Editor-only test aid for the Test_Boss_* scenes: on-screen buttons to jump to the next phase, kill the boss or reset it.</summary>
    public sealed class BossDebugControls : MonoBehaviour
    {
        [SerializeField] BossBase boss;

        void OnGUI()
        {
            if (boss == null) return;
            GUILayout.BeginArea(new Rect(10f, 10f, 190f, 140f));
            GUILayout.Label($"{boss.name}  HP {boss.Health.Current}/{boss.Health.Max}  phase {boss.PhaseIndex + 1}  {boss.State}");
            if (GUILayout.Button("Skip to next phase")) boss.SkipToPhase(boss.PhaseIndex + 1);
            if (GUILayout.Button("Reset boss")) if (boss.Arena != null) boss.Arena.ResetEncounter();
            GUILayout.EndArea();
        }
    }
}
#endif
