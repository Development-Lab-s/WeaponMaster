namespace WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface
{
    public interface IMapService
    {
        int CurrentMapBuildIndex { get; }
        bool IsLoading { get; }
        bool LoadNextMap();
    }
}
