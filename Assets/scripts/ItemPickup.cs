using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("ADN del Ítem (Arrastra el ItemData aquí)")]
    public ItemData datosDelItem; 

    [Header("Configuración")]
    public bool destruirAlRecoger = true;

    public void RecogerObjeto()
    {
        if (datosDelItem == null) 
        {
            Debug.LogWarning("El botón/objeto en la escena no tiene un ItemData asignado.");
            return;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(datosDelItem);
        }
        else
        {
            Debug.LogWarning("No hay un InventoryManager activo en la escena.");
        }

        // 1. Mostrar la ventana de inspección (100% aislada)
        if (ItemDialogueManager.Instance != null)
        {
            ItemDialogueManager.Instance.MostrarItem(datosDelItem);
        }

        if (destruirAlRecoger)
        {
            gameObject.SetActive(false);
        }
    }
}