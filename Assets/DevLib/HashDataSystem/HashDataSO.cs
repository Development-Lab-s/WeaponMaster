using System;
using UnityEngine;

namespace DevLib.HashDataSystem
{
    [CreateAssetMenu(fileName = "Hash data", menuName = "Lib/Hash data", order = 10)]
    public class HashDataSO : ScriptableObject
    {
        [field: SerializeField] public string HashName { get; private set; }
        [field: SerializeField] public int HashValue { get; private set; }
        
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(HashName))
            {
                HashValue = 0;
                return;
            }
            
            HashValue = Animator.StringToHash(HashName);
        }
    }
}