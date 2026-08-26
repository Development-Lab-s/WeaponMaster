using System.IO;
using System.Linq;
using DevLib.FsmSystem.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevLib.FsmSystem.Editor
{
    [CustomEditor(typeof(StateListSO))]
    public class StateListSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView;

        private Button _folderBtn;
        private Button _generateBtn;
        private Label _generatePathLabel;

        private string _generatePath;
        private StateListSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            
            editorView.CloneTree(root);

            _folderBtn = root.Q<Button>("folder-btn");
            _generateBtn = root.Q<Button>("generate-btn");
            _generatePathLabel = root.Q<Label>("selected-label");
            _generatePathLabel.text = "No Path Selected";
            
            _targetData = target as StateListSO;
            if (_targetData != null && !string.IsNullOrEmpty(_targetData.generatePath))
            {
                _generatePathLabel.text = FileUtil.GetProjectRelativePath(_targetData.generatePath);  //여기서 문제가 좀 있을꺼다.
                _generatePath = _targetData.generatePath;
            }

            _folderBtn.clicked += HandleFolderBtnClick;
            _generateBtn.clicked += HandleCodeGenerateClick;
                
            return root;
        }

        private void HandleCodeGenerateClick()
        {
            if (string.IsNullOrEmpty(_generatePath) || !Directory.Exists(_generatePath))
            {
                EditorUtility.DisplayDialog("Error", "경로 설정이 올바르지 않습니다.", "OK");
                return;
            }
            
            int index = 0;
            string enumString = string.Join(", ", _targetData.states.Select(so =>
            {
                so.assetIndex = index;
                EditorUtility.SetDirty(so);
                return $"{so.stateName} = {index++}";
            }));

            //Substring은 지정된 갯수만큼 앞에서 컷
            string ns = FileUtil.GetProjectRelativePath(_generatePath).Substring("Assets/".Length);
            if (ns.StartsWith("Scripts/"))
            {
                ns = ns.Substring("Scripts/".Length);
            }
            ns = ns.Replace("/", "."); //이름공간은 슬래시가 아니라 .으로 구분
            
            string code = string.Format(CodeFormat.EnumFormat, ns, _targetData.enumName, enumString);
            
            File.WriteAllText($"{_generatePath}/{_targetData.enumName}.cs", code);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); //컴파일 새로
        }

        private void HandleFolderBtnClick()
        {
            _generatePath = EditorUtility.OpenFolderPanel("스크립트를 만들 폴더를 선택하세요", _generatePath, "");

            if (!string.IsNullOrEmpty(_generatePath))
            {
                _targetData.generatePath = _generatePath;
                _generatePathLabel.text = FileUtil.GetProjectRelativePath( _generatePath);
                EditorUtility.SetDirty(_targetData);
                AssetDatabase.SaveAssets();
            }
        }
    }
}