using UnityEngine;

namespace WorkFolder.SUH.Scripts.CharacterVisual
{
    public static class AccessoryVisuals
    {
        public static GameObject Find(Transform root, string objectName)
        {
            if (root == null) return null;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child.gameObject;
            return null;
        }

        public static void Show(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible) target.SetActive(visible);
        }
    }
}
