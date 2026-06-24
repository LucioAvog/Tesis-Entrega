using UnityEngine;

public class BridgePanelesDialogo : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private GameObject objetoCarrusel; 

    private void Awake()
    {
        if (objetoCarrusel != null) 
            objetoCarrusel.SetActive(false);
    }

    public void ActivarPanelesVN()
    {
        if (objetoCarrusel == null) return;

        objetoCarrusel.SetActive(true);
        DialogueManager.BloquearAvancePorCarrusel = true;
    }

    public void ConfirmarSeleccion()
    {
        objetoCarrusel.SetActive(false);
        DialogueManager.BloquearAvancePorCarrusel = false;
    }
}