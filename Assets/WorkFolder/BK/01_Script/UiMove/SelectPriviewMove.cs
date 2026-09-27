using System;
using DG.Tweening;
using UnityEngine;

public class SelectPriviewMove : MonoBehaviour
{
  [SerializeField] private GameObject select;
  [SerializeField] private GameObject soundPriview;
  [SerializeField] private GameObject mousePriview;
  [SerializeField] private GameObject debugPriview;
  [SerializeField] private GameObject mikePriview;

  private void Start()
  {
    soundPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }

  public void PriviewSelectSound()
  {
    soundPriview.SetActive(true);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }
  public void PriviewSelectMouse()
  {
    soundPriview.SetActive(false);
    mousePriview.SetActive(true);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }
  public void PriviewSelectDebug()
  {
    soundPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(true);
  }
  public void PriviewSelectMike()
  {
    soundPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(true);
    debugPriview.SetActive(false);
  }
  public void SelectMoveSound()
  {
    select.transform.DOMove(soundPriview.transform.position, 0.7f);
    soundPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }

  public void SelectMoveMouse()
  {
    select.transform.DOMove(mousePriview.transform.position, 0.7f);
    mousePriview.SetActive(false);
    soundPriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }

  public void SelectMoveMike()
  {
    select.transform.DOMove(mikePriview.transform.position, 0.7f);
    mikePriview.SetActive(false);
    soundPriview.SetActive(false);
    debugPriview.SetActive(false);
    mousePriview.SetActive(false);
  }

  public void SelectMoveDebug()
  {
    select.transform.DOMove(debugPriview.transform.position, 0.7f);
    debugPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }

  public void OffPriview()
  {
    debugPriview.SetActive(false);
    mousePriview.SetActive(false);
    mikePriview.SetActive(false);
    debugPriview.SetActive(false);
  }
}
