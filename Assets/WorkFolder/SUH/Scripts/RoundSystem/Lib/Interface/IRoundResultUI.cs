using System.Collections.Generic;

namespace WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface
{
    public interface IRoundResultUI
    {
        void ShowResults(IReadOnlyList<RoundResultEntry> results);
        void HideResults();
    }
}
