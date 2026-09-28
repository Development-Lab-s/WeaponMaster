using DevLib.ServiceLocator;
using Fusion;
using UnityEngine;
using WorkFolder.PAP.Scripts.Network;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public class MapService : MonoBehaviour, IMapService
    {
        [SerializeField] private int[] mapBuildIndices;

        private int currentMapIndex;
        public bool IsLoading { get; private set; }

        public int CurrentMapBuildIndex =>
            mapBuildIndices != null && mapBuildIndices.Length > 0
                ? mapBuildIndices[currentMapIndex]
                : -1;

        private void Awake()
        {
            ServiceLocator.Register<IMapService>(this);
        }

        private void OnEnable()
        {
            RunnerProvider.Ready += OnSceneLoadDone;
        }

        private void OnDestroy()
        {
            RunnerProvider.Ready -= OnSceneLoadDone;
            ServiceLocator.UnRegister<IMapService>(this);
        }

        private void OnSceneLoadDone(NetworkRunner runner)
        {
            IsLoading = false;
        }

        public bool LoadNextMap()
        {
            if (IsLoading || mapBuildIndices == null || mapBuildIndices.Length == 0)
                return false;

            NetworkRunner runner = RunnerProvider.Current;
            NetworkSceneManagerDefault sceneManager =
                runner != null ? runner.GetComponent<NetworkSceneManagerDefault>() : null;

            if (sceneManager == null)
            {
                Debug.LogWarning("[MapService] NetworkSceneManagerDefault를 찾을 수 없습니다.");
                return false;
            }

            currentMapIndex = (currentMapIndex + 1) % mapBuildIndices.Length;
            IsLoading = true;
            sceneManager.LoadScene(
                SceneRef.FromIndex(mapBuildIndices[currentMapIndex]),
                default);
            return true;
        }
    }
}
