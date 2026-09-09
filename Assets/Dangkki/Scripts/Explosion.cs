using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Explosion : MonoBehaviour
{
    [Serializable]
    public class TimedUI
    {
        public GameObject target;
        public float delaySeconds = 2f; 
    }
    
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoScreen;
    
    [SerializeField] private TimedUI[] uiToShow;

    private Coroutine showCo;

    private void Awake()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnVideoFinished;

        videoScreen.gameObject.SetActive(false);
        HideAll();
    }

    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        if (showCo != null)
            StopCoroutine(showCo);

        HideAll();
        videoPlayer.Prepare();
    }

    private void HideAll()
    {
        foreach (var ui in uiToShow)
        {
            if (ui.target != null)
                ui.target.SetActive(false);
        }
    }

    private void OnPrepared(VideoPlayer vp)
    {
        videoScreen.texture = vp.texture;
        videoScreen.gameObject.SetActive(true);
        vp.Play();

        showCo = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        var ordered = uiToShow.Where(ui => ui.target != null)
                              .OrderBy(ui => ui.delaySeconds)
                              .ToArray();

        float elapsed = 0f;
        foreach (var ui in ordered)
        {
            float wait = ui.delaySeconds - elapsed;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }

            ui.target.SetActive(true);
        }

        showCo = null;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoScreen.gameObject.SetActive(false);
    }
}
