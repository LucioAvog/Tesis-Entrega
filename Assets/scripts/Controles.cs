using UnityEngine;

public class Controles : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject imagenObjeto;   // La imagen/ventana que quieres mostrar
    [SerializeField] private CanvasGroup bloqueadorUI;   // El panel que bloqueará el resto de la pantalla

    void Start()
    {
        // Estado inicial: todo apagado
        if (imagenObjeto != null) imagenObjeto.SetActive(false);
        
        SetBloqueo(false);
    }

    public void AlternarEstado()
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
}