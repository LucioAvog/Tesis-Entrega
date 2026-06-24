using UnityEngine;
using UnityEngine.UI; 

[RequireComponent(typeof(Image))] 
public class CanvasSpriteAnimator : MonoBehaviour
{
    [Header("Configuración de la Animación")]
    [SerializeField] private Sprite[] spritesAnimacion;
    [SerializeField] private float framesPorSegundo = 6f;

    private Image uiImage; 
    private int frameActual;
    private float temporizador;

    void Start()
    {
        uiImage = GetComponent<Image>();

        if (spritesAnimacion == null || spritesAnimacion.Length == 0)
        {
            Debug.LogWarning($"¡Ey! Olvidaste asignar los sprites en {gameObject.name}.");
            enabled = false;
        }
    }

    void Update()
    {
        temporizador += Time.deltaTime;
        float tiempoPorFrame = 1f / framesPorSegundo;

        if (temporizador >= tiempoPorFrame)
        {
            temporizador -= tiempoPorFrame;
            frameActual = (frameActual + 1) % spritesAnimacion.Length;
            
            uiImage.sprite = spritesAnimacion[frameActual]; 
        }
    }
}