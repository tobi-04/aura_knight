using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>Colours an Image from a theme token (panels, cards, backgrounds).</summary>
    [RequireComponent(typeof(Image))]
    public sealed class ThemedImage : MonoBehaviour
    {
        [SerializeField] UIColorToken token = UIColorToken.Panel;
        [Range(0f, 1f)] [SerializeField] float alpha = 1f;

        public UIColorToken Token => token;

        public void Configure(UIColorToken colorToken, float alphaValue = 1f)
        {
            token = colorToken;
            alpha = alphaValue;
            Apply();
        }

        void OnEnable() => Apply();

        public void Apply()
        {
            var c = UITheme.Active.GetColor(token);
            c.a *= alpha;
            GetComponent<Image>().color = c;
        }
    }
}
