using System;
using DevLib.ServiceLocator;
using UnityEngine;

namespace WorkFolder.PAP.Scripts.MainSystems
{
    public interface ICameraService
    {
        void SwitchLobbyCam();
        void SwitchMainCam();
        public UnityEngine.Camera CurrentCamera { get; }
    }

    public class CameraService : MonoBehaviour, ICameraService
    {
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private UnityEngine.Camera lobbyCamera;
        private AudioListener _mainListener;
        private AudioListener _lobbyListener;
        public UnityEngine.Camera CurrentCamera { get; private set; }
        
        

        private void Awake()
        {
            _mainListener = mainCamera.GetComponent<AudioListener>();
            _lobbyListener = lobbyCamera.GetComponent<AudioListener>();
            ServiceLocator.Register<ICameraService>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegister(this);
        }

        public void SwitchLobbyCam()
        {
            lobbyCamera.enabled = true;
            mainCamera.enabled = false;
            _mainListener.enabled = false;
            _lobbyListener.enabled = true;
            CurrentCamera = lobbyCamera;
        }
        
        public void SwitchMainCam()
        {
            lobbyCamera.enabled = false;
            mainCamera.enabled = true;
            _mainListener.enabled = true;
            _lobbyListener.enabled = false;
            CurrentCamera = mainCamera;
        }

    }
}