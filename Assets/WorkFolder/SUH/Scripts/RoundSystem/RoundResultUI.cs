using System;
using System.Collections.Generic;
using DevLib.ServiceLocator;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using WorkFolder.SUH.Scripts.RoundSystem.Lib;
using WorkFolder.SUH.Scripts.RoundSystem.Lib.Interface;

namespace WorkFolder.SUH.Scripts.RoundSystem
{
    public sealed class RoundResultUI : MonoBehaviour, IRoundResultUI
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text[] rankTexts;
        [SerializeField] private TMP_Text[] nameTexts;
        [SerializeField] private TMP_Text[] scoreTexts;
        [SerializeField] private TMP_Text[] killTexts;
        [SerializeField] private RectTransform myRow;
        private TMP_Text myText;
        public UnityEvent OnActiveEvent;

        private void Awake()
        {
            ServiceLocator.Register<IRoundResultUI>(this);
            if (root != null && root.TryGetComponent(out Canvas canvas))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
            }
            ConfigureText(timerText, 36f, 64f);
            foreach (TMP_Text[] column in new[] { rankTexts, nameTexts, scoreTexts, killTexts })
                if (column != null)
                    foreach (TMP_Text text in column) ConfigureText(text, 20f, 44f);
            if (myRow != null && nameTexts != null && nameTexts.Length > 0 && nameTexts[0] != null)
            {
                myText = Instantiate(nameTexts[0], myRow);
                myText.name = "MyRoundResult";
                myText.rectTransform.anchorMin = Vector2.zero;
                myText.rectTransform.anchorMax = Vector2.one;
                myText.rectTransform.offsetMin = new Vector2(15f, 0f);
                myText.rectTransform.offsetMax = new Vector2(-15f, 0f);
                ConfigureText(myText, 18f, 26f);
            }
            HideResults();
        }


        private void OnDestroy() => ServiceLocator.UnRegister<IRoundResultUI>(this);

        public void Present(RoundState state, int round, float seconds, IReadOnlyList<RoundResultEntry> results, string localId, bool mapReady)
        {
            bool show = state == RoundState.Result || state == RoundState.LoadingNextMap || (!mapReady && round > 0);
            if (show)
            {
                ShowResults(results);
                if (myText != null)
                {
                    myText.text = string.Empty;
                    for (int i = 0; i < results.Count; i++)
                        if (results[i].PlayerId == localId)
                            myText.SetText($"MY  #{i + 1}    {results[i].PlayerName}    Kill {results[i].KillCount}    Score {results[i].Score}");
                }
            }
            else HideResults();
            int remaining = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            if (timerText != null) timerText.SetText($"{remaining / 60:00}:{remaining % 60:00}");
        }

        public void ShowResults(IReadOnlyList<RoundResultEntry> results)
        {
            if (root != null && !root.activeSelf) root.SetActive(true);
            OnActiveEvent?.Invoke();
            int count = Mathf.Min(rankTexts?.Length ?? 0,
                Mathf.Min(nameTexts?.Length ?? 0, Mathf.Min(scoreTexts?.Length ?? 0, killTexts?.Length ?? 0)));
            for (int i = 0; i < count; i++)
            {
                bool populated = results != null && i < results.Count;
                Set(rankTexts[i], populated ? (i + 1).ToString() : string.Empty);
                Set(nameTexts[i], populated ? results[i].PlayerName : string.Empty);
                Set(scoreTexts[i], populated ? results[i].Score.ToString() : string.Empty);
                Set(killTexts[i], populated ? results[i].KillCount.ToString() : string.Empty);
            }
        }

        public void HideResults()
        {
            if (root != null && root.activeSelf) root.SetActive(false);
        }

        private static void ConfigureText(TMP_Text text, float minimum, float maximum)
        {
            if (text == null) return;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
            text.fontSize = maximum;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Center;
            text.margin = Vector4.zero;
            text.richText = false;
            text.raycastTarget = false;
        }

        private static void Set(TMP_Text text, string value)
        {
            if (text != null) text.SetText(value);
        }
    }
}
