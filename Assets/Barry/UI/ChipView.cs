using Barry.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Barry.UI
{
    /// <summary>A pill: background Image + TMP label, coloured from a StatusTone.</summary>
    public sealed class ChipView : MonoBehaviour
    {
        [SerializeField] Image background;
        [SerializeField] TMP_Text label;
        HeaderChipModel last;
        bool has;

        public void Set(HeaderChipModel model) { last = model; has = true; Render(); }
        public void Set(string text, StatusTone tone) => Set(new HeaderChipModel(text, tone));
        void OnEnable() { ThemeApplier.Changed += OnTheme; Render(); }
        void OnDisable() { ThemeApplier.Changed -= OnTheme; }
        void OnTheme(ThemeTokens _) => Render();

        void Render()
        {
            if (!has || ThemeApplier.Current == null) return;
            ToneColors.Resolve(last.Tone, ThemeApplier.Current, out var fg, out var bg);
            label.text = last.Text;
            label.color = fg;
            if (background != null) background.color = bg;
        }
    }
}
