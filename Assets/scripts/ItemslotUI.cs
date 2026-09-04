using UnityEngine;
using UnityEngine.UI; 

public class ItemSlotUI : MonoBehaviour
{
    private ItemData itemData;
    private GameObject inventoryPanel;

    public void Setup(ItemData item, GameObject panel)
    {
        itemData = item;
        inventoryPanel = panel;
        
        // Conexión exclusiva mediante el componente Button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnSlotClicked);
        }
    }

    public void OnSlotClicked()
    {
        Debug.Log("¡Clic detectado en el ítem: " + (itemData != null ? itemData.itemName : "NULO") + "!");

        if (itemData != null)
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false); 
            }

            if (ItemDialogueManager.Instance != null)
            {
                ItemDialogueManager.Instance.MostrarItem(itemData);
            }
            else
            {
                Debug.LogError("No se encontró el ItemDialogueManager en la escena.");
            }
        }
    }
}