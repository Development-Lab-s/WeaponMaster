using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class Panggksmsrj : MonoBehaviour
{
  private void Start()
  {
    gameObject.transform.localScale = Vector3.zero;
    StartCoroutine(CO());
  }

  public IEnumerator CO()
  {
    yield return new WaitForSeconds(1f);
    Pnag();
  }

  public void Pnag()
  {
    gameObject.transform.DOScale(1, 1f).SetEase(Ease.InOutCubic);
  }
}
