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
            ColorPickerFeedback.CreateFeedback();
        }

        void Start()
        {
 
        }
        
        #region UNITY_LifeCycle

        private void Awake()
        {
            
        }
        
        #endregion
        
        public void Exit()
        {
            ColorPickerFeedback.CreateFeedback();
        }
    }
}