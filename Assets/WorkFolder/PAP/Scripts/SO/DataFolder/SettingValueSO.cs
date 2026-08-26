using UnityEngine;

namespace WorkFolder.PAP.Scripts.SO.DataFolder
{
    [CreateAssetMenu(fileName = "Setting Value data", menuName = "Setting/Value data", order = 0)]
    public class SettingValueSO : ScriptableObject
    {
        public float MouseSensitivityX;
        public float MouseSensitivityY;
    }
}