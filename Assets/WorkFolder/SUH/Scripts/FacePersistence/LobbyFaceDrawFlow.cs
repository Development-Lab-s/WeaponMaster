using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorkFolder.SUH.Scripts.FacePersistence
{
    // Local panel inside Lobby. No scene loading, unloading or active-scene changes.
    public static class LobbyFaceDrawFlow
    {
        public static bool IsOpen { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => IsOpen = false;

        public static async Task<bool> OpenAsync(Scene lobby, GameObject panel)
        {
            if (IsOpen) throw new InvalidOperationException("FaceDraw is already open.");
            if (panel == null || panel.scene != lobby) throw new InvalidOperationException("Assign a face drawing panel inside the Lobby scene.");
            IsOpen = true;
            var restore = new List<Action>();
            FaceSaveController controller = null;
            var cursorLock = Cursor.lockState;
            var cursorVisible = Cursor.visible;
            try
            {
                // Hide only presentation. Keep Lobby objects, services and EventSystem running.
                foreach (var root in lobby.GetRootGameObjects())
                {
                    if (root == panel) continue;
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
                panel.SetActive(true);
                bool savedSuccessfully = await completion.Task;
                // Finish FaceDraw.Confirm before hiding the panel or firing Lobby return events.
                await Task.Yield();
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
                IsOpen = false;
            }
        }
    }
}
