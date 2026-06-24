using UnityEngine;
using UnityEngine.UI;

public class ActivarImagenBloqueante : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject imagenObjeto;   // La imagen/ventana que quieres mostrar
    [SerializeField] private Button miBoton;            // El botón que enciende/apaga
    [SerializeField] private CanvasGroup bloqueadorUI;   // El panel que bloqueará el resto de la pantalla

    void Start()
    {
        // Estado inicial: todo apagado
        if (imagenObjeto != null) imagenObjeto.SetActive(false);
        
        SetBloqueo(false);

        // Asignar evento al botón
        if (miBoton != null)
        {
            miBoton.onClick.AddListener(AlternarEstado);
        }
    }

    private void AlternarEstado()
    {
        if (imagenObjeto == null) return;

        // Invertimos el estado actual de la imagen
        bool nuevoEstado = !imagenObjeto.activeSelf;
        
        imagenObjeto.SetActive(nuevoEstado);
        
        // Activamos o desactivamos el bloqueo del resto de la pantalla
        SetBloqueo(nuevoEstado);
    }

    private void SetBloqueo(bool activarBloqueo)
    {
        if (bloqueadorUI == null) return;

        // Si activarBloqueo es true, bloquea los clics (blocksRaycasts = true)
        bloqueadorUI.blocksRaycasts = activarBloqueo;
        
        // Opcional: interactable controla si se pueden usar los elementos hijos de ese grupo
        bloqueadorUI.interactable = activarBloqueo; 
    }

    void OnDestroy()
    {
        if (miBoton != null)
        {
            miBoton.onClick.RemoveListener(AlternarEstado);
        }
    }
}