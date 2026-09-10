using System;
using System.Collections;
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

    private Coroutine dropRoutine;

    private void Awake()
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].target != null)
                entries[i].target.SetActive(false);
        }
    }

    public void Drop() // 떨굴때 부르기
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

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
