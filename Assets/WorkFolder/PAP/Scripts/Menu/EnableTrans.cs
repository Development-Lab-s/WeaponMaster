using System;
using UnityEngine;
using UnityEngine.Events;

namespace WorkFolder.PAP.Scripts.Menu
{
    public class EnableTrans : MonoBehaviour
    {
        [SerializeField] private GameObject targetUI;
        private Action end;
        public UnityEvent OnMove;

        private void Awake()
        {
            end += HandleObjectEnable;
        }

        private void HandleObjectEnable()
        {
            targetUI.SetActive(true);
            OnMove?.Invoke();
            Trans.Instance.DePlay();
        }

        public void PlayTrans()
        {
            Trans.Instance.Play(end);
        }
        
        
    }
}