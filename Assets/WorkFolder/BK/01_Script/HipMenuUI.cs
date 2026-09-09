using System;
using UnityEngine;

public class HipMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject hipButtons;
    [SerializeField] private GameObject hipButtons2;

    private void Start()
    {
        hipButtons.SetActive(false);
        hipButtons2.SetActive(false);
    }

    public void Show()
    {
        hipButtons.SetActive(true);
        hipButtons2.SetActive(true);
        Debug.Log("HipMenuUI Show");
    }

    public void Hide()
    {
        hipButtons.SetActive(false);
        hipButtons2.SetActive(false);
    }
}