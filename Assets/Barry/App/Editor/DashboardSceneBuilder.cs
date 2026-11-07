using Barry.Config;
using Barry.Theme;
using Barry.UI;
using Barry.XR;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Barry.App.Editor
{
    /// <summary>Menu: Barry/Build Dashboard Scene. Regenerates Scenes/Dashboard.unity and UI/Prefabs/*.prefab (HANDOFF §4/§9.3).</summary>
    public static class DashboardSceneBuilder
    {
        const string Root = "Assets/Barry";
        const float Eye = 1.6f;
        // HANDOFF §4 says 30°, but flat panels 0.45 m wide at 1.3 m span ±9.8° while the 1.8 m camera
        // screen/header at 2.0 m span ±24.2°. Clearing them from the head position needs >= 34°, so use 36°
        // (2° gap). Vertically the side panels (±16.7°) are already inside the control strip (-21°) bound.
        const float SidePanelYaw = 36f;
        static TMP_FontAsset body, mono, brand;
        static ThemeTokens look;   // Galactic tokens: build-time colours so the scene reads correctly in Edit mode too

        [MenuItem("Barry/Build Dashboard Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Barry: exit Play mode before running 'Build Dashboard Scene' (scene creation is not allowed in Play mode).");
                return;
            }
            // NewScene(Single) unloads unused assets, so load assets only after it;
            // references loaded earlier serialize as null.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            body = Load<TMP_FontAsset>("Theme/Fonts/Inter SDF.asset");
            mono = Load<TMP_FontAsset>("Theme/Fonts/JetBrainsMono SDF.asset");
            brand = Load<TMP_FontAsset>("Theme/Fonts/Orbitron SDF.asset");
            var config = Load<BarryConfig>("Config/BarryConfig.asset");
            var galactic = Load<ThemeTokens>("Theme/Tokens/Galactic.asset");
            var light = Load<ThemeTokens>("Theme/Tokens/Light.asset");
            look = galactic;

            // Rig: camera at eye height, headset-tracked.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, Eye, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = galactic.envSky;
            cam.nearClipPlane = 0.1f;
            camGo.AddComponent<AudioListener>();
            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.positionInput = new InputActionProperty(new InputAction("Position", binding: "<XRHMD>/centerEyePosition"));
            tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation"));

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<XRUIInputModule>();

            // Panels (sizes in mm per §4; canvas scale 0.001).
            var header = BuildHeader();
            Place(header.gameObject, new Vector3(0, Eye + 0.506f + 0.055f + 0.02f, 2.0f), Quaternion.identity);
            var camera = BuildCameraScreen();
            Place(camera.gameObject, new Vector3(0, Eye, 2.0f), Quaternion.identity);
            var status = BuildStatus();
            PlaceOnArc(status.gameObject, -SidePanelYaw, 1.3f, Eye);
            var actions = BuildActions();
            PlaceOnArc(actions.gameObject, SidePanelYaw, 1.3f, Eye);
            var strip = BuildControlStrip();
            float below = 25f * Mathf.Deg2Rad;
            Place(strip.gameObject, new Vector3(0, Eye - 0.9f * Mathf.Sin(below), 0.9f * Mathf.Cos(below)), Quaternion.Euler(15f, 0, 0));

            // TODO(#39/#40): once EnvironmentView and RoundedPanel land, enable these (signatures unconfirmed):
            //   EnvironmentView.Create(galactic);                              // sky, stars, nebula, planet, floor grid, platform
            //   foreach (var c in new[] { header, status, actions, strip }) RoundedPanel.Apply(c, galactic, glow: 0.35f);
            //   RoundedPanel.Apply(camera, galactic, glow: galactic.cameraGlowAlpha);   // camera screen: stronger glow (0.55)

            // App + controller.
            var appGo = new GameObject("BarryApp");
            appGo.SetActive(false);   // so serialized refs are set before Awake
            var app = appGo.AddComponent<BarryApp>();
            appGo.AddComponent<XRSessionWatcher>();
            SetRef(app, "config", config);
            var ctrl = appGo.AddComponent<DashboardController>();
            SetRef(ctrl, "app", app);
            SetRef(ctrl, "header", header.GetComponentInChildren<HeaderStripView>(true));
            SetRef(ctrl, "cameraScreen", camera.GetComponentInChildren<CameraScreenView>(true));
            SetRef(ctrl, "statusPanel", status.GetComponentInChildren<StatusPanelView>(true));
            SetRef(ctrl, "actionsPanel", actions.GetComponentInChildren<ActionsPanelView>(true));
            SetRef(ctrl, "controlStrip", strip.GetComponentInChildren<ControlStripView>(true));

            var themeGo = new GameObject("ThemeApplier");
            var ta = themeGo.AddComponent<ThemeApplier>();
            SetRef(ta, "galactic", galactic);
            SetRef(ta, "light", light);
            SetRef(ta, "config", config);
            themeGo.transform.SetSiblingIndex(0);
            appGo.SetActive(true);

            // Save prefabs (panels), then the scene.
            string pdir = Root + "/UI/Prefabs";
            foreach (var g in new[] { header, camera, status, actions, strip })
                PrefabUtility.SaveAsPrefabAssetAndConnect(g.gameObject, $"{pdir}/{g.name}.prefab", InteractionMode.AutomatedAction);

            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;   // the only Player setting changed
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Dashboard.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Barry: Dashboard scene built.");
        }

        // ---------------------------------------------------------------- panels

        static Canvas BuildHeader()
        {
            var c = NewCanvas("HeaderStrip", 1800, 110);
            Frame(c.transform, 1800, 110, "Frame");
            var view = c.gameObject.AddComponent<HeaderStripView>();
            Label(c.transform, "Wordmark", "BARRY", 48, 30, 25, 220, 60, ThemeRole.Brand, FontRole.Brand);
            Label(c.transform, "Mode", "MODE · MANUAL", 30, 260, 30, 300, 50, ThemeRole.TextPrimary, FontRole.Body, bold: true);
            Label(c.transform, "ModeMore", "more modes later", 26, 570, 32, 250, 46, ThemeRole.TextSecondary, FontRole.Body);
            var robot = Chip(c.transform, "RobotChip", 840, 25, 400, 60);
            var control = Chip(c.transform, "ControlChip", 1260, 25, 500, 60);
            SetRef(view, "modeLabel", c.transform.Find("Mode").GetComponent<TMP_Text>());
            SetRef(view, "robotChip", robot);
            SetRef(view, "controlChip", control);
            return c;
        }

        static Canvas BuildCameraScreen()
        {
            var c = NewCanvas("CameraScreen", 1800, 1012);
            var view = c.gameObject.AddComponent<CameraScreenView>();
            var bezel = Img(c.transform, "Bezel", ThemeRole.PanelBg);
            Stretch(bezel.rectTransform, 0, 0, 0, 0);
            var glow = Img(c.transform, "Border", ThemeRole.PanelBorder);
            glow.raycastTarget = false;
            Stretch(glow.rectTransform, 0, 0, 0, 0);
            glow.transform.SetAsFirstSibling();
            var vid = new GameObject("Video", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            vid.transform.SetParent(c.transform, false);
            Stretch(vid.rectTransform, 20, 20, 20, 20);
            vid.color = galacticVideoBg();
            vid.gameObject.AddComponent<ThemeTarget>().colorRole = ThemeRole.VideoBg;
            var ph = Label(c.transform, "Placeholder", "Waiting for camera…", 40, 0, 0, 0, 0, ThemeRole.StopText, FontRole.Body);
            Stretch(ph.rectTransform, 20, 20, 20, 20); ph.alignment = TextAlignmentOptions.Center;
            // overlays
            var live = Chip(c.transform, "LiveChip", 50, 50, 200, 56);
            var fps = DimLabel(c.transform, "Fps", "-- fps", 50 + 215, 50, 170);
            var age = DimLabel(c.transform, "Age", "frame age -- ms", 50 + 400, 50, 330);
            var rp = DimLabel(c.transform, "RollPitch", "ROLL --° · PITCH --°", 1800 - 50 - 520, 50, 520);
            var ret = Label(c.transform, "Reticle", "+", 90, 0, 0, 100, 100, ThemeRole.StopText, FontRole.Mono);
            ret.rectTransform.anchorMin = ret.rectTransform.anchorMax = new Vector2(.5f, .5f);
            ret.rectTransform.anchoredPosition = Vector2.zero; ret.alignment = TextAlignmentOptions.Center;
            ret.raycastTarget = false;
            SetRef(view, "video", vid);
            SetRef(view, "screenRect", c.GetComponent<RectTransform>());
            SetRef(view, "fpsLabel", fps); SetRef(view, "ageLabel", age); SetRef(view, "rollPitchLabel", rp);
            SetRef(view, "placeholder", ph.gameObject); SetRef(view, "liveChip", live);
            return c;
        }

        static Color galacticVideoBg() => new Color(0.106f, 0.122f, 0.18f);

        static Canvas BuildStatus()
        {
            var c = NewCanvas("StatusPanel", 450, 780);
            Frame(c.transform, 450, 780, "Frame");
            var view = c.gameObject.AddComponent<StatusPanelView>();
            Label(c.transform, "Title", "STATUS", 24, 24, 20, 300, 32, ThemeRole.TextSecondary, FontRole.Body, bold: true);
            var cells = new[] { ("Robot", 24f, 70f), ("Control", 235f, 70f), ("Camera", 24f, 175f), ("Backend RTT", 235f, 175f) };
            var vals = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                Label(c.transform, cells[i].Item1 + "L", cells[i].Item1.ToUpperInvariant(), 22, cells[i].Item2, cells[i].Item3, 200, 30, ThemeRole.TextSecondary, FontRole.Body, bold: true);
                vals[i] = Value(c.transform, cells[i].Item1 + "V", "--", 28, cells[i].Item2, cells[i].Item3 + 34, 200, 44);
            }
            Label(c.transform, "BatteryL", "BATTERY", 22, 24, 285, 200, 30, ThemeRole.TextSecondary, FontRole.Body, bold: true);
            var bat = Value(c.transform, "BatteryV", "-- %", 28, 260, 280, 166, 44);
            bat.alignment = TextAlignmentOptions.Right;
            var track = Img(c.transform, "BatteryTrack", ThemeRole.NeutralFill);
            At(track.rectTransform, 24, 335, 402, 22);
            var bfill = Img(track.transform, "BatteryFill", ThemeRole.Accent);
            Stretch(bfill.rectTransform, 0, 0, 0, 0);
            Label(c.transform, "CmdTitle", "COMMAND SENT", 22, 24, 385, 300, 30, ThemeRole.TextSecondary, FontRole.Body, bold: true);
            var rows = new StatusPanelView.BarRow[3];
            string[] names = { "Forward", "Strafe", "Turn" };
            for (int i = 0; i < 3; i++)
            {
                float y = 430 + i * 80;
                Label(c.transform, names[i] + "L", names[i], 24, 24, y, 120, 40, ThemeRole.TextPrimary, FontRole.Body);
                var tr = Img(c.transform, names[i] + "Track", ThemeRole.NeutralFill);
                At(tr.rectTransform, 145, y + 8, 175, 26);
                var mid = Img(tr.transform, "Center", ThemeRole.NeutralBorder);
                mid.rectTransform.anchorMin = new Vector2(.5f, 0); mid.rectTransform.anchorMax = new Vector2(.5f, 1);
                mid.rectTransform.sizeDelta = new Vector2(2, 0);
                var f = Img(tr.transform, names[i] + "Fill", ThemeRole.Accent);
                f.rectTransform.anchorMin = new Vector2(.5f, 0); f.rectTransform.anchorMax = new Vector2(.5f, 1);
                f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
                var v = Value(c.transform, names[i] + "V", "0.00", 28, 330, y, 100, 40);
                v.alignment = TextAlignmentOptions.Right;
                rows[i] = new StatusPanelView.BarRow { value = v, fill = f.rectTransform };
            }
            var foot = Label(c.transform, "Footer", "", 22, 24, 690, 402, 70, ThemeRole.TextSecondary, FontRole.Body);
            foot.textWrappingMode = TextWrappingModes.Normal;
            SetRef(view, "robot", vals[0]); SetRef(view, "control", vals[1]); SetRef(view, "cameraFps", vals[2]); SetRef(view, "backendRtt", vals[3]);
            SetRef(view, "battery", bat); SetRef(view, "batteryFill", bfill.rectTransform); SetRef(view, "footer", foot);
            var so = new SerializedObject(view);
            string[] f2 = { "forward", "strafe", "turn" };
            for (int i = 0; i < 3; i++)
            {
                var p = so.FindProperty(f2[i]);
                p.FindPropertyRelative("value").objectReferenceValue = rows[i].value;
                p.FindPropertyRelative("fill").objectReferenceValue = rows[i].fill;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return c;
        }

        static Canvas BuildActions()
        {
            var c = NewCanvas("ActionsPanel", 450, 780);
            var view = c.gameObject.AddComponent<ActionsPanelView>();
            var expanded = new GameObject("Expanded", typeof(RectTransform));
            expanded.transform.SetParent(c.transform, false);
            Stretch((RectTransform)expanded.transform, 0, 0, 0, 0);
            Frame(expanded.transform, 450, 780, "Frame");
            Label(expanded.transform, "Title", "ACTIONS", 24, 24, 20, 200, 32, ThemeRole.TextSecondary, FontRole.Body, bold: true);
            Label(expanded.transform, "Close", "B · close", 22, 250, 20, 176, 32, ThemeRole.TextSecondary, FontRole.Body).alignment = TextAlignmentOptions.Right;
            var list = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            list.transform.SetParent(expanded.transform, false);
            At((RectTransform)list.transform, 24, 70, 402, 610);
            var vlg = list.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 12; vlg.childControlWidth = true; vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
            var tmpl = Button(list.transform, "ItemTemplate", "[Action]", 26, 402, 80, ThemeRole.NeutralFill, ThemeRole.TextPrimary);
            tmpl.gameObject.SetActive(false);
            var foot = Label(expanded.transform, "Footer", "", 22, 24, 695, 402, 70, ThemeRole.TextSecondary, FontRole.Body);
            foot.textWrappingMode = TextWrappingModes.Normal;
            var tab = new GameObject("CollapsedTab", typeof(RectTransform));
            tab.transform.SetParent(c.transform, false);
            At((RectTransform)tab.transform, 0, 0, 280, 70);
            Frame(tab.transform, 280, 70, "Frame");
            var tl = Label(tab.transform, "Label", "ACTIONS · B", 26, 0, 0, 280, 70, ThemeRole.Accent, FontRole.Body, bold: true);
            tl.alignment = TextAlignmentOptions.Center;
            tab.SetActive(false);
            SetRef(view, "expanded", expanded); SetRef(view, "collapsedTab", tab); SetRef(view, "collapsedLabel", tl);
            SetRef(view, "listRoot", (RectTransform)list.transform); SetRef(view, "itemTemplate", tmpl); SetRef(view, "footer", foot);
            return c;
        }

        static Canvas BuildControlStrip()
        {
            var c = NewCanvas("ControlStrip", 900, 120);
            Frame(c.transform, 900, 120, "Frame");
            var view = c.gameObject.AddComponent<ControlStripView>();
            Label(c.transform, "IntensityL", "INTENSITY", 20, 20, 40, 170, 40, ThemeRole.TextSecondary, FontRole.Body, bold: true);
            var lv = new UnityEngine.UI.Button[3];
            for (int i = 0; i < 3; i++)
                lv[i] = Button(c.transform, "Level" + ControlStripView.Levels[i], ControlStripView.Levels[i], 22, 105, 70, ThemeRole.NeutralFill, ThemeRole.TextPrimary, 195 + i * 115, 25);
            var stop = Button(c.transform, "Stop", "STOP", 40, 240, 100, ThemeRole.Danger, ThemeRole.StopText, 545, 10, bold: true);
            var hint = Label(stop.transform, "Hint", "A button · or point + trigger", 16, 0, 0, 240, 24, ThemeRole.StopText, FontRole.Body);
            hint.alignment = TextAlignmentOptions.Center;
            hint.rectTransform.anchorMin = new Vector2(0, 0); hint.rectTransform.anchorMax = new Vector2(1, 0);
            hint.rectTransform.pivot = new Vector2(.5f, 0); hint.rectTransform.anchoredPosition = new Vector2(0, 6);
            hint.raycastTarget = false;
            stop.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.Top;
            var exit = Button(c.transform, "Exit", "Exit VR", 22, 100, 70, ThemeRole.PanelBg, ThemeRole.TextPrimary, 790, 25, themed: true);
            var so = new SerializedObject(view);
            var arr = so.FindProperty("levelButtons");
            arr.arraySize = 3;
            for (int i = 0; i < 3; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = lv[i];
            so.FindProperty("stopButton").objectReferenceValue = stop;
            so.FindProperty("exitButton").objectReferenceValue = exit;
            so.FindProperty("stopHint").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();
            return c;
        }

        // ---------------------------------------------------------------- helpers

        static Canvas NewCanvas(string name, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = Vector3.one * 0.001f;
            return canvas;
        }

        static void Place(GameObject go, Vector3 pos, Quaternion rot) { go.transform.SetPositionAndRotation(pos, rot); }

        static void PlaceOnArc(GameObject go, float yawDeg, float dist, float y)
        {
            float a = yawDeg * Mathf.Deg2Rad;
            go.transform.SetPositionAndRotation(new Vector3(Mathf.Sin(a) * dist, y, Mathf.Cos(a) * dist), Quaternion.Euler(0, yawDeg, 0));
        }

        static void Stretch(RectTransform rt, float l, float t, float r, float b)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        /// <summary>Top-left anchored placement, y down, sizes in mm.</summary>
        static void At(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static Image Img(Transform parent, string name, ThemeRole role)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.AddComponent<ThemeTarget>().colorRole = role;
            var image = go.GetComponent<Image>();
            if (look != null) image.color = ThemeApplier.Resolve(look, role);
            return image;
        }

        static void Frame(Transform parent, float w, float h, string name)
        {
            var border = Img(parent, name + "Border", ThemeRole.PanelBorder);
            Stretch(border.rectTransform, 0, 0, 0, 0); border.raycastTarget = false;
            var fill = Img(border.transform, name + "Fill", ThemeRole.PanelBg);
            Stretch(fill.rectTransform, 2, 2, 2, 2);
        }

        static TMP_Text Label(Transform parent, string name, string text, float size, float x, float y, float w, float h,
            ThemeRole color, FontRole font, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.textWrappingMode = TextWrappingModes.NoWrap;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            At(t.rectTransform, x, y, w, h);
            var tt = go.AddComponent<ThemeTarget>();
            tt.colorRole = color; tt.applyFont = true; tt.fontRole = font;
            t.font = font == FontRole.Brand ? brand : font == FontRole.Mono ? mono : body;
            if (look != null) t.color = ThemeApplier.Resolve(look, color);   // opaque; never default white
            return t;
        }

        static TMP_Text Value(Transform parent, string name, string text, float size, float x, float y, float w, float h)
        {
            // Cap height >= 1.2 cm per metre: at 1.3 m that is 15.6 mm; JetBrains Mono caps ~0.73 em => size >= 22.
            var t = Label(parent, name, text, Mathf.Max(size, 28f), x, y, w, h, ThemeRole.TextPrimary, FontRole.Mono, bold: true);
            if (look != null) { var c = look.textPrimary; c.a = 1f; t.color = c; }
            t.GetComponent<ThemeTarget>().applyColor = false;   // colour driven by StatusTone at runtime
            return t;
        }

        static TMP_Text DimLabel(Transform parent, string name, string text, float x, float y, float w)
        {
            var bg = Img(parent, name + "Bg", ThemeRole.OverlayDim);
            At(bg.rectTransform, x, y, w, 56); bg.raycastTarget = false;
            var t = Label(bg.transform, name, text, 30, 14, 0, w - 28, 56, ThemeRole.StopText, FontRole.Mono, bold: true);
            return t;
        }

        static ChipView Chip(Transform parent, string name, float x, float y, float w, float h)
        {
            var bg = Img(parent, name, ThemeRole.NeutralFill);
            bg.GetComponent<ThemeTarget>().applyColor = false;
            At(bg.rectTransform, x, y, w, h); bg.raycastTarget = false;
            var t = Label(bg.transform, "Text", "--", 30, 0, 0, w, h, ThemeRole.TextPrimary, FontRole.Body, bold: true);
            t.GetComponent<ThemeTarget>().applyColor = false;
            t.alignment = TextAlignmentOptions.Center;
            Stretch(t.rectTransform, 0, 0, 0, 0);
            var chip = bg.gameObject.AddComponent<ChipView>();
            SetRef(chip, "background", bg);
            SetRef(chip, "label", t);
            return chip;
        }

        static UnityEngine.UI.Button Button(Transform parent, string name, string text, float size, float w, float h,
            ThemeRole bg, ThemeRole fg, float x = 0, float y = 0, bool bold = true, bool themed = false)
        {
            var img = Img(parent, name, bg);
            img.GetComponent<ThemeTarget>().applyColor = themed;   // otherwise set by views
            var rt = img.rectTransform;
            if (parent.GetComponent<VerticalLayoutGroup>() == null) At(rt, x, y, w, h);
            else { rt.sizeDelta = new Vector2(w, h); img.gameObject.AddComponent<LayoutElement>().preferredHeight = h; }
            var btn = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            btn.targetGraphic = img;
            var t = Label(img.transform, "Text", text, size, 0, 0, w, h, fg, FontRole.Body, bold);
            t.GetComponent<ThemeTarget>().applyColor = themed;
            t.alignment = TextAlignmentOptions.Center;
            Stretch(t.rectTransform, 0, 0, 0, 0);
            return btn;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static T Load<T>(string rel) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>($"{Root}/{rel}");
            if (a == null) throw new System.InvalidOperationException("Missing asset " + rel);
            return a;
        }
    }
}
