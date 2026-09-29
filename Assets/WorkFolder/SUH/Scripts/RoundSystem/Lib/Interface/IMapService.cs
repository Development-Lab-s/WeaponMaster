namespace WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface
{
    public interface IMapService
    {
        int CurrentMapIndex { get; }
        bool IsLoading { get; }
        bool HasMap { get; }
        int ChooseNextMap(int previous);
        bool ApplyMap(int index);
        void AdoptInitialMap(int index);
    }
}
