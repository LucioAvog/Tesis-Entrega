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
        if (itemData != null && itemData.itemDialogue != null)
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.GetConversation(itemData.itemDialogue);
            }
        }
    }
}