using System;
using TMPro;
using UnityEngine;

public class BKDeadLogTest : MonoBehaviour
{
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        text.SetText(KillLog("바부","부바"));
    }

    public string KillLog(string killer, string player)
    {
        string killLog = $"{killer}가 {player}를 처치하였습니다.";
        return killLog;
    }
}
