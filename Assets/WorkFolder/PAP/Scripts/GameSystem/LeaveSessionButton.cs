using System;
using System.Threading.Tasks;
using BK._01_Script;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using WorkFolder.PAP.Scripts.Network;

namespace WorkFolder.PAP.Scripts.GameSystem
{
    // 게임 중 "종료" 버튼. 공유 모드 세션을 끊고 로비 씬으로 돌아간다.
    // 로비의 NetworkRunner는 게임 씬까지 따라오기 때문에, 씬만 바꾸면 세션이 살아 있고
    // 로비에 돌아갔을 때 러너가 두 개가 된다. 그래서 반드시 러너를 Shutdown한 뒤 씬을 바꾼다.
    public sealed class LeaveSessionButton : MonoBehaviour, IButton
    {
        [SerializeField] private string lobbySceneName = "Lobby";

        // 디스폰 소식이 다른 클라이언트에 전송될 시간을 준 뒤 연결을 끊는다.
        [SerializeField, Min(0f)] private float despawnFlushSeconds = 0.25f;

        private bool _isLeaving;

        public void Click()
        {
            Leave();
        }

        public async void Leave()
        {
            if (_isLeaving)
                return;
            _isLeaving = true;

            try
            {
                NetworkRunner runner = FindRunningRunner();
                if (runner != null)
                {
                    // 공유 모드에서는 나간 사람의 오브젝트가 남을 수 있으니 내 것은 직접 치우고 나간다.
                    DespawnOwnedObjects(runner);
                    await Task.Delay(TimeSpan.FromSeconds(despawnFlushSeconds));

                    await LeaveVoiceChannel();

                    if (runner != null)
                        await runner.Shutdown(true, ShutdownReason.Ok);
                }
                else
                {
                    await LeaveVoiceChannel();
                }
            }
            catch (Exception exception)
            {
                // 연결 정리가 실패해도 로비로는 돌아가야 한다.
                Debug.LogException(exception);
            }

            ResetStaticGameState();
            SceneManager.LoadScene(lobbySceneName);
        }

        // StartUIEnable과 같은 기준: 내가 스폰한 것(캐릭터·무기)만. 씬 오브젝트와 중첩 오브젝트는 제외.
        private static void DespawnOwnedObjects(NetworkRunner runner)
        {
            foreach (NetworkObject netObj in runner.SimulationUnityScene.GetComponents<NetworkObject>(true))
            {
                if (netObj != null
                    && netObj.IsValid
                    && netObj.HasStateAuthority
                    && !netObj.NetworkTypeId.IsSceneObject
                    && !netObj.IsNested)
                {
                    runner.Despawn(netObj);
                }
            }
        }

        private static async Task LeaveVoiceChannel()
        {
            if (VivoxManager.Instance == null)
                return;

            try
            {
                await VivoxManager.Instance.LeaveCurrentChannelAsync();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LeaveSessionButton] 음성 채널 나가기 실패: {exception.Message}");
            }
        }

        // 씬이 바뀌어도 남는 static 값이 다음 게임에 새지 않게 되돌린다.
        // 골반 카메라(Esc) 메뉴에서 종료하면 이 값이 true로 남아 다음 게임에서 이동이 막힌다.
        private static void ResetStaticGameState()
        {
            PlayerHipCamera.IsCameraEnable = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static NetworkRunner FindRunningRunner()
        {
            NetworkRunner current = RunnerProvider.Current;
            if (current != null && current.IsRunning)
                return current;

            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                if (runner != null && runner.IsRunning)
                    return runner;
            }

            return null;
        }
    }
}
