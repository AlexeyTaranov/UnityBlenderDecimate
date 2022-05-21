using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Editor.UnityBlenderDecimate
{
    public class UnityBlenderDecimate : EditorWindow
    {
        [MenuItem("Tools/Unity To Blender")]
        public static void ShowExample()
        {
            UnityBlenderDecimate window = GetWindow<UnityBlenderDecimate>();
            window.titleContent = new GUIContent("Unity Blender Decimate");
        }

        [SerializeField] private VisualTreeAsset visualTreeAsset;
        [SerializeField] private string blenderPath;
        private Button BlenderPathSelectButton => rootVisualElement.Q<Button>("blenderPathSelect");
        private Label BlenderPathLabel => rootVisualElement.Q<Label>("blenderPath");
        private ObjectField PythonScriptField => rootVisualElement.Q<ObjectField>("pythonScript");
        private ObjectField InputObject => rootVisualElement.Q<ObjectField>("inputObject");

        private GameObject selectedGameObject;
        private string pythonScriptPath;

        public void CreateGUI()
        {
            rootVisualElement.Add(visualTreeAsset.Instantiate());
#if UNITY_EDITOR_OSX
            SetupOSXBlenderPath();
#else
            BlenderPathSelectButton.RegisterCallback<ClickEvent>(OnBlenderSelectPathClick);
#endif

            PythonScriptField.RegisterValueChangedCallback(OnPythonScriptSelect);
            InputObject.RegisterValueChangedCallback(CheckSelectedInputObject);
            rootVisualElement.Q<Button>("execute").RegisterCallback<ClickEvent>(Execute);
            
            UpdateNextStep();
        }

        private void SetupOSXBlenderPath()
        {
            BlenderPathSelectButton.SetEnabled(false);
            BlenderPathLabel.text = IsAvailableBlender() ? blenderPath : "Can't find Blender in Applications folder =(";

            bool IsAvailableBlender()
            {
                blenderPath = Path.Combine("/Applications", "Blender.app", "Contents", "MacOS", "Blender");
                return File.Exists(blenderPath);
            }
        }

        private void UpdateNextStep()
        {
            Func<(bool isValidStep, string stepName)>[] steps =
            {
                () => (File.Exists(blenderPath), "blenderPathStep"),
                () => (string.IsNullOrEmpty(pythonScriptPath) == false, "pythonScript"),
                () => (selectedGameObject != null, "inputObjectStep"),
                () => (true, "blenderExecuteStep")
            };

            var failedStep = Array.FindIndex(steps, func => func.Invoke().isValidStep == false);
            for (var i = 0; i < steps.Length; i++)
            {
                var stepName = steps[i].Invoke().stepName;
                var element = rootVisualElement.Q<VisualElement>(stepName);
                element?.SetEnabled(failedStep >= i || failedStep == -1);
            }
        }

        private void OnBlenderSelectPathClick(ClickEvent @event)
        {
            var path = string.IsNullOrEmpty(blenderPath) ? Application.dataPath : blenderPath;
            var executableExtension = "exe";
            blenderPath = EditorUtility.OpenFilePanel("Select Blender executable", path, executableExtension);
            BlenderPathLabel.text = blenderPath;
            
            UpdateNextStep();
        }

        private void OnPythonScriptSelect(ChangeEvent<Object> @event)
        {
            var path = AssetDatabase.GetAssetPath(@event.newValue);
            pythonScriptPath = Path.GetExtension(path) == ".py" ? Path.GetFullPath(path) : null;
            
            UpdateNextStep();
        }

        private void CheckSelectedInputObject(ChangeEvent<Object> @event)
        {
            PrefabAssetType prefabType = PrefabAssetType.NotAPrefab;
            var go = @event.newValue as GameObject;
            if (go != null)
            {
                var prefabAssetType = PrefabUtility.GetPrefabAssetType(go);
                var fileExtension = Path.GetExtension(AssetDatabase.GetAssetPath(go));
                var isObjModel = prefabAssetType == PrefabAssetType.Model && fileExtension == ".obj";
                prefabType = isObjModel ? PrefabAssetType.Model : PrefabAssetType.NotAPrefab;
            }
            selectedGameObject = prefabType == PrefabAssetType.Model ? go : null;
            InputObject.value = selectedGameObject;

            UpdateNextStep();
        }

        private void Execute(ClickEvent @event)
        {
            var decimateValue = rootVisualElement.Q<Slider>("decimate").value;
            var absolutePathToObj = Path.GetFullPath(AssetDatabase.GetAssetPath(selectedGameObject));
            absolutePathToObj = '"' + absolutePathToObj + '"';
            var blenderProcess = new Process();
            blenderProcess.StartInfo.FileName = blenderPath;
            var bgBlenderArgs = $"--background --python {pythonScriptPath} -- {absolutePathToObj} {decimateValue}";
            blenderProcess.StartInfo.Arguments = bgBlenderArgs;
            blenderProcess.Start();
            //TODO: Don't lock Unity
            blenderProcess.WaitForExit();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }
}