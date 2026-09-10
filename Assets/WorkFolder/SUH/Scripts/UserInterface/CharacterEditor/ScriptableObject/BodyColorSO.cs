using UnityEngine;

namespace WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.ScriptableObject
{
    [CreateAssetMenu(fileName = "bodycolor data", menuName = "SO/Suxghui/CharacterEditor/BodyColor SO", order = 0)]
    public class BodyColorSO : UnityEngine.ScriptableObject
    {
        public string BodyColorHex;
        public int Index;
    }
}