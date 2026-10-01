using System;
using UnityEngine;
using UnityEngine.Events;

namespace WorkFolder.PAP.Scripts
{
    public class EnablePlaySound : MonoBehaviour
    {
        public UnityEvent OnActiveEvent;

        private void OnEnable()
        {
            OnActiveEvent?.Invoke();
        }
    }
}