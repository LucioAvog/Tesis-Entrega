using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CarruselPanelesInteractivo : MonoBehaviour
{
    [Header("Configuración de Paneles")]
    [SerializeField] private CanvasGroup[] paneles;
    
    [Header("Ajustes Visuales")]
    [SerializeField] private float tiempoTransicion = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float nivelOscuridad = 0.6f;

    private RectTransform[] rects;
    private Image[] imagenesPaneles;
    private Vector2[] posicionesIniciales;
    private float distanciaHorizontal;
    private int indiceActual = 0;
    private int indiceCentro = 0;
    private Coroutine corrutinaAnimacion;

    public int ObtenerIndiceActual() => indiceActual;

    private void Awake()
    {
        rects = new RectTransform[paneles.Length];
        imagenesPaneles = new Image[paneles.Length];
        posicionesIniciales = new Vector2[paneles.Length];
        indiceCentro = paneles.Length / 2;

        for (int i = 0; i < paneles.Length; i++)
        {
            if (paneles[i] != null)
            {
                rects[i] = paneles[i].GetComponent<RectTransform>();
                imagenesPaneles[i] = paneles[i].GetComponent<Image>();
                posicionesIniciales[i] = rects[i].anchoredPosition;
            }
        }

        if (rects.Length > 1 && rects[0] != null && rects[1] != null)
        {
            distanciaHorizontal = Mathf.Abs(posicionesIniciales[1].x - posicionesIniciales[0].x);
        }
    }

    private void OnEnable()
    {
        if (paneles == null || paneles.Length == 0) return;

        if (corrutinaAnimacion != null)
        {
            StopCoroutine(corrutinaAnimacion);
            corrutinaAnimacion = null;
        }

        indiceActual = indiceCentro;

        for (int i = 0; i < paneles.Length; i++)
        {
            if (paneles[i] != null)
            {
                bool esElCentro = (i == indiceActual);
                rects[i].anchoredPosition = posicionesIniciales[i];
                
                if (imagenesPaneles[i] != null)
                {
                    float factor = esElCentro ? 1f : (1f - nivelOscuridad);
                    imagenesPaneles[i].color = new Color(factor, factor, factor, 1f);
                }

                paneles[i].blocksRaycasts = esElCentro;
                paneles[i].interactable = esElCentro;
            }
        }
    }

    public void SiguientePanel() => IntentarCambiarPanel(indiceActual + 1);
    public void AnteriorPanel() => IntentarCambiarPanel(indiceActual - 1);

    private void IntentarCambiarPanel(int nuevoIndice)
    {
        if (corrutinaAnimacion != null || nuevoIndice < 0 || nuevoIndice >= paneles.Length) return;

        indiceActual = nuevoIndice;
        corrutinaAnimacion = StartCoroutine(TransicionarCarrusel());
    }

    private IEnumerator TransicionarCarrusel()
    {
        float tiempo = 0f;
        Vector2[] posInicio = new Vector2[paneles.Length];
        Color[] colorInicio = new Color[paneles.Length];
        float deltaXTarget = (indiceActual - indiceCentro) * distanciaHorizontal;

        for (int i = 0; i < paneles.Length; i++)
        {
            if (paneles[i] != null)
            {
                posInicio[i] = rects[i].anchoredPosition;
                colorInicio[i] = imagenesPaneles[i] != null ? imagenesPaneles[i].color : Color.white;
                paneles[i].blocksRaycasts = false;
                paneles[i].interactable = false;
            }
        }

        while (tiempo < tiempoTransicion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tiempo / tiempoTransicion);

            for (int i = 0; i < paneles.Length; i++)
            {
                if (paneles[i] != null)
                {
                    float targetX = posicionesIniciales[i].x - deltaXTarget;
                    rects[i].anchoredPosition = new Vector2(Mathf.Lerp(posInicio[i].x, targetX, t), rects[i].anchoredPosition.y);
                    
                    bool esElCentro = (i == indiceActual);
                    float factorTarget = esElCentro ? 1f : (1f - nivelOscuridad);
                    Color colorTarget = new Color(factorTarget, factorTarget, factorTarget, 1f);

                    if (imagenesPaneles[i] != null)
                    {
                        imagenesPaneles[i].color = Color.Lerp(colorInicio[i], colorTarget, t);
                    }
                }
            }
            yield return null;
        }

        for (int i = 0; i < paneles.Length; i++)
        {
            if (paneles[i] != null)
            {
                bool esElCentro = (i == indiceActual);
                float targetX = posicionesIniciales[i].x - deltaXTarget;
                rects[i].anchoredPosition = new Vector2(targetX, rects[i].anchoredPosition.y);

                if (imagenesPaneles[i] != null)
                {
                    float factor = esElCentro ? 1f : (1f - nivelOscuridad);
                    imagenesPaneles[i].color = new Color(factor, factor, factor, 1f);
                }

                paneles[i].blocksRaycasts = esElCentro;
                paneles[i].interactable = esElCentro;
            }
        }

        corrutinaAnimacion = null;
    }
}