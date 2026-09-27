using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class SettingModeChoose : MonoBehaviour
{
  [Header("Setting")]
  [SerializeField]private GameObject soundSet;
  [SerializeField]private GameObject mouseSet;
  [SerializeField]private GameObject debugSet;
  [SerializeField]private GameObject mikeSet;
  
  private void Start()
  {
    mouseSet.SetActive(false);
    debugSet.SetActive(false);
    mikeSet.SetActive(false);
  }

  public void ChooseMode1()
  {
    soundSet.SetActive(true);
    mouseSet.SetActive(false);
    debugSet.SetActive(false);
    mikeSet.SetActive(false);
  }
  public void ChooseMode2()
  {
    soundSet.SetActive(false);
    mouseSet.SetActive(true);
    debugSet.SetActive(false); 
    mikeSet.SetActive(false);
  }
  public void ChooseMode3()
  {
    soundSet.SetActive(false);
    mouseSet.SetActive(false);
    debugSet.SetActive(true);
    mikeSet.SetActive(false);
  }

  public void ChooseMode4()
  {
    soundSet.SetActive(false);
    mouseSet.SetActive(false);
    debugSet.SetActive(false);
    mikeSet.SetActive(true);
  }


}
