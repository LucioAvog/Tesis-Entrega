using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("ADN del Ítem (Arrastra el ItemData aquí)")]
    public ItemData datosDelItem; 

    [Header("Configuración")]
    public bool destruirAlRecoger = true;

    public void RecogerObjeto()
    {
        if (datosDelItem == null) return;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(datosDelItem);
        }

        // Ahora solo usamos el manager bueno (el que tiene la animación)
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