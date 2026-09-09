using DevLib.HashDataSystem;
using UnityEngine;

namespace DevLib.FsmSystem.Runtime
{
    [CreateAssetMenu(fileName = "State data", menuName = "Lib/FSM/State data", order = 0)]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string className;
        public int assetIndex;
        public HashDataSO animHash;
    }
}