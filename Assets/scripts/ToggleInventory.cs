using UnityEngine;

public class ToggleInventory : MonoBehaviour
{
    public GameObject inventoryPanel;

    public void TogglePanel()
    {
        if (inventoryPanel == null) return;

        // Comprobamos si el inventario está actualmente abierto o cerrado
        bool estaAbierto = inventoryPanel.activeSelf;

        if (estaAbierto)
        {
            // Si está abierto, lo cerramos
            inventoryPanel.SetActive(false);
        }
        else
        {
            // Si está cerrado, lo abrimos y apagamos el punto/notificación de nuevo ítem
            inventoryPanel.SetActive(true);

            if (InventoryManager.Instance != null && InventoryManager.Instance.iconoNotificacion != null)
            {
                InventoryManager.Instance.iconoNotificacion.SetActive(false);
            }
        }
    }
}