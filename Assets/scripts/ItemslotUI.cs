using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler
{
    private ItemData itemData;
    private GameObject inventoryPanel;

    public void Setup(ItemData item, GameObject panel)
    {
        itemData = item;
        inventoryPanel = panel;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemData != null)
        {
            // Oculta la mochila al seleccionar un ítem
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            // CORREGIDO: Abre el ítem en ItemDialogueManager y NO en el DialogueManager de la novela
            if (ItemDialogueManager.Instance != null)
            {
                ItemDialogueManager.Instance.MostrarItem(itemData);
            }
        }
    }
}