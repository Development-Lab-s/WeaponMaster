using System.Collections.Generic;
using DG.Tweening;
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
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.2f;
        [SerializeField] private GameObject checkTarget;
        private BoxCollider hitBox;
        private readonly List<RaycastResult> uiHits = new();
        private readonly List<(Renderer renderer, int slot, int color, int emission)> outlines = new();
        private MaterialPropertyBlock colorBlock;
        private Tween hoverTween;
        private Sequence clickTween;
        private Vector3 restingScale;
        private Quaternion restingRotation;
        private bool clicking;
        private Color outlineColor = Color.black;
        private bool hovered;

        private void Awake()
        {
            hitBox = GetComponent<BoxCollider>();
            FitCollider();
            colorBlock = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (material == null || material.name.IndexOf("MatOutline", System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    // glTF uses baseColorFactor/emissiveFactor rather than Unity's _Color.
                    string color = material.HasProperty("baseColorFactor") ? "baseColorFactor" :
                        material.HasProperty("_Color") ? "_Color" : "_BaseColor";
                    if (!material.HasProperty(color)) continue;
                    int emission = material.HasProperty("emissiveFactor") ? Shader.PropertyToID("emissiveFactor") :
                        material.HasProperty("_EmissionColor") ? Shader.PropertyToID("_EmissionColor") : -1;
                    outlines.Add((renderer, slot, Shader.PropertyToID(color), emission));
                }
            }
            ApplyOutlineColor(Color.clear);
        }

        private void ApplyOutlineColor(Color color)
        {
            outlineColor = color;
            foreach (var outline in outlines)
            {
                if (outline.renderer == null) continue;
                colorBlock.Clear();
                outline.renderer.GetPropertyBlock(colorBlock, outline.slot);
                colorBlock.SetColor(outline.color, color * 3f);
                outline.renderer.SetPropertyBlock(colorBlock, outline.slot);
            }
        }

        private void SetHovered(bool value)
        {
            if (hovered == value) return;
            hovered = value;
            hoverTween?.Kill();
            hoverTween = DOTween.To(() => outlineColor, ApplyOutlineColor,
                    value ? Color.white : new Color(0,0,0,0), hoverDuration)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void OnDisable()
        {
            clickTween?.Kill();
            clickTween = null;
            if (clicking) transform.SetLocalPositionAndRotation(transform.localPosition, restingRotation);
            if (clicking) transform.localScale = restingScale;
            clicking = false;
            hoverTween?.Kill();
            hoverTween = null;
            hovered = false;
            ApplyOutlineColor(new Color(0,0,0,0));
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
            bool pointingAtBrush = IsPointingAtBrush();
            SetHovered(pointingAtBrush);
            if (pointingAtBrush && Mouse.current.leftButton.wasPressedThisFrame && checkTarget.activeSelf)
            {
                PlayClick();
            }
        }

        private void PlayClick()
        {
            clicking = true;
            restingScale = transform.localScale;
            restingRotation = transform.localRotation;
            SetHovered(false);
            clickTween = DOTween.Sequence().SetUpdate(true);
            clickTween.Append(transform.DOScale(restingScale * 0.88f, 0.09f).SetEase(Ease.OutCubic));
            clickTween.Join(transform.DOLocalRotateQuaternion(restingRotation * Quaternion.Euler(0f, 0f, -8f), 0.09f));
            clickTween.Append(transform.DOScale(restingScale, 0.15f).SetEase(Ease.OutCubic));
            clickTween.Join(transform.DOLocalRotateQuaternion(restingRotation, 0.15f).SetEase(Ease.OutCubic));
            clickTween.OnComplete(() =>
            {
                clicking = false;
                clickTween = null;
                if (characterEditor != null) characterEditor.OpenFaceDraw();
            });
        }

        private bool IsPointingAtBrush()
        {
            if (clicking || !Application.isFocused || LobbyFaceDrawFlow.IsOpen || characterEditor == null ||
                Mouse.current == null) return false;
            var camera = Camera.main;
            if (camera == null) return false;
            var point = Mouse.current.position.ReadValue();
            // Ignore actual UI controls, while allowing clicks through decorative fullscreen images.
            if (EventSystem.current != null)
            {
                uiHits.Clear();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, uiHits);
                foreach (var hit in uiHits)
                    if (hit.gameObject.GetComponentInParent<Selectable>() != null) return false;
            }
            return Physics.Raycast(camera.ScreenPointToRay(point), out var target, camera.farClipPlane,
                camera.cullingMask, QueryTriggerInteraction.Collide) && target.collider == hitBox;
        }
    }
}
