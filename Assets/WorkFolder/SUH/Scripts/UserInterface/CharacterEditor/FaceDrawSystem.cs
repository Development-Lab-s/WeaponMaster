using DevLib.ModuleSystem;
using UnityEngine;

namespace WorkFolder.SUH.Scripts.UserInterface.CharacterEditor
{
    public class FaceDrawSystem : MonoBehaviour, IAfterInitModule
    {
        private Camera mainCamera;

        public void AfterInit()
        {
            mainCamera = Camera.main;
        }

        private Vector3 ScreenToWorldPoint(Vector3 screenVector3)
        {
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(screenVector3);
            
            return worldPoint;
        }
    }
}