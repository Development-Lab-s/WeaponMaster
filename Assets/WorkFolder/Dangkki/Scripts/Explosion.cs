using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Explosion : MonoBehaviour //깡통수정
{
    [Serializable]
    public class TimedUI
    {
        public GameObject target;
        public float delaySeconds = 2f; 
    }
    
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoScreen;

    [SerializeField] private bool autoPlay = true;
    [SerializeField] private float autoPlayTime = 0f;
    
    [SerializeField] private TimedUI[] uiToShow;

    private Coroutine autoCo;
    private Coroutine playCo;
    private Coroutine showCo;

    private void Awake()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;

        videoPlayer.loopPointReached += OnVideoFinished;

        videoScreen.gameObject.SetActive(false);
        HideAll();
        
    }


    
    

    private void OnDestroy()
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void Start()
    {
        videoPlayer.Prepare();

        if (autoPlay)
            autoCo = StartCoroutine(AutoPlayRoutine());
    }

    private IEnumerator AutoPlayRoutine()
    {
        if (autoPlayTime > 0f)
            yield return new WaitForSeconds(autoPlayTime);

        autoCo = null;
        Play();
    }

    public void Play()
    {
        if (autoCo != null)
            StopCoroutine(autoCo);
        if (playCo != null)
            StopCoroutine(playCo);
        if (showCo != null)
            StopCoroutine(showCo);

        videoScreen.gameObject.SetActive(false);
        HideAll();

        playCo = StartCoroutine(PlayRoutine());
    }

    private void HideAll()
    {
        foreach (var ui in uiToShow)
        {
            if (ui.target != null)
                ui.target.SetActive(false);
        }
    }

    private IEnumerator PlayRoutine()
    {
        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared)
                yield return null;
        }

        if (videoPlayer.frame > 0)
        {
            videoPlayer.frame = 0;
            yield return null;
        }

        videoScreen.texture = videoPlayer.texture;
        videoScreen.gameObject.SetActive(true);
        videoPlayer.Play();

        playCo = null;
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
