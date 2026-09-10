using UnityEngine;

namespace WorkFolder.SUH.Scripts.UserInterface.CharacterEditor.ScriptableObject
{
    [CreateAssetMenu(fileName = "hair style data", menuName = "SO/Suxghui/CharacterEditor/Hair Style SO", order = 0)]
    public class HairStyleSO : UnityEngine.ScriptableObject
    {
        public uint hairStyleId;
        public GameObject hairObject;
    }
}