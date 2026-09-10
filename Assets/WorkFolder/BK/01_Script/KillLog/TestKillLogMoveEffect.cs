using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestKillLogMoveEffect : MonoBehaviour
{
    private bool notFirst = false;
    private void OnEnable()
    {
        if (notFirst == false)
        {
            notFirst = true;
            return;
        }
        else
        {
            MoveKillLog();
        }
    }

    private void MoveKillLog()
    {
        Sequence s = DOTween.Sequence();
        s.Append(gameObject.transform.DOLocalMove(new Vector3(-2,2,-6),0.3f)).SetEase(Ease.OutExpo); //화면까지 옴
        s.Append(gameObject.transform.DOShakePosition(0.5f,new Vector3(0f,0f,0.5f),10,150,false,true));
        //s.Append(gameObject.transform.DOShakeScale(0.5f,new Vector3(0.5f,0,0.5f),10,90,true));
        s.AppendInterval(1f);
        s.Append(gameObject.transform.DOLocalMove(new Vector3(-2, 2, -12), 0.2f));
        s.AppendCallback(() =>
        {
            gameObject.transform.position = new Vector3(-3, 2, 41);
            gameObject.SetActive(false);
        });
        /*
        gameObject.transform.DOLocalMove(new Vector3(-2,2,-6),0.5f); //화면까지 옴
        StartCoroutine(KillLogMoveCO());*/
    }

    /*private IEnumerator KillLogMoveCO()
    {
        yield return new WaitForSeconds(2f);
        gameObject.transform.DOLocalMove(new Vector3(-2,2,-12),0.5f);
        yield return new WaitForSeconds(0.5f);
        gameObject.transform.position = new Vector3(-3,2,41);
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
       
    }*/
}
