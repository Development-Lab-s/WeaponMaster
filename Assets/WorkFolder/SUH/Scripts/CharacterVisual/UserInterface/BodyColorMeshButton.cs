using System;
using UnityEngine;
using UnityEngine.Events;
using WorkFolder.SUH.Scripts.CharacterVisual.UserInterface.ServiceLocator;

namespace WorkFolder.SUH.Scripts.CharacterVisual.UserInterface
{
    public class BodyColorMeshButton : MonoBehaviour
    {
        [SerializeField] private uint currentIndex;
        
        private ICharacterEditor service;
        public UnityEvent OnClickEvent;
        private void Start()
        {
            service = DevLib.ServiceLocator.ServiceLocator.Get<ICharacterEditor>();
        }

        private void OnMouseDown()
        {
            service?.BodyColorIndexChange(currentIndex);
            OnClickEvent?.Invoke();
        }
    }
}