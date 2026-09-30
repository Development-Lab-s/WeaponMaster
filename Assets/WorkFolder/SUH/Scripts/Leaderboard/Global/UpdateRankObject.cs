using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WorkFolder.SUH.Scripts.Leaderboard.Global
{
    public class UpdateRankObject : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI killText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Image profileImage;

        public void UpdateRankText(int rank, string name, uint kill, double score)
        {
            rankText.SetText(rank.ToString());
            nameText.SetText(name);
            killText.SetText(kill.ToString());
            scoreText.SetText(score.ToString());
            profileImage.sprite = null;
        }
    }
}