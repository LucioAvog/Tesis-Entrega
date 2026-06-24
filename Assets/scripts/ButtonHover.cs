using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverAndCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private bool scaleSaved = false; 

    [Header("Configuración de Tamaño")]
    [SerializeField] private float scaleMultiplier = 1.1f;

    [Header("Configuración del Cursor")]
    [SerializeField] private Texture2D customCursor; // Arrastra aquí tu textura de cursor
    [SerializeField] private Vector2 hotSpot = Vector2.zero; // El punto de clic (ej. esquina superior izquierda [0,0])
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    void Awake()
    {
        originalScale = transform.localScale;

        if (originalScale.magnitude < 0.01f)
        {
            originalScale = Vector3.one;
        }

        scaleSaved = true; 
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Aplica el escalado
        if (scaleSaved)
        {
            transform.localScale = originalScale * scaleMultiplier;
        }

        // Cambia al cursor personalizado si se asignó una textura
        if (customCursor != null)
        {
            Cursor.SetCursor(customCursor, hotSpot, cursorMode);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Devuelve el tamaño original
        if (scaleSaved)
        {
            transform.localScale = originalScale;
        }

        // Restablece el cursor por defecto
        ResetCursor();
    }

    void OnDisable()
    {
        // Asegura que si el botón se oculta/desactiva, el cursor y tamaño vuelvan a la normalidad
        ResetCursor();

        if (scaleSaved)
        {
            transform.localScale = originalScale;
        }
    }

    private void ResetCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}