using DevLib.ModuleSystem;
using DevLib.ServiceLocator;
using UnityEngine;
using UnityEngine.Events;
using WorkFolder.SUH.Scripts.DataStoreService.Lib;
using WorkFolder.SUH.Scripts.DataStoreService.Lib.Interface.ServiceLocator;
using WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.ServiceLoactor;

namespace WorkFolder.SUH.Scripts.UserInterface.CharacterEditor
{
    public class CharacterEditorUserInterface : MonoBehaviour, IAfterInitModule
    {
        public UnityEvent OnBodyColorFire;
        public UnityEvent OnFaceDrawFire;
        public UnityEvent OnSaveEnter;

        public UnityEvent<uint> OnBodyColorChanged;
        
        #region Unity Button Event Method
        
        public void Button_Save_Click()
        {
            OnSaveEnter?.Invoke();
        }

        public void Button_FaceDrawEnter_Click()
        {
            OnFaceDrawFire?.Invoke();
        }

        public void Button_BodyColorEnter_Click()
        {
            OnBodyColorFire?.Invoke();
        }
        
        public async void Button_BodyColor_Click(uint index)
        {
            IDataStoreService dataStoreService = ServiceLocator.Get<IDataStoreService>();
            if (dataStoreService == null)
                return;

            await dataStoreService.SetAsync(
                nameof(PlayerDataStore.BodyColor),
                (uint)index);
            
            OnBodyColorChanged?.Invoke(index);
        }

        #endregion
        
        #region UNITY_LIFECLCYLE

        private void Start()
        {
            AfterInit();
        }
        public void AfterInit()
        {
            if (ServiceLocator.Get<IBodyColorSender>() != null)
                return;
            
            ServiceLocator.Register(this);
        }
        #endregion
    }
}
