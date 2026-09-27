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
    [SerializeField] private Vector3 capPosition;
    
    [SerializeField] GameObject canvas;

    private Vector3 originRot;
    private Vector3 newRot = new Vector3(-2, 120, 100);

    private Vector3 originPlrPos;
    private Vector3 newPlrPos;

    private void Awake()
    {
    }

    private void Start()
    {
        originRot = cap.transform.eulerAngles;
        originPlrPos = new Vector3(player.transform.position.x, player.transform.position.y -2, cap.transform.position.z);
        player.transform.position = originPlrPos;
    }

    public void Open()
    {
        player.transform.DOLocalMove(capPosition, playerTime).SetEase(Ease.OutBack);
        DOVirtual.DelayedCall(waitTime, () => cap.transform.DORotate(newRot, capTime).SetEase(Ease.OutQuart));
    }

    public void Close()
    {
        player.transform.position = originPlrPos;
        cap.transform.eulerAngles = originRot;
    }
}
