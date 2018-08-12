#if UNITY_EDITOR
//-----------------------------------------------------------------------Window/Odin Inspector
// <copyright file="OdinProjectValidator.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Sirenix.OdinInspector.Editor
{
    using System.Collections.Generic;
    using System.Linq;
    using Sirenix.Utilities;
    using Sirenix.Utilities.Editor;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// The Odin Scene Validator window.
    /// </summary>
    public class OdinProjectValidator : EditorWindow {

        enum ScanType {
            Prefab,
            ScriptableObject,
            Scene
        }

        List<IValidationInfo> validationInfos;
        int errorCount;
        bool isScanning;
        float offsetLeftSide;
        Vector2 scrollLeftSide;
        Vector2 scrollRightRightSide;
        IValidationInfo selectedValidationInfo;
        IValidationInfo validationInfoToSelect;
        bool triggerScan;
        int validCount;
        int warningCount;
        bool includeValid;
        bool includeErrors = true;
        bool includeWarnings = true;

        /// <summary>
        /// Opens the window.
        /// </summary>
        [MenuItem("Tools/Odin Inspector/Project Validator")]
        public static void OpenWindow() {
            var rect = GUIHelper.GetEditorWindowRect();
            var size = new Vector2(800, 600);
            var window = GetWindow<OdinProjectValidator>();
            window.Show();
            window.position = new Rect(rect.center - size * 0.5f, size);
            window.wantsMouseMove = true;
            window.titleContent = new GUIContent("Odin Project Validator");
        }

        void OnGUI() {
            if (this.validationInfos == null) {
                this.FullScan(ScanType.Scene);
            }

            if (Event.current.type == EventType.Layout) {
                if (this.validationInfoToSelect != null) {
                    this.selectedValidationInfo = this.validationInfoToSelect;
                    this.validationInfoToSelect = null;
                }
            }

            this.DrawToolbar();

            EditorGUILayout.BeginHorizontal(); {
                var rect = EditorGUILayout.BeginVertical(GUILayoutOptions.Width(300 + this.offsetLeftSide)); {
                    this.scrollLeftSide = EditorGUILayout.BeginScrollView(this.scrollLeftSide);
                    this.DrawHierachy();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(); {
                    SirenixEditorGUI.DrawSolidRect(GUIHelper.GetCurrentLayoutRect(), SirenixGUIStyles.DarkEditorBackground);
                    this.scrollRightRightSide = EditorGUILayout.BeginScrollView(this.scrollRightRightSide);
                    this.DrawPropertyTree();
                    EditorGUILayout.EndScrollView();
                }
                EditorGUILayout.EndVertical();

                rect.xMin = rect.xMax - 4;
                rect.x += 4;
                SirenixEditorGUI.DrawSolidRect(rect, SirenixGUIStyles.BorderColor);
                rect.xMin -= 2;
                rect.xMax += 2;
                this.offsetLeftSide = this.offsetLeftSide + SirenixEditorGUI.SlideRect(rect).x;
            }
            EditorGUILayout.EndHorizontal();

            if (this.isScanning && (Event.current.type == EventType.Repaint)) {
                this.warningCount = 0;
                this.errorCount = 0;
                this.validCount = 0;

                foreach(var o in this.validationInfos) {
                    if (o.ErrorCount == 0 && o.WarningCount == 0) {
                        this.validCount++;
                    }
                    this.errorCount += o.ErrorCount;
                    this.warningCount += o.WarningCount;
                }
                this.validationInfos = this.validationInfos.OrderByDescending(x => x.ErrorCount).ThenByDescending(x => x.WarningCount).ThenBy(x => x.Name).ToList();
                this.isScanning = false;
            }
            else if (this.triggerScan && Event.current.type == EventType.Repaint) {
                this.isScanning = true;
                this.triggerScan = false;
                this.Repaint();
            }

            this.RepaintIfRequested();
        }

        void DrawHierachy() {
            if (this.validationInfos != null) {
                SirenixEditorGUI.BeginVerticalList();
                for (int i = 0; i < this.validationInfos.Count; i++) {
                    this.validationInfos[i].DrawMenu();
                }
                SirenixEditorGUI.EndVerticalList();
            }
        }

        void DrawPropertyTree() {
            if (this.validationInfos != null && this.validationInfos.Count > 0) {
                if (this.isScanning) {
                    // Spamming the progress bar dialog a lot across several frames apparently crashes the editor on Mac OSX.
                    bool showProgress = Application.platform != RuntimePlatform.OSXEditor;

                    float total = this.validationInfos.Count * 2;
                    float offset = Event.current.type == EventType.Layout ? 0 : this.validationInfos.Count;
                    for (int i = 0; i < this.validationInfos.Count; i++) {

                        if (showProgress) {
                            float t = (offset + i) / total;
                            EditorUtility.DisplayProgressBar("Scanning in " + Event.current.type, this.validationInfos[i].Name, t);
                        }

                        this.validationInfos[i].DrawPropertyTree();
                    }

                    if (showProgress) {
                        EditorUtility.ClearProgressBar();
                    }
                }
                else {
                    if (this.selectedValidationInfo != null) {
                        this.selectedValidationInfo.DrawPropertyTree();
                    }
                }
            }
        }

        void DrawToolbar() {
            this.includeWarnings = EditorPrefs.GetBool("OdinValidation.includeWarnings", this.includeWarnings);
            this.includeErrors = EditorPrefs.GetBool("OdinValidation.includeErrors", this.includeErrors);
            this.includeValid = EditorPrefs.GetBool("OdinValidation.includeValid", this.includeValid);

            SirenixEditorGUI.BeginHorizontalToolbar(); {
                //GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                //this.includeValid = SirenixEditorGUI.ToolbarButton(new GUIContent("      " + this.validCount + " "), this.includeValid) ? !this.includeValid : this.includeValid;
                this.includeValid = SirenixEditorGUI.ToolbarToggle(this.includeValid, new GUIContent("      " + this.validCount + " "));
                GUIHelper.PushColor(Color.green);
                GUI.DrawTexture(new Rect(GUILayoutUtility.GetLastRect().position + new Vector2(6, 4), new Vector2(16, 16)), EditorIcons.Checkmark.Highlighted, ScaleMode.ScaleToFit);
                GUIHelper.PopColor();

                //this.includeWarnings = SirenixEditorGUI.ToolbarButton(new GUIContent("      " + this.WarningCount + " "), this.includeWarnings) ? !this.includeWarnings : this.includeWarnings;
                this.includeWarnings = SirenixEditorGUI.ToolbarToggle(this.includeWarnings, new GUIContent("      " + this.warningCount + " "));
                GUI.DrawTexture(new Rect(GUILayoutUtility.GetLastRect().position + Vector2.one * 2, new Vector2(20, 20)), EditorIcons.UnityWarningIcon, ScaleMode.ScaleToFit);

                //this.includeErrors = SirenixEditorGUI.ToolbarButton(new GUIContent("      " + this.ErrorCount + " "), this.includeErrors) ? !this.includeErrors : this.includeErrors;
                this.includeErrors = SirenixEditorGUI.ToolbarToggle(this.includeErrors, new GUIContent("      " + this.errorCount + " "));
                GUI.DrawTexture(new Rect(GUILayoutUtility.GetLastRect().position + Vector2.one * 2, new Vector2(22, 22)), EditorIcons.UnityErrorIcon, ScaleMode.ScaleToFit);
                //GL.sRGBWrite = false;

                GUILayout.FlexibleSpace();

                if (SirenixEditorGUI.ToolbarButton(GUIHelper.TempContent("  Scan Scene  "))) {
                    this.FullScan(ScanType.Scene);
                }
                if (SirenixEditorGUI.ToolbarButton(GUIHelper.TempContent("  Scan ScriptObjs  "))) {
                    this.FullScan(ScanType.ScriptableObject);
                }
                if (SirenixEditorGUI.ToolbarButton(GUIHelper.TempContent("  Scan Prefabs  "))) {
                    this.FullScan(ScanType.Prefab);
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();

            EditorPrefs.SetBool("OdinValidation.includeWarnings", this.includeWarnings);
            EditorPrefs.SetBool("OdinValidation.includeErrors", this.includeErrors);
            EditorPrefs.SetBool("OdinValidation.includeValid", this.includeValid);
        }

        void FullScan(ScanType scanType) {
            Object prevSelected = this.selectedValidationInfo == null ? (Behaviour)null : this.selectedValidationInfo.Behaviour ?? (Object)this.selectedValidationInfo.Obj;

            var drawingConfig = InspectorConfig.Instance.DrawingConfig;
            var odinEditorType = typeof(OdinEditor);

            switch(scanType){
                case ScanType.Scene: 
                    this.validationInfos = Resources.FindObjectsOfTypeAll<Transform>()
                        .Where(x => (x.gameObject.scene.IsValid() && (x.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0))
                        .Select(x => new { go = x.gameObject, components = x.GetComponents(typeof(Behaviour)) })
                        .SelectMany(x => x.components.Select(c => new { go = x.go, component = c }))
                        .Where(x => x.component == null || drawingConfig.GetEditorType(x.component.GetType()) == odinEditorType)
                        .OrderBy(x => x.go.name)
                        .ThenBy(x => x.component == null ? "" : x.component.name)
                        .Select(x => new { tree = x.component == null ? (PropertyTree)null : PropertyTree.Create(new SerializedObject(x.component)), go = x.go })
                        .Examine(x => { if (x.tree != null) x.tree.UpdateTree(); })
                        .Where(x => x.tree == null || x.tree.RootPropertyCount != 0)
                        .Select((x, i) => new BehaviourValidationInfo(x.tree, this, x.go))
                        .Select(x => (IValidationInfo)x)
                        .ToList();
                    break;

                case ScanType.ScriptableObject: 
                    this.validationInfos = AssetDatabase.FindAssets("t:ScriptableObject")
                        .Select(x => AssetDatabase.GUIDToAssetPath(x))
                        .Select(x => AssetDatabase.LoadAssetAtPath<ScriptableObject>(x))
                        .Where(x => x == null || drawingConfig.GetEditorType(x.GetType()) == odinEditorType)
                        .OrderBy(x => x.name)
                        .Select(x => x == null ? (PropertyTree) null : PropertyTree.Create(new SerializedObject(x)))
                        .Examine(x => { if(x != null) x.UpdateTree(); })
                        .Where(x => x == null || x.RootPropertyCount != 0)
                        .Select((x, i) => new ScriptObjValidationInfo(x, this))
                        .Select(x => (IValidationInfo)x)
                        .ToList();
                    break;

                case ScanType.Prefab:
                    this.validationInfos = AssetDatabase.FindAssets("t:GameObject")
                        .Select(x => AssetDatabase.GUIDToAssetPath(x))
                        .Select(x => AssetDatabase.LoadAssetAtPath<GameObject>(x))
                        // .Where(x => (x.gameObject.scene.IsValid() && (x.gameObject.hideFlags & HideFlags.HideInHierarchy) == 0))
                        .Select(x => new { go = x.gameObject, components = x.GetComponents(typeof(Behaviour)) })
                        .SelectMany(x => x.components.Select(c => new { go = x.go, component = c }))
                        .Where(x => x.component == null || drawingConfig.GetEditorType(x.component.GetType()) == odinEditorType)
                        .OrderBy(x => x.go.name)
                        .ThenBy(x => x.component == null ? "" : x.component.name)
                        .Select(x => new { tree = x.component == null ? (PropertyTree)null : PropertyTree.Create(new SerializedObject(x.component)), go = x.go })
                        .Examine(x => { if (x.tree != null) x.tree.UpdateTree(); })
                        .Where(x => x.tree == null || x.tree.RootPropertyCount != 0)
                        .Select((x, i) => new BehaviourValidationInfo(x.tree, this, x.go))
                        .Select(x => (IValidationInfo)x)
                        .ToList();
                    break;
            }

            if (prevSelected != null) {
                var prev = this.validationInfos.FirstOrDefault(x => x.Behaviour == prevSelected || x.Obj == prevSelected);
                if (prev != null) {
                    prev.Select();
                }
            }

            this.triggerScan = true;
        }

        void OnDisable() { this.validationInfos = null; }

        interface IValidationInfo {
            bool IsSelected { get; }
            void Select();
            void DrawMenu();
            void DrawPropertyTree(); 
            
            Object Obj { get; } 
            Object Behaviour { get; }
            string Name { get; } 
            int ErrorCount { get; } 
            int WarningCount { get; } 

        }

        class ScriptObjValidationInfo : IValidationInfo {
            readonly PropertyTree propertyTree;
            readonly OdinProjectValidator window;

            public readonly ScriptableObject ScriptableObj;
            public Object Obj { get{ return ScriptableObj; }}
            public Object Behaviour { get{ return null; }}
            public string Name { get; private set; }
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }

            public ScriptObjValidationInfo(PropertyTree tree, OdinProjectValidator window) {
                this.ScriptableObj = tree != null ? (ScriptableObject)tree.WeakTargets[0] : null;
                this.propertyTree = tree;
                this.window = window;

                this.Name = "           " + (this.ScriptableObj != null ? this.ScriptableObj.name + " - " + this.ScriptableObj.GetType().GetNiceName() : "");
            }

            public bool IsSelected {
                get { return this.window.selectedValidationInfo == this; }
            }

            public void Select() {
                this.window.validationInfoToSelect = this;
            }

            bool IsIncluded {
                get {
                    return
                        this.window.includeErrors && this.ErrorCount > 0 ||
                        this.window.includeWarnings && this.WarningCount > 0 ||
                        this.window.includeValid && (this.WarningCount + this.ErrorCount) == 0;
                }
            }

            public void DrawMenu() {
                if (this.ScriptableObj == null) return;
                if (!this.IsIncluded) { return; }

                GUIHelper.PushGUIEnabled(GUI.enabled && this.ErrorCount + this.WarningCount > 0);

                var rect = SirenixEditorGUI.BeginListItem(); {
                    if (Event.current.rawType == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
                        if (this.IsSelected || this.ScriptableObj == null) {
                            EditorGUIUtility.PingObject(this.ScriptableObj);
                        }

                        this.Select();
                        GUIHelper.RequestRepaint();
                    }

                    if (this.IsSelected) {
                        GUIHelper.PushGUIEnabled(true);
                        SirenixEditorGUI.DrawSolidRect(rect, SirenixGUIStyles.MenuButtonActiveBgColor);
                        GUIHelper.PushLabelColor(Color.white);
                        EditorGUILayout.LabelField(this.Name);
                        GUIHelper.PopLabelColor();
                        GUIHelper.PopGUIEnabled();
                    }
                    else {
                        EditorGUILayout.LabelField(this.Name);
                    }
                    rect = new Rect(rect.position, new Vector2(20, 20));
                    rect.x += 6;

                    const float offAlpha = 0.1f;
                    var tmpColor = GUI.color;
                    GUI.color = this.WarningCount > 0 ? Color.white : new Color(1, 1, 1, offAlpha);
                    //GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    GUI.DrawTexture(rect, EditorIcons.UnityWarningIcon);

                    rect.x += 20;
                    GUI.color = this.ErrorCount > 0 ? Color.white : new Color(1, 1, 1, offAlpha);
                    GUI.DrawTexture(rect, EditorIcons.UnityErrorIcon);

                    if (this.IsIncluded && this.ErrorCount == 0 && this.WarningCount == 0) {
                        rect.x -= 10;
                        GUI.color = (this.ErrorCount + this.WarningCount) == 0 ? Color.green : new Color(0, 1, 0, offAlpha);
                        GUI.DrawTexture(rect, EditorIcons.Checkmark.Highlighted);
                    }
                    //GL.sRGBWrite = false;

                    GUI.color = tmpColor;
                }
                SirenixEditorGUI.EndListItem();

                GUIHelper.PopGUIEnabled();
            }

            public void DrawPropertyTree() {
                if (this.ScriptableObj == null) return;
                if (this.window.isScanning && Event.current.type == EventType.Repaint) {
                    OdinInspectorValidationChecker.BeginValidationCheck();
                }

                GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(10, 10, 6, 10) }); {
                    if (this.propertyTree != null) {
                        if (this.window.isScanning) {
                            InspectorUtilities.BeginDrawPropertyTree(this.propertyTree, true);
                            foreach (var property in this.propertyTree.EnumerateTree(true)) {
                                try {
                                    InspectorUtilities.DrawProperty(property, new GUIContent(""));
                                }
                                catch (System.Exception ex) {
                                    if (ex is ExitGUIException || ex.InnerException is ExitGUIException) {
                                        throw ex;
                                    }
                                    Debug.Log("The following exception was thrown when drawing property " + property.Path + ".");
                                    Debug.LogException(ex);
                                }
                            }

                            InspectorUtilities.EndDrawPropertyTree(this.propertyTree);
                        }
                        else {
                            this.propertyTree.Draw();
                        }
                    }
                    else {
                        SirenixEditorGUI.ErrorMessageBox("Missing Reference.");
                    }
                }
                GUILayout.EndVertical();

                if (this.window.isScanning && Event.current.type == EventType.Repaint)
                {
                    // We can't count the correct the correct number of warnings and errors for each behavior
                    // until we have a proper way of drawing a property tree with the guarantee that every property will be drawn.
                    this.WarningCount = OdinInspectorValidationChecker.WarningMessages.Count();
                    this.ErrorCount = OdinInspectorValidationChecker.ErrorMessages.Count();

                    OdinInspectorValidationChecker.EndValidationCheck();
                }
            }
        }

        class BehaviourValidationInfo : IValidationInfo {
            readonly PropertyTree propertyTree;
            readonly OdinProjectValidator window;

            public readonly GameObject GameObject;
            public readonly Behaviour Behav;
            public Object Obj { get{ return GameObject; }}
            public Object Behaviour { get{ return Behav; }}
            public string Name { get; private set; }
            public int ErrorCount { get; private set; }
            public int WarningCount { get; private set; }

            public BehaviourValidationInfo(PropertyTree tree, OdinProjectValidator window, GameObject go) {
                this.Behav = (tree != null) ? (Behaviour)tree.WeakTargets[0] : null;

                this.propertyTree = tree;
                this.window = window;
                this.GameObject = go;

                this.Name = "           " + this.GameObject.name + (this.Behav != null ? " - " + this.Behav.GetType().GetNiceName() : "");
            }

            public bool IsSelected {
                get { return this.window.selectedValidationInfo == this; }
            }

            public void Select() {
                this.window.validationInfoToSelect = this;
            }

            bool IsIncluded {
                get {
                    return
                        this.window.includeErrors && this.ErrorCount > 0 ||
                        this.window.includeWarnings && this.WarningCount > 0 ||
                        this.window.includeValid && (this.WarningCount + this.ErrorCount) == 0;
                }
            }

            public void DrawMenu() {
                if (this.GameObject == null) { return; }
                if (!this.IsIncluded) { return; }

                GUIHelper.PushGUIEnabled(GUI.enabled && this.ErrorCount + this.WarningCount > 0);

                var rect = SirenixEditorGUI.BeginListItem(true); {
                    if (Event.current.rawType == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
                        if (this.IsSelected || this.Behav == null) {
                            EditorGUIUtility.PingObject(this.GameObject);
                        }

                        this.Select();
                        GUIHelper.RequestRepaint();
                    }

                    if (this.IsSelected) {
                        GUIHelper.PushGUIEnabled(true);
                        SirenixEditorGUI.DrawSolidRect(rect, SirenixGUIStyles.MenuButtonActiveBgColor);
                        GUIHelper.PushLabelColor(Color.white);
                        EditorGUILayout.LabelField(this.Name);
                        GUIHelper.PopLabelColor();
                        GUIHelper.PopGUIEnabled();
                    }
                    else {
                        EditorGUILayout.LabelField(this.Name);
                    }
                    rect = new Rect(rect.position, new Vector2(20, 20));
                    rect.x += 6;

                    const float offAlpha = 0.1f;
                    var tmpColor = GUI.color;
                    GUI.color = this.WarningCount > 0 ? Color.white : new Color(1, 1, 1, offAlpha);
                    //GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                    GUI.DrawTexture(rect, EditorIcons.UnityWarningIcon);

                    rect.x += 20;
                    GUI.color = this.ErrorCount > 0 ? Color.white : new Color(1, 1, 1, offAlpha);
                    GUI.DrawTexture(rect, EditorIcons.UnityErrorIcon);

                    if (this.IsIncluded && this.ErrorCount == 0 && this.WarningCount == 0)
                    {
                        rect.x -= 10;
                        GUI.color = (this.ErrorCount + this.WarningCount) == 0 ? Color.green : new Color(0, 1, 0, offAlpha);
                        GUI.DrawTexture(rect, EditorIcons.Checkmark.Highlighted);
                    }
                    //GL.sRGBWrite = false;

                    GUI.color = tmpColor;
                }
                SirenixEditorGUI.EndListItem();

                GUIHelper.PopGUIEnabled();
            }

            public void DrawPropertyTree() {
                if (this.GameObject == null) return;

                if (this.window.isScanning && Event.current.type == EventType.Repaint) {
                    OdinInspectorValidationChecker.BeginValidationCheck();
                }

                GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(10, 10, 6, 10) }); {
                    if (this.propertyTree != null) {
                        if (this.window.isScanning) {
                            InspectorUtilities.BeginDrawPropertyTree(this.propertyTree, true);
                            foreach (var property in this.propertyTree.EnumerateTree(true)) {
                                try {
                                    InspectorUtilities.DrawProperty(property, new GUIContent(""));
                                }
                                catch (System.Exception ex) {
                                    if (ex is ExitGUIException || ex.InnerException is ExitGUIException) {
                                        throw ex;
                                    }
                                    else
                                    {
                                        Debug.Log("The following exception was thrown when drawing property " + property.Path + ".");
                                        Debug.LogException(ex);
                                    }
                                }
                            }

                            InspectorUtilities.EndDrawPropertyTree(this.propertyTree);
                        }
                        else
                        {
                            this.propertyTree.Draw(true);
                        }
                    }
                    else
                    {
                        SirenixEditorGUI.ErrorMessageBox("Missing Reference.");
                    }
                }
                GUILayout.EndVertical();

                if (this.window.isScanning && Event.current.type == EventType.Repaint) {
                    // We can't count the correct the correct number of warnings and errors for each behavior
                    // until we have a proper way of drawing a property tree with the guarantee that every property will be drawn.
                    this.WarningCount = OdinInspectorValidationChecker.WarningMessages.Count();
                    this.ErrorCount = OdinInspectorValidationChecker.ErrorMessages.Count();

                    OdinInspectorValidationChecker.EndValidationCheck();
                }
            }
        }
    }
}
#endif