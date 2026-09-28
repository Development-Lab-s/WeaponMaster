using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WorkFolder.SUH.Scripts.CharacterVisual;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BrushFaceDrawButton : MonoBehaviour
    {
        [SerializeField] private CharacterEditorUI characterEditor;
        [SerializeField] private GameObject targetObject;
        private BoxCollider hitBox;
        private readonly List<RaycastResult> uiHits = new();

        private void Awake()
        {
            hitBox = GetComponent<BoxCollider>();
            FitCollider();
        }

        public void FitCollider()
        {
            var box = GetComponent<BoxCollider>();
            bool found = false;
            var bounds = new Bounds();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var local = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var offset = Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    var point = transform.InverseTransformPoint(renderer.transform.TransformPoint(local.center + offset));
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!found) return;
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size * 1.15f, Vector3.one * 0.05f);
            box.isTrigger = true;
        }

        private void Update()
        {
            if (LobbyFaceDrawFlow.IsOpen || characterEditor == null ||
                Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || !targetObject.activeSelf) return;
            var camera = Camera.main;
            if (camera == null) return;
            var point = Mouse.current.position.ReadValue();
            // Ignore actual UI controls, while allowing clicks through decorative fullscreen images.
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
                foreach (var hit in uiHits)
                    if (hit.gameObject.GetComponentInParent<Selectable>() != null) return;
            }
            if (Physics.Raycast(camera.ScreenPointToRay(point), out var target, camera.farClipPlane,
                    camera.cullingMask, QueryTriggerInteraction.Collide) && target.collider == hitBox)
                characterEditor.OpenFaceDraw();
        }
    }
}
