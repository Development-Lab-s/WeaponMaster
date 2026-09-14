using System;
using TMPro;
using UnityEngine;

public class SetMyRanking : MonoBehaviour
{
   public TextMeshProUGUI[] rankingTexts;

   public string _name;
   public int _score=0;
   public int _kill=0;

   private void Awake()
   {
      rankingTexts = GetComponentsInChildren<TextMeshProUGUI>();
      SetRanking("황보경",1000,0);
   }
   public void SetRanking(string name, int score, int kill)
   {
      _name = name;
      _score = score;
      _kill = kill;
      
      rankingTexts[1].text = name;
      rankingTexts[2].text = score.ToString();
      rankingTexts[3].text = kill.ToString();
   }
}
