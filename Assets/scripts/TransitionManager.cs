using System.Collections;
using UnityEngine;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("Configuraci�n")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            FadeIn();
        }
    }

    public void FadeIn()
    {
        if (fadeCanvasGroup != null)
            StartCoroutine(FadeRoutine(1f, 0f));
    }

    public void FadeOut()
    {
        if (fadeCanvasGroup != null)
            StartCoroutine(FadeRoutine(0f, 1f));
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
    {
        fadeCanvasGroup.blocksRaycasts = true;

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = endAlpha;

        if (endAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}