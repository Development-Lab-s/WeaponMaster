using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestKillLogMoveEffect : MonoBehaviour
{
    private void Start()
    {
        MoveKillLog();
    }

    private void MoveKillLog()
    {
        gameObject.transform.DOLocalMove(new Vector3(-2,2,-6),0.5f);
        StartCoroutine(KillLogMoveCO());
    }

    private IEnumerator KillLogMoveCO()
    {
        yield return new WaitForSeconds(2f);
        gameObject.transform.DOLocalMove(new Vector3(-2,2,-12),0.5f);
        yield return new WaitForSeconds(0.5f);
        gameObject.transform.position = new Vector3(-3,2,41);
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
       
    }
}
