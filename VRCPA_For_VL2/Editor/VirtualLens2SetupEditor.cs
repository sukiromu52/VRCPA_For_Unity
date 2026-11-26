using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
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
        private const string CurrentVersion = "1.0.2";
        private const string GitHubUrl = "https://github.com/VRCPA/VRC-Photo-Atelier";
        private const string ReleasesUrl = "https://github.com/VRCPA/VRCPA_For_Unity/releases";
        private const string LatestReleaseUrl = ReleasesUrl + "/latest";
        private const string ReleaseTagUrl = ReleasesUrl + "/tag/";
        private const string LanguagePreference = "VRCPA.VirtualLens2Setup.Language";
        private static readonly Version InstalledVersion = Version.Parse(CurrentVersion);
        private static readonly string[] LanguageChoices = { "English", "简体中文", "日本語" };

        private enum Language { English, SimplifiedChinese, Japanese }
        private enum ReleaseStatus { Idle, Checking, UpToDate, UpdateAvailable, NoRelease, Failed }

        private SerializedProperty virtualLensObject;
        private SerializedProperty cameraPose;
        private Language language;
        private string message;
        private UnityWebRequest releaseRequest;
        private ReleaseStatus releaseStatus;
        private string latestTag;
        private Button checkButton;
        private VisualElement releaseRow;
        private Label releaseLabel;
        private Button releaseButton;

        private void OnEnable()
        {
            virtualLensObject = serializedObject.FindProperty("virtualLensObject");
            cameraPose = serializedObject.FindProperty("cameraPose");
            var savedLanguage = EditorPrefs.GetInt(LanguagePreference, (int)Language.English);
            language = savedLanguage >= 0 && savedLanguage < LanguageChoices.Length
                ? (Language)savedLanguage
                : Language.English;
        }

        private void OnDisable()
        {
            var request = releaseRequest;
            releaseRequest = null;
            if (request == null)
                return;

            releaseStatus = ReleaseStatus.Idle;
            request.Abort();
            request.Dispose();
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

            var versionRow = new VisualElement();
            versionRow.style.flexDirection = FlexDirection.Row;
            versionRow.style.flexWrap = Wrap.Wrap;
            versionRow.style.alignItems = Align.Center;
            versionRow.style.marginTop = 4;
            var version = new Label("v" + CurrentVersion) { name = "vrcpa-version" };
            version.style.fontSize = 11;
            version.style.color = subtitle.style.color;
            version.style.marginRight = 5;
            checkButton = new Button(CheckForUpdates) { name = "vrcpa-check-version" };
            checkButton.style.fontSize = 11;
            versionRow.Add(version);
            versionRow.Add(checkButton);
            wordmark.Add(versionRow);
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
                RefreshVersionUI();
                message = null;
                controls.MarkDirtyLayout();
                controls.MarkDirtyRepaint();
            });
            actions.Add(languagePicker);
            cover.Add(actions);

            releaseRow = new VisualElement { name = "vrcpa-release-result" };
            releaseRow.style.width = Length.Percent(100);
            releaseRow.style.flexDirection = FlexDirection.Row;
            releaseRow.style.flexWrap = Wrap.Wrap;
            releaseRow.style.alignItems = Align.Center;
            releaseRow.style.marginTop = 5;
            releaseLabel = new Label { name = "vrcpa-release-message" };
            releaseLabel.style.whiteSpace = WhiteSpace.Normal;
            releaseLabel.style.flexShrink = 1;
            releaseLabel.style.color = foreground;
            releaseButton = new Button(() => Application.OpenURL(ReleaseTagUrl + Uri.EscapeDataString(latestTag)))
            {
                name = "vrcpa-open-release",
            };
            releaseRow.Add(releaseLabel);
            releaseRow.Add(releaseButton);
            cover.Add(releaseRow);
            RefreshVersionUI();
            return cover;
        }

        private void CheckForUpdates()
        {
            if (releaseRequest != null)
                return;

            var request = UnityWebRequest.Head(LatestReleaseUrl);
            request.timeout = 10;
            request.redirectLimit = 3;
            releaseRequest = request;
            releaseStatus = ReleaseStatus.Checking;
            RefreshVersionUI();

            try
            {
                request.SendWebRequest().completed += _ => CompleteReleaseCheck(request);
            }
            catch (InvalidOperationException)
            {
                releaseRequest = null;
                request.Dispose();
                releaseStatus = ReleaseStatus.Failed;
                RefreshVersionUI();
            }
        }

        private void CompleteReleaseCheck(UnityWebRequest request)
        {
            if (releaseRequest != request)
                return;

            releaseRequest = null;
            using (request)
            {
                releaseStatus = request.responseCode == 404 ? ReleaseStatus.NoRelease : ReleaseStatus.Failed;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    // GitHub's latest-release link resolves to the versioned release without an API request.
                    var releaseUrl = request.uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
                    if (string.Equals(releaseUrl, ReleasesUrl, StringComparison.OrdinalIgnoreCase))
                        releaseStatus = ReleaseStatus.NoRelease;
                    else if (releaseUrl.StartsWith(ReleaseTagUrl, StringComparison.OrdinalIgnoreCase))
                        ApplyReleaseTag(Uri.UnescapeDataString(releaseUrl.Substring(ReleaseTagUrl.Length)));
                }
            }
            RefreshVersionUI();
        }

        private void ApplyReleaseTag(string tag)
        {
            latestTag = tag?.Trim();
            if (!Version.TryParse(latestTag?.TrimStart('v', 'V'), out var latestVersion))
            {
                releaseStatus = ReleaseStatus.Failed;
                return;
            }

            releaseStatus = latestVersion > InstalledVersion ? ReleaseStatus.UpdateAvailable : ReleaseStatus.UpToDate;
        }

        private void RefreshVersionUI()
        {
            if (checkButton == null)
                return;

            checkButton.text = Text("Check for updates", "检查更新", "更新を確認");
            checkButton.SetEnabled(releaseStatus != ReleaseStatus.Checking);
            releaseRow.style.display = releaseStatus == ReleaseStatus.Idle ? DisplayStyle.None : DisplayStyle.Flex;
            var hasRelease = releaseStatus == ReleaseStatus.UpToDate || releaseStatus == ReleaseStatus.UpdateAvailable;
            releaseButton.style.display = hasRelease ? DisplayStyle.Flex : DisplayStyle.None;
            releaseButton.text = Text("View release ↗", "查看发布页 ↗", "リリースを表示 ↗");
            releaseLabel.text = releaseStatus switch
            {
                ReleaseStatus.Checking => Text("Checking GitHub…", "正在查询 GitHub…", "GitHub に確認中…"),
                ReleaseStatus.UpToDate => Text($"You’re up to date (GitHub {latestTag}).", $"当前无需更新（GitHub {latestTag}）。", $"更新はありません（GitHub {latestTag}）。"),
                ReleaseStatus.UpdateAvailable => Text($"{latestTag} is available.", $"新版本 {latestTag} 已发布。", $"{latestTag} が公開されました。"),
                ReleaseStatus.NoRelease => Text("No public release found.", "未找到公开发布的版本。", "公開リリースが見つかりませんでした。"),
                ReleaseStatus.Failed => Text("Could not check GitHub. Try again.", "无法查询 GitHub，请重试。", "GitHub を確認できませんでした。再試行してください。"),
                _ => string.Empty,
            };
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
