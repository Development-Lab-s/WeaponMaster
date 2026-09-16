using TMPro;
using UnityEngine;

public class RankingRow : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text killText;

    public void SetData(
        int rank,
        string playerName,
        int score,
        int kills)
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        scoreText.text = score.ToString();
        killText.text = kills.ToString();
    }

    public void Clear()
    {
        rankText.text = "";
        nameText.text = "";
        scoreText.text = "";
        killText.text = "";
    }
}