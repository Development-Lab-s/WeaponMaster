using System.Collections.Generic;
using DevLib.ServiceLocator;
using TMPro;
using UnityEngine;
using WorkFolder.SUH.Scripts.RoundSystem.Lib;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public class RoundResultUI : MonoBehaviour, IRoundResultUI
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text[] rankTexts;
        [SerializeField] private TMP_Text[] nameTexts;
        [SerializeField] private TMP_Text[] scoreTexts;

        private void Awake()
        {
            ServiceLocator.Register<IRoundResultUI>(this);
            HideResults();
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegister<IRoundResultUI>(this);
        }

        public void ShowResults(IReadOnlyList<RoundResultEntry> results)
        {
            if (root != null)
                root.SetActive(true);

            ClearRows();

            if (results == null)
                return;

            int rowCount = Mathf.Min(
                results.Count,
                Mathf.Min(rankTexts?.Length ?? 0,
                    Mathf.Min(nameTexts?.Length ?? 0, scoreTexts?.Length ?? 0)));

            for (int index = 0; index < rowCount; index++)
            {
                rankTexts[index].SetText($"{index + 1}");
                nameTexts[index].SetText(results[index].PlayerName ?? "Unknown");
                scoreTexts[index].SetText(results[index].Score.ToString());
            }
        }

        public void HideResults()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void ClearRows()
        {
            int rowCount = Mathf.Min(
                rankTexts?.Length ?? 0,
                Mathf.Min(nameTexts?.Length ?? 0, scoreTexts?.Length ?? 0));

            for (int index = 0; index < rowCount; index++)
            {
                rankTexts[index].SetText(string.Empty);
                nameTexts[index].SetText(string.Empty);
                scoreTexts[index].SetText(string.Empty);
            }
        }
    }
}
