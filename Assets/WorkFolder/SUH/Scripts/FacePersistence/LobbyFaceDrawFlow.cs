using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // Local panel inside Lobby. No scene loading, unloading or active-scene changes.
    public static class LobbyFaceDrawFlow
    {
        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsOpen = false;

        public static async Task<bool> OpenAsync(Scene lobby, GameObject panel, Func<Task> beforeReturn = null)
        {
            if (IsOpen) throw new InvalidOperationException("FaceDraw is already open.");
            if (panel == null || panel.scene != lobby) throw new InvalidOperationException("Assign a face drawing panel inside the Lobby scene.");
            IsOpen = true;
            var restore = new List<Action>();
            FaceSaveController controller = null;
            var cursorLock = Cursor.lockState;
            var cursorVisible = Cursor.visible;
            CanvasGroup fade = null;
            try
            {
                fade = CreateFade(lobby);
                await FadeAsync(fade, 1f);
                if (panel == null) return false;
                // Hide only presentation. Keep Lobby objects, services and EventSystem running.
                foreach (var root in lobby.GetRootGameObjects())
                {
                    if (root == panel || root == fade.gameObject) continue;
                    foreach (var component in root.GetComponentsInChildren<Behaviour>(true))
                    {
                        if (component.transform.IsChildOf(panel.transform)) continue;
                        if (!component.enabled || !(component is Camera || component is Canvas ||
                            component is AudioListener || component is CinemachineVirtualCameraBase ||
                            component is CinemachineBrain)) continue;
                        component.enabled = false;
                        var target = component;
                        restore.Add(() => { if (target != null) target.enabled = true; });
                    }
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Existing inactive Lobby object: no Instantiate/Destroy or scene transitions.
                controller = panel.GetComponentInChildren<FaceSaveController>(true);
                if (controller == null) throw new InvalidOperationException("FaceSaveController missing in Lobby panel.");
                var completion = new TaskCompletionSource<bool>();
                controller.FlowFinished = saved => completion.TrySetResult(saved);
                controller.PrepareDirectDrawing();
                panel.SetActive(true);
                await FadeAsync(fade, 0f);
                bool savedSuccessfully = await completion.Task;
                // Finish FaceDraw.Confirm before hiding the panel or firing Lobby return events.
                await Task.Yield();
                if (fade == null) return false;
                await FadeAsync(fade, 1f);
                if (controller != null) controller.FlowFinished = null;
                if (panel != null) panel.SetActive(false);
                foreach (var action in restore) action();
                restore.Clear();
                Cursor.lockState = cursorLock;
                Cursor.visible = cursorVisible;
                if (beforeReturn != null) await beforeReturn();
                await FadeAsync(fade, 0f);
                return savedSuccessfully;
            }
            finally
            {
                if (panel != null)
                {
                    if (controller != null) controller.FlowFinished = null;
                    panel.SetActive(false);
                }
                foreach (var action in restore) action();
                Cursor.lockState = cursorLock;
                Cursor.visible = cursorVisible;
                if (fade != null)
                {
                    fade.DOKill();
                    UnityEngine.Object.Destroy(fade.gameObject);
                }
                IsOpen = false;
            }
        }

        private static CanvasGroup CreateFade(Scene scene)
        {
            var overlay = new GameObject("Face Draw Transition", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(overlay, scene);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
            shade.transform.SetParent(overlay.transform, false);
            var rect = (RectTransform)shade.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            shade.GetComponent<Image>().color = Color.black;
            var group = overlay.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            return group;
        }

        private static async Task FadeAsync(CanvasGroup fade, float alpha)
        {
            if (fade == null) return;
            fade.blocksRaycasts = true;
            await fade.DOFade(alpha, 0.25f).SetEase(Ease.InOutSine).SetUpdate(true)
                .SetLink(fade.gameObject).AsyncWaitForCompletion();
            if (fade != null) fade.blocksRaycasts = alpha > 0f;
        }
    }
}
