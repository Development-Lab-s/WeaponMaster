using System.Collections;
using UnityEngine;

public class DropText : MonoBehaviour
{
    [SerializeField] private GameObject[] textObjects;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float interval = 1f;

    private Coroutine dropRoutine;

    private void Awake()
    {
        for (int i = 0; i < textObjects.Length; i++)
        {
            textObjects[i].SetActive(false);
        }
    }

    public void Drop()
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);

        dropRoutine = StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        if (startDelay > 0f)
            yield return Wait(startDelay);

        for (int i = 0; i < textObjects.Length; i++)
        {
            Debug.unityLogger.Log(textObjects[i]);
            textObjects[i].SetActive(true);

            if (i < textObjects.Length - 1 && interval > 0f)
                yield return Wait(interval);
        }

        dropRoutine = null;
    }

    private object Wait(float seconds)
    {

        return new WaitForSeconds(seconds);
    }
}
