using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DropText : MonoBehaviour
{
    [Serializable]
    private struct Entry
    {
        public GameObject target;
        public float delay;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private TextMeshProUGUI targetByText;

    private Coroutine dropRoutine;

    private void Awake()
    {
        DisableUis();
    }

    public void DisableUis()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].target != null)
                entries[i].target.SetActive(false);
        }
    }

    public void Dead(string deathMent,string targetBy)
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);
        deathText.SetText(deathMent);
        targetByText.SetText($"처치자 : {targetBy}");
        dropRoutine = StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].delay > 0f)
                yield return new WaitForSeconds(entries[i].delay);
            if (entries[i].target != null)
                entries[i].target.SetActive(true);
        }

        dropRoutine = null;
    }
}
