using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BKDropText : MonoBehaviour
{
    [Serializable]
    private struct BKEntry
    {
        public GameObject target;
        public float delay;
    }

    [SerializeField] private BKDropText.BKEntry[] entries;
    [SerializeField] private TextMeshProUGUI deathText;

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

    public void Dead(string deathMent)
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);
        deathText.SetText(deathMent);
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
