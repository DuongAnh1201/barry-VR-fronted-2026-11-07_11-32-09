using Barry.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Barry.UI
{
    public sealed class StatusPanelView : MonoBehaviour
    {
        [SerializeField] TMP_Text robot, control, cameraFps, backendRtt, battery;
        [SerializeField] RectTransform batteryFill;      // anchored 0..1 in its parent
        [SerializeField] BarRow forward, strafe, turn;
        [SerializeField] TMP_Text footer;

        [System.Serializable]
        public sealed class BarRow
        {
            public TMP_Text value;
            public RectTransform fill;   // child of a bar track; anchors set from model
        }

        StatusPanelModel last;

        void OnEnable() { ThemeApplier.Changed += OnTheme; }
        void OnDisable() { ThemeApplier.Changed -= OnTheme; }
        void OnTheme(ThemeTokens _) => Render();

        public void Bind(StatusPanelModel m) { last = m; Render(); }

        void Render()
        {
            if (last == null) return;
            footer.text = StatusPanelModel.Footer;
            Set(robot, last.Robot); Set(control, last.Control); Set(cameraFps, last.CameraFps);
            Set(backendRtt, last.BackendRtt); Set(battery, last.Battery);
            batteryFill.anchorMin = new Vector2(0, 0);
            batteryFill.anchorMax = new Vector2(Mathf.Clamp01(last.BatteryFill), 1);
            Bar(forward, last.Forward); Bar(strafe, last.Strafe); Bar(turn, last.Turn);
        }

        static void Set(TMP_Text t, StatusValueModel v)
        {
            t.text = v.Text;
            var k = ThemeApplier.Current;
            if (k == null) return;
            if (v.Tone == StatusTone.Neutral) t.color = k.textPrimary;
            else { ToneColors.Resolve(v.Tone, k, out var fg, out _); t.color = fg; }
        }

        static void Bar(BarRow r, CommandBarModel m)
        {
            r.value.text = m.ValueText;
            r.fill.anchorMin = new Vector2(m.FillStart, 0);
            r.fill.anchorMax = new Vector2(m.FillEnd, 1);
            r.fill.offsetMin = r.fill.offsetMax = Vector2.zero;
        }
    }
}
