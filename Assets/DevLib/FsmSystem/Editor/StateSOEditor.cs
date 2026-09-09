using System;
using System.Collections.Generic;
using System.Linq;
using DevLib.FsmSystem.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevLib.FsmSystem.Editor
{
    [UnityEditor.CustomEditor(typeof(StateSO))]
    
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorView;

        private StateSO _targetData;
        
        public override VisualElement CreateInspectorGUI()
        {
            _targetData = (StateSO) target;
            VisualElement root = new VisualElement();
            
            editorView.CloneTree(root);

            FillDropdownField(root);
            
            return root;
        }

        private void FillDropdownField(VisualElement root)
        {
            DropdownField field = root.Q<DropdownField>("state-class");
            
            //Linq 컬렉션에다가 쓸 수 있는 SQL이라고 생각해
            // SELECT * FROM 테이블명 WHERE 조건
            IEnumerable<string> choices = TypeCache.GetTypesDerivedFrom<AbstractState>()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Select(type => $"{type.FullName}, {type.Assembly.GetName().Name}" );
            
            field.choices.AddRange(choices);

            if (_targetData != null &&
                !string.IsNullOrEmpty(_targetData.className) &&
                field.choices.Contains(_targetData.className))
            {
                field.value = _targetData.className; //내가 선택한걸로 돌려라.
            }else if (_targetData != null && field.choices.Count > 0)
            {
                _targetData.className = field.choices.First();
                EditorUtility.SetDirty(_targetData);
            }
            
            //만약 더티 플래그가 활성화되어있다면 저장 새로해라.
            AssetDatabase.SaveAssetIfDirty(_targetData);
        }
    }
}