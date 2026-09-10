using System;
using System.Collections.Generic;
using UnityEngine;

public class KillLogPoolManger : MonoBehaviour
{
    [SerializeField]private GameObject killLog;
    [SerializeField]private Transform parent;
    public List<GameObject> killLogList;
    [SerializeField]private int maxListCount = 5;
    
    public static KillLogPoolManger instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        for (int i = 0; i < maxListCount; i++)
        {
            GameObject go = Instantiate(killLog, parent);
            go.SetActive(false);
            killLogList.Add(go);
        }
    }
}
