using System;
using UnityEngine;

public class SettingModeChoose : MonoBehaviour
{
  [Header("Setting")]
  [SerializeField]private GameObject setting1;
  [SerializeField]private GameObject setting2;
  [SerializeField]private GameObject setting3;

  [Header("Select")]
  [SerializeField]private GameObject select1;
  [SerializeField]private GameObject select2;
  [SerializeField]private GameObject select3;
  private void Start()
  {
    ChooseMode1();
  }

  public void ChooseMode1()
  {
    setting1.SetActive(true);
    setting2.SetActive(false);
    setting3.SetActive(false);
    
    select1.SetActive(true);
    select2.SetActive(false);
    select3.SetActive(false);
  }
  public void ChooseMode2()
  {
    setting1.SetActive(false);
    setting2.SetActive(true);
    setting3.SetActive(false); 
    
    select1.SetActive(false);
    select2.SetActive(true);
    select3.SetActive(false);
  }
  public void ChooseMode3()
  {
    setting1.SetActive(false);
    setting2.SetActive(false);
    setting3.SetActive(true);
    
    select1.SetActive(false);
    select2.SetActive(false);
    select3.SetActive(true);
  }
}
