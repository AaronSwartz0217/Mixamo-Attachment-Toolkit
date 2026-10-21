using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Codex.MixamoAttachments.Editor
{
    public sealed class MixamoAttachmentWindow : EditorWindow
    {
        private GameObject characterRoot;
        private Animator animator;
        private GameObject attachmentSource;
        private GameObject previewInstance;
        private AttachmentType selectedType;
        private WeaponAttachmentProfile selectedProfile;
        private AttachmentState selectedState = AttachmentState.Default;
        private Vector2 scroll;
        private bool showSockets = true;
        private bool showAttachment = true;
        private readonly bool[] socketSelections = new bool[SocketDefinition.All.Count];
        private const string DefaultReferenceModelPath =
            "Assets/Animate/导入的tpose参考/T-pose-W.fbx";
        private bool useReferenceHandRig = true;
        private bool scaleReferenceByHandLength = true;
        private bool includeAttachmentsInFbx;
        private GameObject referenceModel;

        [MenuItem("Tools/Mixamo Attachment Toolkit")]
        public static void Open()
        {
            var window = GetWindow<MixamoAttachmentWindow>();
            window.titleContent = new GUIContent("Mixamo 装备挂载");
            window.minSize = new Vector2(520f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            for (var i = 0; i < socketSelections.Length; i++)
                socketSelections[i] = true;

            if (referenceModel == null)
                referenceModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultReferenceModelPath);

            Selection.selectionChanged += Repaint;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Repaint;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Mixamo 骨骼装备系统", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "插件只在现有骨骼下创建普通 Socket，不会修改 Mixamo 原始骨骼层级。",
                MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawCharacterSection();
            EditorGUILayout.Space(8f);
            DrawSocketSection();
            EditorGUILayout.Space(8f);
            DrawAttachmentSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCharacterSection()
        {
            EditorGUILayout.LabelField("1. 角色", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            characterRoot = (GameObject)EditorGUILayout.ObjectField(
                "角色根节点",
                characterRoot,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
                animator = MixamoBoneResolver.FindAnimator(characterRoot);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("使用当前选择"))
                    AssignSelectedCharacter();

                if (GUILayout.Button("扫描骨架"))
                    animator = MixamoBoneResolver.FindAnimator(characterRoot);
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);

            if (characterRoot == null)
            {
                EditorGUILayout.HelpBox("请选择场景中的角色根节点。", MessageType.Warning);
                return;
            }

            if (EditorUtility.IsPersistent(characterRoot))
            {
                EditorGUILayout.HelpBox(
                    "当前选择的是 Project 资源。请先创建场景工作副本；插件不会直接修改原始 FBX。",
                    MessageType.Warning);

                if (GUILayout.Button("从角色资源创建场景工作副本", GUILayout.Height(28f)))
                    CreateCharacterWorkingCopy();

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选中场景角色"))
                    Selection.activeGameObject = characterRoot;

                if (GUILayout.Button("另存为带挂点角色 Prefab"))
                    SaveCharacterAsPrefab();
            }

            includeAttachmentsInFbx = EditorGUILayout.ToggleLeft(
                "FBX 中包含当前挂载的装备",
                includeAttachmentsInFbx);
            if (GUILayout.Button("导出带参考骨骼的角色 FBX", GUILayout.Height(28f)))
                ExportCharacterAsFbx();

            EditorGUILayout.HelpBox(
                "保持当前角色网格和整体尺寸；T-pose-W 只提供手部辅助骨骼的层级与局部姿态。导入比例自动设为 1。",
                MessageType.Info);

            if (animator == null)
            {
                EditorGUILayout.HelpBox("角色下没有找到 Animator，将仅使用 Mixamo 名称回退查找。", MessageType.Warning);
                return;
            }

            var humanoidStatus = animator.isHuman && animator.avatar != null && animator.avatar.isValid;
            EditorGUILayout.HelpBox(
                humanoidStatus
                    ? "已检测到有效 Humanoid Avatar，将优先使用人体骨骼映射。"
                    : "Animator 不是有效 Humanoid，将使用 Mixamo 骨骼名称回退查找。",
                humanoidStatus ? MessageType.Info : MessageType.Warning);
        }

        private void DrawSocketSection()
        {
            showSockets = EditorGUILayout.Foldout(showSockets, "2. 创建标准 Socket", true);
            if (!showSockets)
                return;

            if (characterRoot == null)
            {
                EditorGUILayout.HelpBox("请先选择角色。", MessageType.Warning);
                return;
            }

            if (EditorUtility.IsPersistent(characterRoot))
            {
                EditorGUILayout.HelpBox("请先在角色区域创建场景工作副本。", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4f);
            useReferenceHandRig = EditorGUILayout.ToggleLeft(
                "参考 T-pose-W 的手部辅助骨骼和挂点（推荐）",
                useReferenceHandRig);
            if (useReferenceHandRig)
            {
                referenceModel = (GameObject)EditorGUILayout.ObjectField(
                    "参考模型",
                    referenceModel,
                    typeof(GameObject),
                    false);
                scaleReferenceByHandLength = EditorGUILayout.ToggleLeft(
                    "按目标与参考手掌长度修正位置比例",
                    scaleReferenceByHandLength);

                if (referenceModel == null)
                    EditorGUILayout.HelpBox("没有找到参考模型，将使用原来的零偏移 Socket。", MessageType.Warning);
            }

            var availableCount = 0;
            var selectedCount = 0;

            for (var i = 0; i < SocketDefinition.All.Count; i++)
            {
                var definition = SocketDefinition.All[i];
                var bone = MixamoBoneResolver.Resolve(characterRoot, animator, definition);
                var existing = SocketCreator.FindExisting(characterRoot, definition);
                var status = existing != null ? "已存在" : bone != null ? "可创建" : "缺少骨骼";

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(bone == null && existing == null))
                        socketSelections[i] = EditorGUILayout.Toggle(socketSelections[i], GUILayout.Width(18f));

                    EditorGUILayout.LabelField(definition.DisplayName, GUILayout.Width(120f));
                    EditorGUILayout.LabelField(definition.SocketName);
                    EditorGUILayout.LabelField(status, GUILayout.Width(68f));
                }

                if (bone != null || existing != null)
                    availableCount++;
                if (socketSelections[i] && (bone != null || existing != null))
                    selectedCount++;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("全选可用项"))
                {
                    for (var i = 0; i < SocketDefinition.All.Count; i++)
                    {
                        var definition = SocketDefinition.All[i];
                        socketSelections[i] =
                            MixamoBoneResolver.Resolve(characterRoot, animator, definition) != null ||
                            SocketCreator.FindExisting(characterRoot, definition) != null;
                    }
                }

                if (GUILayout.Button("全部取消"))
                    Array.Clear(socketSelections, 0, socketSelections.Length);
            }

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"创建/修复所选 Socket（{selectedCount}/{availableCount}）", GUILayout.Height(30f)))
                    CreateSelectedSockets();
            }
        }

        private void DrawAttachmentSection()
        {
            showAttachment = EditorGUILayout.Foldout(showAttachment, "3. 装备配置与预览", true);
            if (!showAttachment)
                return;

            EditorGUI.BeginChangeCheck();
            attachmentSource = (GameObject)EditorGUILayout.ObjectField(
                "装备 Prefab/对象",
                attachmentSource,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
                RecognizeSelectedAttachment();

            selectedType = (AttachmentType)EditorGUILayout.EnumPopup("装备类型", selectedType);

            EditorGUILayout.Space(4f);
            selectedProfile = (WeaponAttachmentProfile)EditorGUILayout.ObjectField(
                "偏移 Profile",
                selectedProfile,
                typeof(WeaponAttachmentProfile),
                false);
            selectedState = (AttachmentState)EditorGUILayout.EnumPopup("编辑状态", selectedState);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(attachmentSource == null || selectedType == AttachmentType.None))
                {
                    if (GUILayout.Button("创建偏移 Profile"))
                        CreateOffsetProfile();
                }

                using (new EditorGUI.DisabledScope(selectedProfile == null || previewInstance == null))
                {
                    if (GUILayout.Button("应用当前 Profile"))
                        ApplySelectedProfilePose();
                }
            }

            using (new EditorGUI.DisabledScope(selectedProfile == null || previewInstance == null))
            {
                if (GUILayout.Button("保存为基础恒定偏移"))
                    SaveProfilePose(true);

                if (GUILayout.Button($"保存为 {selectedState} 状态偏移"))
                    SaveProfilePose(false);
            }

            var recognition = GetRecognition();
            if (attachmentSource != null)
            {
                var text = recognition.IsRecognized
                    ? $"识别结果：{recognition.Type}（依据：{recognition.Source} / {recognition.Evidence}）"
                    : "未能自动识别，请手动选择装备类型。";
                EditorGUILayout.HelpBox(text, recognition.IsRecognized ? MessageType.Info : MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(attachmentSource == null))
                {
                    if (GUILayout.Button("重新识别"))
                        RecognizeSelectedAttachment();
                }

                using (new EditorGUI.DisabledScope(attachmentSource == null || selectedType == AttachmentType.None))
                {
                    if (GUILayout.Button("写入装备标记"))
                        WriteAttachmentMarker(attachmentSource, selectedType, null);
                }
            }

            using (new EditorGUI.DisabledScope(
                       characterRoot == null ||
                       EditorUtility.IsPersistent(characterRoot) ||
                       attachmentSource == null ||
                       selectedType == AttachmentType.None))
            {
                if (GUILayout.Button("挂载并创建预览", GUILayout.Height(28f)))
                    CreatePreview();
            }

            using (new EditorGUI.DisabledScope(previewInstance == null))
            {
                if (GUILayout.Button("保存当前预览姿态"))
                    SavePreviewPose();

                if (GUILayout.Button("恢复已保存姿态"))
                    RestorePreviewPose();

                if (GUILayout.Button("删除预览"))
                    DeletePreview();
            }

            EditorGUILayout.HelpBox(
                "请调整预览对象自身的 Local Position、Local Rotation 和 Local Scale。不要为单件装备移动角色骨骼。",
                MessageType.None);
        }

        private void AssignSelectedCharacter()
        {
            var selected = Selection.activeGameObject;
            if (selected == null)
                return;

            characterRoot = selected;
            animator = MixamoBoneResolver.FindAnimator(characterRoot);
        }

        private void CreateCharacterWorkingCopy()
        {
            if (characterRoot == null || !EditorUtility.IsPersistent(characterRoot))
                return;

            GameObject instance;
            if (PrefabUtility.IsPartOfPrefabAsset(characterRoot))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(characterRoot);
            else
                instance = Instantiate(characterRoot);

            if (instance == null)
            {
                EditorUtility.DisplayDialog("创建失败", "无法从所选资源创建角色实例。", "确定");
                return;
            }

            instance.name = $"{characterRoot.name}_AttachmentSetup";
            Undo.RegisterCreatedObjectUndo(instance, "创建角色装备工作副本");
            characterRoot = instance;
            animator = MixamoBoneResolver.FindAnimator(characterRoot);
            Selection.activeGameObject = characterRoot;
            MarkSceneDirty();
            ShowNotification(new GUIContent("已创建场景工作副本"));
        }

        private void SaveCharacterAsPrefab()
        {
            if (characterRoot == null || EditorUtility.IsPersistent(characterRoot))
                return;

            var defaultName = characterRoot.name.Replace("_AttachmentSetup", string.Empty) +
                              "_WithAttachments.prefab";
            var path = EditorUtility.SaveFilePanelInProject(
                "保存带挂点角色 Prefab",
                defaultName,
                "prefab",
                "请选择新 Prefab 的保存位置。原始 FBX 不会被覆盖。",
                "Assets");

            if (string.IsNullOrWhiteSpace(path))
                return;

            var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(
                characterRoot,
                path,
                InteractionMode.UserAction);

            if (saved == null)
            {
                EditorUtility.DisplayDialog("保存失败", "角色 Prefab 保存失败，请检查 Console。", "确定");
                return;
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = saved;
            ShowNotification(new GUIContent("已保存带挂点角色 Prefab"));
        }

        private void ExportCharacterAsFbx()
        {
            if (characterRoot == null || EditorUtility.IsPersistent(characterRoot))
                return;

            if (useReferenceHandRig && referenceModel != null)
            {
                ReferenceHandRigCopier.Apply(
                    characterRoot,
                    animator,
                    referenceModel,
                    scaleReferenceByHandLength);
                MarkSceneDirty();
            }

            if (!CharacterFbxExporter.IsAvailable)
            {
                EditorUtility.DisplayDialog(
                    "需要 FBX Exporter",
                    "请在 Window > Package Manager 中安装 Unity 官方 FBX Exporter（com.unity.formats.fbx），然后重新点击导出。",
                    "确定");
                return;
            }

            var defaultName = characterRoot.name.Replace("_AttachmentSetup", string.Empty) +
                              "_WithReferenceBones.fbx";
            var path = EditorUtility.SaveFilePanel(
                "导出带参考骨骼的角色 FBX",
                Application.dataPath,
                defaultName,
                "fbx");
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!CharacterFbxExporter.Export(characterRoot, path, includeAttachmentsInFbx, out var error))
            {
                EditorUtility.DisplayDialog("FBX 导出失败", error, "确定");
                return;
            }

            ShowNotification(new GUIContent("角色 FBX 已导出"));
            Debug.Log($"[Mixamo Attachments] FBX 已导出：{path}", characterRoot);
        }

        private void CreateSelectedSockets()
        {
            var created = 0;
            var missing = 0;
            AttachmentSocket lastSocket = null;

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("创建 Mixamo 装备 Socket");

            for (var i = 0; i < SocketDefinition.All.Count; i++)
            {
                if (!socketSelections[i])
                    continue;

                var socket = SocketCreator.CreateOrRepair(characterRoot, animator, SocketDefinition.All[i]);
                if (socket == null)
                {
                    missing++;
                    continue;
                }

                lastSocket = socket;
                created++;
            }

            var referenceResult = default(ReferenceHandRigCopier.Result);
            if (useReferenceHandRig && referenceModel != null)
            {
                referenceResult = ReferenceHandRigCopier.Apply(
                    characterRoot,
                    animator,
                    referenceModel,
                    scaleReferenceByHandLength);
            }

            EnsureController();
            Undo.CollapseUndoOperations(undoGroup);
            MarkSceneDirty();

            if (lastSocket != null)
                Selection.activeGameObject = lastSocket.gameObject;

            var message = $"已处理 {created} 个 Socket，缺失 {missing} 个";
            if (useReferenceHandRig && referenceModel != null)
            {
                message += $"；参考骨骼新增 {referenceResult.CreatedBones}，挂点更新 {referenceResult.UpdatedSockets}";
                Debug.Log(
                    $"[Mixamo Attachments] 参考手部比例：Right={referenceResult.RightScale:F3}, " +
                    $"Left={referenceResult.LeftScale:F3}",
                    characterRoot);
            }
            ShowNotification(new GUIContent(message));
        }

        private void EnsureController()
        {
            if (characterRoot == null || characterRoot.GetComponent<AttachmentController>() != null)
                return;

            Undo.AddComponent<AttachmentController>(characterRoot);
        }

        private void CreateOffsetProfile()
        {
            if (attachmentSource == null || selectedType == AttachmentType.None)
                return;

            var path = EditorUtility.SaveFilePanelInProject(
                "创建武器偏移 Profile",
                attachmentSource.name + "_AttachmentProfile.asset",
                "asset",
                "选择偏移配置的保存位置。",
                "Assets");

            if (string.IsNullOrWhiteSpace(path))
                return;

            var profile = CreateInstance<WeaponAttachmentProfile>();
            profile.Configure(attachmentSource, selectedType);
            profile.SetBasePose(previewInstance != null
                ? AttachmentPose.Capture(previewInstance.transform)
                : AttachmentPose.Identity);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            selectedProfile = profile;
            AssignProfile(attachmentSource, profile);
            if (previewInstance != null)
                AssignProfile(previewInstance, profile);
            Selection.activeObject = profile;
        }

        private void SaveProfilePose(bool saveAsBase)
        {
            if (selectedProfile == null || previewInstance == null)
                return;

            Undo.RecordObject(selectedProfile, "保存装备状态偏移");
            var pose = AttachmentPose.Capture(previewInstance.transform);
            if (saveAsBase || selectedState == AttachmentState.Default)
                selectedProfile.SetBasePose(pose);
            else
                selectedProfile.SetStateFromFinalPose(selectedState, pose);

            selectedProfile.Configure(attachmentSource, selectedType);
            EditorUtility.SetDirty(selectedProfile);
            AssetDatabase.SaveAssets();
            AssignProfile(attachmentSource, selectedProfile);
            AssignProfile(previewInstance, selectedProfile);
            ShowNotification(new GUIContent(saveAsBase ? "基础偏移已保存" : $"{selectedState} 偏移已保存"));
        }

        private void ApplySelectedProfilePose()
        {
            if (selectedProfile == null || previewInstance == null)
                return;

            Undo.RecordObject(previewInstance.transform, "应用装备偏移 Profile");
            selectedProfile.Evaluate(selectedState).Apply(previewInstance.transform);
            MarkSceneDirty();
        }

        private static void AssignProfile(GameObject target, WeaponAttachmentProfile profile)
        {
            if (target == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrWhiteSpace(assetPath) && PrefabUtility.IsPartOfPrefabAsset(target))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                try
                {
                    var marker = prefabRoot.GetComponent<AttachmentItem>();
                    if (marker == null)
                        marker = prefabRoot.AddComponent<AttachmentItem>();
                    marker.SetProfile(profile);
                    EditorUtility.SetDirty(marker);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }

                return;
            }

            var sceneMarker = target.GetComponent<AttachmentItem>();
            if (sceneMarker == null)
                sceneMarker = Undo.AddComponent<AttachmentItem>(target);
            else
                Undo.RecordObject(sceneMarker, "指定装备偏移 Profile");
            sceneMarker.SetProfile(profile);
            EditorUtility.SetDirty(sceneMarker);
        }

        private RecognitionResult GetRecognition()
        {
            if (attachmentSource == null)
                return default;

            var path = AssetDatabase.GetAssetPath(attachmentSource);
            return AttachmentRecognizer.Recognize(attachmentSource, path);
        }

        private void RecognizeSelectedAttachment()
        {
            var recognition = GetRecognition();
            selectedType = recognition.IsRecognized ? recognition.Type : AttachmentType.None;
            selectedProfile = attachmentSource != null
                ? attachmentSource.GetComponent<AttachmentItem>()?.Profile
                : null;
        }

        private void CreatePreview()
        {
            if (!SocketDefinition.TryGet(selectedType, out var definition))
                return;

            var socket = SocketCreator.CreateOrRepair(characterRoot, animator, definition);
            if (socket == null)
            {
                EditorUtility.DisplayDialog(
                    "无法创建预览",
                    $"没有找到 {definition.DisplayName} 对应的骨骼。",
                    "确定");
                return;
            }

            DeletePreview();

            if (EditorUtility.IsPersistent(attachmentSource))
                previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(attachmentSource, socket.transform);
            else
                previewInstance = Instantiate(attachmentSource, socket.transform, false);

            if (previewInstance == null)
                return;

            previewInstance.name = $"{attachmentSource.name} [Attachment Preview]";
            Undo.RegisterCreatedObjectUndo(previewInstance, "创建装备预览");

            var sourceMarker = attachmentSource.GetComponent<AttachmentItem>();
            if (sourceMarker != null)
                sourceMarker.ApplyLocalPose(previewInstance.transform);
            else
            {
                previewInstance.transform.localPosition = Vector3.zero;
                previewInstance.transform.localRotation = Quaternion.identity;
                previewInstance.transform.localScale = Vector3.one;
            }

            WriteAttachmentMarker(previewInstance, selectedType, previewInstance.transform);
            Selection.activeGameObject = previewInstance;
            MarkSceneDirty();
        }

        private void SavePreviewPose()
        {
            if (previewInstance == null)
                return;

            WriteAttachmentMarker(previewInstance, selectedType, previewInstance.transform);
            WriteAttachmentMarker(attachmentSource, selectedType, previewInstance.transform);
            ShowNotification(new GUIContent("装备姿态已保存"));
        }

        private void RestorePreviewPose()
        {
            if (previewInstance == null || attachmentSource == null)
                return;

            var marker = attachmentSource.GetComponent<AttachmentItem>();
            if (marker == null)
                return;

            Undo.RecordObject(previewInstance.transform, "恢复装备姿态");
            marker.ApplyLocalPose(previewInstance.transform);
            MarkSceneDirty();
        }

        private static void WriteAttachmentMarker(GameObject target, AttachmentType type, Transform pose)
        {
            if (target == null || !SocketDefinition.TryGet(type, out var definition))
                return;

            var assetPath = AssetDatabase.GetAssetPath(target);
            if (!string.IsNullOrWhiteSpace(assetPath) && PrefabUtility.IsPartOfPrefabAsset(target))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
                try
                {
                    var marker = prefabRoot.GetComponent<AttachmentItem>();
                    if (marker == null)
                        marker = prefabRoot.AddComponent<AttachmentItem>();

                    marker.Configure(type, definition.SocketId);
                    if (pose != null)
                        marker.CaptureLocalPose(pose);

                    EditorUtility.SetDirty(marker);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }

                return;
            }

            var sceneMarker = target.GetComponent<AttachmentItem>();
            if (sceneMarker == null)
                sceneMarker = Undo.AddComponent<AttachmentItem>(target);
            else
                Undo.RecordObject(sceneMarker, "配置装备");

            sceneMarker.Configure(type, definition.SocketId);
            if (pose != null)
                sceneMarker.CaptureLocalPose(pose);

            EditorUtility.SetDirty(sceneMarker);
            if (target.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(target.scene);
        }

        private void DeletePreview()
        {
            if (previewInstance == null)
                return;

            Undo.DestroyObjectImmediate(previewInstance);
            previewInstance = null;
            MarkSceneDirty();
        }

        private void MarkSceneDirty()
        {
            if (characterRoot == null)
                return;

            var scene = characterRoot.scene;
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
