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

  [Header("Select")]
  [SerializeField]private GameObject select;
  [SerializeField]private GameObject selectPos1;
  [SerializeField]private GameObject selectPos2;
  [SerializeField]private GameObject selectPos3;

  [Header("Preview")]
  [SerializeField] private GameObject previewSelect1;
  [SerializeField] private GameObject previewSelect2;
  [SerializeField] private GameObject previewSelect3;
  private void Start()
  {
    mouseSet.SetActive(false);
    debugSet.SetActive(false);
    
    selectPos2.SetActive(false);
    selectPos3.SetActive(false);
    
    previewSelect1.SetActive(false);
    previewSelect2.SetActive(false);
    previewSelect3.SetActive(false);
  }

  public void ChooseMode1()
  {
    soundSet.SetActive(true);
    mouseSet.SetActive(false);
    debugSet.SetActive(false);
    
    select.SetActive(true);
    select.transform.DOMoveX(selectPos1.transform.position.x, 0.3f).SetEase(Ease.InOutQuart);
  }
  public void ChooseMode2()
  {
    soundSet.SetActive(false);
    mouseSet.SetActive(true);
    debugSet.SetActive(false); 
    
    select.transform.DOMoveX(selectPos2.transform.position.x, 0.3f).SetEase(Ease.InOutQuart);
  }
  public void ChooseMode3()
  {
    soundSet.SetActive(false);
    mouseSet.SetActive(false);
    debugSet.SetActive(true);
    
    select.transform.DOMoveX(selectPos3.transform.position.x, 0.3f).SetEase(Ease.InOutQuart);
  }

  public void PreviewMode1()
  {
    previewSelect1.SetActive(true);
    previewSelect2.SetActive(false);
    previewSelect3.SetActive(false);
  }
  public void PreviewMode2()
  {
    previewSelect1.SetActive(false);
    previewSelect2.SetActive(true);
    previewSelect3.SetActive(false);
  }
  public void PreviewMode3()
  {
   previewSelect1.SetActive(false);
   previewSelect2.SetActive(false);
   previewSelect3.SetActive(true);
  }

  public void ClosePreview()
  {
   previewSelect1.SetActive(false);
   previewSelect2.SetActive(false);
   previewSelect3.SetActive(false);
  }

}
