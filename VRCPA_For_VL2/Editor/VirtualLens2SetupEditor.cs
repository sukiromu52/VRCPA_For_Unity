using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VRC.SDK3.Avatars.Components;

namespace VRCPA.Editor
{
    [CustomEditor(typeof(VirtualLens2Setup))]
    internal sealed class VirtualLens2SetupEditor : UnityEditor.Editor
    {
        private const string SettingsType = "VirtualLens2.VirtualLensSettings";
        private const string PoseSourceProperty = "externalPoseSource";
        private const string CameraPosePath = "Anchor/X/Y/Z/yaw/Pitch/CameraPOS";
        private const string GitHubUrl = "https://github.com/sukiromu52/VRC-Photo-Atelier";
        private const string LanguagePreference = "VRCPA.VirtualLens2Setup.Language";
        private static readonly string[] LanguageChoices = { "English", "简体中文", "日本語" };

        private enum Language { English, SimplifiedChinese, Japanese }

        private SerializedProperty virtualLensObject;
        private SerializedProperty cameraPose;
        private Language language;
        private string message;

        private void OnEnable()
        {
            virtualLensObject = serializedObject.FindProperty("virtualLensObject");
            cameraPose = serializedObject.FindProperty("cameraPose");
            var savedLanguage = EditorPrefs.GetInt(LanguagePreference, (int)Language.English);
            language = savedLanguage >= 0 && savedLanguage < LanguageChoices.Length
                ? (Language)savedLanguage
                : Language.English;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var controls = new IMGUIContainer(OnInspectorGUI);
            root.Add(CreateCover(controls));
            root.Add(controls);
            return root;
        }

        private VisualElement CreateCover(IMGUIContainer controls)
        {
            var dark = EditorGUIUtility.isProSkin;
            Color surface = dark ? new Color32(28, 28, 30, 235) : new Color32(255, 255, 255, 225);
            Color foreground = dark ? new Color32(245, 245, 247, 255) : new Color32(29, 29, 31, 255);
            Color accent = dark ? new Color32(10, 132, 255, 255) : new Color32(0, 113, 227, 255);
            Color linkText = dark ? new Color32(117, 188, 255, 255) : new Color32(0, 82, 168, 255);
            var border = dark ? new Color(1, 1, 1, 0.09f) : new Color(0, 0, 0, 0.08f);

            var cover = new VisualElement { name = "vrcpa-cover" };
            var card = cover.style;
            card.flexDirection = FlexDirection.Row;
            card.flexWrap = Wrap.Wrap;
            card.alignItems = Align.Center;
            card.justifyContent = Justify.SpaceBetween;
            card.paddingLeft = card.paddingRight = 12;
            card.paddingTop = card.paddingBottom = 10;
            card.marginTop = 3;
            card.marginBottom = 10;
            card.backgroundColor = surface;
            card.borderTopWidth = card.borderRightWidth = card.borderBottomWidth = card.borderLeftWidth = 1;
            card.borderTopColor = card.borderRightColor = card.borderBottomColor = card.borderLeftColor = border;
            card.borderTopLeftRadius = card.borderTopRightRadius = card.borderBottomLeftRadius = card.borderBottomRightRadius = 12;

            var identity = new VisualElement { name = "vrcpa-brand" };
            identity.style.flexDirection = FlexDirection.Row;
            identity.style.alignItems = Align.Center;
            identity.style.marginRight = 12;

            var wordmark = new VisualElement();
            var title = new Label("For VirtualLens2") { name = "vrcpa-title" };
            title.style.fontSize = 22;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = foreground;
            var subtitle = new Label("VRC Photo Atelier");
            subtitle.style.fontSize = 12;
            subtitle.style.color = new Color(foreground.r, foreground.g, foreground.b, 0.72f);
            wordmark.Add(title);
            wordmark.Add(subtitle);
            identity.Add(wordmark);
            cover.Add(identity);

            var actions = new VisualElement();
            actions.style.minWidth = 86;
            actions.style.flexShrink = 0;

            var github = new Button(() => Application.OpenURL(GitHubUrl))
            {
                name = "vrcpa-github",
                text = "GitHub ↗",
                tooltip = GitHubUrl,
            };
            var link = github.style;
            link.flexShrink = 0;
            link.paddingLeft = link.paddingRight = 9;
            link.paddingTop = link.paddingBottom = 4;
            link.marginLeft = link.marginRight = 0;
            link.marginTop = link.marginBottom = 3;
            link.fontSize = 12;
            link.color = linkText;
            link.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.11f);
            var linkBorder = new Color(accent.r, accent.g, accent.b, 0.30f);
            link.borderTopColor = link.borderRightColor = link.borderBottomColor = link.borderLeftColor = linkBorder;
            link.borderTopLeftRadius = link.borderTopRightRadius = link.borderBottomLeftRadius = link.borderBottomRightRadius = 12;
            actions.Add(github);

            var languagePicker = new DropdownField(new List<string>(LanguageChoices), (int)language)
            {
                name = "vrcpa-language",
                tooltip = Text("Inspector language", "面板语言", "インスペクターの言語"),
            };
            languagePicker.style.marginLeft = languagePicker.style.marginRight = 0;
            languagePicker.style.marginTop = languagePicker.style.marginBottom = 2;
            languagePicker.style.fontSize = 11;
            languagePicker.RegisterValueChangedCallback(_ =>
            {
                language = (Language)languagePicker.index;
                EditorPrefs.SetInt(LanguagePreference, (int)language);
                languagePicker.tooltip = Text("Inspector language", "面板语言", "インスペクターの言語");
                message = null;
                controls.MarkDirtyLayout();
                controls.MarkDirtyRepaint();
            });
            actions.Add(languagePicker);
            cover.Add(actions);
            return cover;
        }

        private string Text(string english, string chinese, string japanese)
        {
            return language switch
            {
                Language.SimplifiedChinese => chinese,
                Language.Japanese => japanese,
                _ => english,
            };
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(Text(
                    "Exit Play Mode to configure the external pose.",
                    "请退出 Play Mode 再设置外部位姿。",
                    "外部ポーズを設定するには、再生モードを終了してください。"), MessageType.Info);
                return;
            }

            serializedObject.Update();
            if (GUILayout.Button(Text("Auto Detect", "自动检测", "自動検出")))
                Detect();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(virtualLensObject, new GUIContent(Text(
                "VirtualLens2 Object", "VirtualLens2 对象", "VirtualLens2 オブジェクト")));
            EditorGUILayout.PropertyField(cameraPose, new GUIContent(Text(
                "CameraPOS", "相机位姿 (CameraPOS)", "カメラ姿勢 (CameraPOS)")));
            if (EditorGUI.EndChangeCheck())
                message = null;
            serializedObject.ApplyModifiedProperties();

            var setup = (VirtualLens2Setup)target;
            var settings = FindSettings(setup.VirtualLensObject);
            using var lens = settings ? new SerializedObject(settings) : null;
            var source = lens?.FindProperty(PoseSourceProperty);
            var error = Validate(setup, settings, source);

            if (source != null && source.propertyType == SerializedPropertyType.ObjectReference)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField(Text("Current Pose Source", "当前位姿来源", "現在のポーズ参照"),
                        source.objectReferenceValue, typeof(Transform), true);
            }

            var alreadyBound = error == null && source.objectReferenceValue == setup.CameraPose;
            if (error != null)
                EditorGUILayout.HelpBox(error, MessageType.Info);
            else if (alreadyBound)
                EditorGUILayout.HelpBox(Text(
                    "VirtualLens2 is bound to this CameraPOS.",
                    "VirtualLens2 已绑定到这个 CameraPOS。",
                    "VirtualLens2 にこの CameraPOS が設定されています。"), MessageType.Info);
            else if (source.objectReferenceValue)
                EditorGUILayout.HelpBox(Text(
                    "Binding replaces the current source. Use Unity Undo to restore it.",
                    "绑定会替换当前来源，可用 Unity Undo 撤销。",
                    "現在の参照先が置き換わります。Unity の「元に戻す」で復元できます。"), MessageType.Info);

            using (new EditorGUI.DisabledScope(error != null || alreadyBound))
            {
                if (GUILayout.Button(Text("Bind External Pose Source", "绑定外部位姿来源", "外部ポーズ参照を設定")))
                    message = Bind(setup);
            }

            if (!string.IsNullOrEmpty(message))
                EditorGUILayout.HelpBox(message, MessageType.Info);
        }

        private void Detect()
        {
            var setup = (VirtualLens2Setup)target;
            var avatar = GetAvatar(setup.transform);
            if (!avatar)
            {
                message = Text(
                    "Place VRCPA under an avatar with an Avatar Descriptor in the Hierarchy.",
                    "请在 Hierarchy 中把 VRCPA 放进带有 Avatar Descriptor 的 Avatar。",
                    "Hierarchy で、Avatar Descriptor のあるアバターの下に VRCPA を置いてください。");
                return;
            }

            cameraPose.objectReferenceValue = setup.transform.Find(CameraPosePath);
            var matches = avatar.gameObject.scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .Where(component => IsSettings(component) && GetSettingsAvatar(component) == avatar)
                .ToArray();

            var selected = FindSettings(virtualLensObject.objectReferenceValue as GameObject);
            if (!matches.Contains(selected))
                selected = matches.Length == 1 ? matches[0] : null;
            virtualLensObject.objectReferenceValue = selected ? selected.gameObject : null;

            if (!selected)
                message = Text(
                    $"Found {matches.Length} VirtualLens2 setups for this avatar. Drag the intended object from the Hierarchy.",
                    $"在这个 Avatar 下找到 {matches.Length} 个 VirtualLens2 配置。请从 Hierarchy 手动拖入。",
                    $"このアバターの VirtualLens2 設定が {matches.Length} 件見つかりました。対象を Hierarchy からドラッグしてください。");
            else if (!cameraPose.objectReferenceValue)
                message = Text(
                    "CameraPOS was not found under VRCPA. Drag it from the Hierarchy.",
                    "VRCPA 下未找到 CameraPOS。请从 Hierarchy 手动拖入。",
                    "VRCPA の下に CameraPOS が見つかりません。Hierarchy からドラッグしてください。");
            else
                message = null;
        }

        private static MonoBehaviour FindSettings(GameObject gameObject)
        {
            return gameObject ? gameObject.GetComponents<MonoBehaviour>().FirstOrDefault(IsSettings) : null;
        }

        private static bool IsSettings(MonoBehaviour component)
        {
            return component && component.GetType().FullName == SettingsType;
        }

        private static VRCAvatarDescriptor GetAvatar(Transform transform)
        {
            return transform && !EditorUtility.IsPersistent(transform)
                ? transform.GetComponentInParent<VRCAvatarDescriptor>(true)
                : null;
        }

        private static VRCAvatarDescriptor GetSettingsAvatar(MonoBehaviour settings)
        {
            if (!settings || EditorUtility.IsPersistent(settings))
                return null;

            using var lens = new SerializedObject(settings);
            if (IsDestructive(lens) && lens.FindProperty("avatar")?.objectReferenceValue is GameObject avatar)
                return GetAvatar(avatar.transform);

            return GetAvatar(settings.transform);
        }

        private static bool IsDestructive(SerializedObject settings)
        {
            var mode = settings.FindProperty("buildMode");
            return mode != null && mode.propertyType == SerializedPropertyType.Enum &&
                mode.enumValueIndex >= 0 && mode.enumValueIndex < mode.enumNames.Length &&
                mode.enumNames[mode.enumValueIndex] == "Destructive";
        }

        private string Validate(VirtualLens2Setup setup, MonoBehaviour settings, SerializedProperty source)
        {
            if (!settings)
                return Text(
                    "Drag in the object with VirtualLens Settings, or click Auto Detect.",
                    "请拖入带有 VirtualLens Settings 组件的对象，或点击自动检测。",
                    "VirtualLens Settings のあるオブジェクトをドラッグするか、「自動検出」を押してください。");
            if (source == null || source.propertyType != SerializedPropertyType.ObjectReference)
                return Text(
                    "This version of VirtualLens2 does not expose an External Pose Source field.",
                    "这个 VirtualLens2 版本没有可绑定的 External Pose Source 字段。",
                    "このバージョンの VirtualLens2 には設定可能な External Pose Source がありません。");
            if (!setup.CameraPose)
                return Text(
                    "Drag in the VRCPA CameraPOS, or click Auto Detect.",
                    "请拖入 VRCPA 的 CameraPOS，或点击自动检测。",
                    "VRCPA の CameraPOS をドラッグするか、「自動検出」を押してください。");

            var avatar = GetAvatar(setup.transform);
            if (!avatar || GetAvatar(setup.CameraPose) != avatar || GetSettingsAvatar(settings) != avatar ||
                settings.gameObject.scene != setup.gameObject.scene)
                return Text(
                    "VRCPA, VirtualLens2, and CameraPOS must belong to the same avatar in the same scene.",
                    "VRCPA、VirtualLens2 和 CameraPOS 必须属于同一个场景中的 Avatar。",
                    "VRCPA、VirtualLens2、CameraPOS は同じシーンにある同一のアバターに属している必要があります。");

            return null;
        }

        private string Bind(VirtualLens2Setup setup)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return Text(
                    "Exit Play Mode before binding the pose source.",
                    "请退出 Play Mode 再绑定。",
                    "参照先を設定するには、再生モードを終了してください。");

            var settings = FindSettings(setup.VirtualLensObject);
            using var lens = settings ? new SerializedObject(settings) : null;
            var source = lens?.FindProperty(PoseSourceProperty);
            var error = Validate(setup, settings, source);
            if (error != null)
                return error;
            if (source.objectReferenceValue == setup.CameraPose)
                return Text(
                    "VirtualLens2 is bound to this CameraPOS.",
                    "VirtualLens2 已绑定到这个 CameraPOS。",
                    "VirtualLens2 にこの CameraPOS が設定されています。");

            Undo.SetCurrentGroupName("Bind VRCPA VirtualLens2 CameraPOS");
            source.objectReferenceValue = setup.CameraPose;
            lens.ApplyModifiedProperties();

            return IsDestructive(lens)
                ? Text(
                    "Pose source assigned. Apply the VirtualLens Settings again.",
                    "绑定已写入。请在 VirtualLens Settings 中重新 Apply。",
                    "参照先を設定しました。VirtualLens Settings で Apply を再実行してください。")
                : Text(
                    "Pose source assigned. Save the scene or prefab before the next build.",
                    "绑定已写入。请在下次构建前保存场景或 Prefab。",
                    "参照先を設定しました。次回のビルド前にシーンまたは Prefab を保存してください。");
        }
    }
}
