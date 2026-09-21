using System;
using DG.Tweening;
using UnityEngine;

public class OpenPal : MonoBehaviour
{
    [SerializeField] private GameObject cap;
    [SerializeField] private GameObject player;

    [SerializeField] private float playerTime;
    [SerializeField] float capTime;
    [SerializeField] float waitTime;
    
    [SerializeField] GameObject canvas;

    private void Awake()
    {
        player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y -2, cap.transform.position.z);
    }

    private void Start()
    {
        player.transform.DOMove(transform.position, playerTime).SetEase(Ease.OutBack)
            .OnComplete(() => canvas.SetActive(true));
        DOVirtual.DelayedCall(waitTime, () => cap.transform.DORotate(new Vector3(-2, 120, 100), capTime).SetEase(Ease.OutQuart));
        
    }
}
