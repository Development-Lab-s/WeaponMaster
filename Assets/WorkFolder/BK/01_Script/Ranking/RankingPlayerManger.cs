using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RankingPlayerManger : MonoBehaviour
{
  public SetMyRanking[]  setMyRankings;

  [Header("Ranking")]
  public List<string> names;
  public List<int> scores;
  public List<int> kills;
  private void Awake()
  {
    setMyRankings = GetComponentsInChildren<SetMyRanking>();
  }

  private void Start()
  {
    foreach (var v in setMyRankings)
    {
      names.Add(v._name);
      scores.Add(v._score);
      kills.Add(v._kill);
    }
  }
}
