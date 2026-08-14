using UnityEngine;

public class ToggleInventory : MonoBehaviour
{
    public GameObject inventoryPanel;

    public void TogglePanel()
    {
        if (inventoryPanel != null)
        {
            bool currentState = inventoryPanel.activeSelf;
            inventoryPanel.SetActive(!currentState);
        }
    }
}