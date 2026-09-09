using DevLib.ServiceLocator;
using Unity.VisualScripting;
using UnityEngine;
using WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.Interface;
using WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.ServiceLoactor;
using WorkFolder.SUH.Scripts.WeaponVisualHolder.Lib.Feedback;

namespace WorkFolder.SUH.Scripts.UserInterface.CharacterEditor
{
    public class CharacterEditor : MonoBehaviour, IEnter, IExit
    {
        [SerializeField] private ColorPickerFeedback ColorPickerFeedback;

        public void Enter()
        {
            
        }

        #region Event

        public void CharacterEditorExit()
        {
            // 씬 이동 또는 종료 메소드
        }
        
        #endregion
        
        public void Exit()
        {
            
        }
    }
}