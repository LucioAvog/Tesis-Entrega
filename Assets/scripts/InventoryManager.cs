using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public Transform itemsParent;
    public GameObject slotPrefab;

    private List<ItemData> inventory = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("InventoryManager recibió un ItemData nulo.");
            return;
        }

        if (!inventory.Contains(item))
        {
            inventory.Add(item);
            UpdateUI();
        }
        else
        {
            UpdateUI();
        }
    }

    public bool HasItem(string itemName)
    {
        return inventory.Exists(i => i.itemName == itemName);
    }

    public void RemoveItem(ItemData item)
    {
        if (inventory.Contains(item))
        {
            inventory.Remove(item);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (itemsParent == null)
        {
            Debug.LogError("InventoryManager no tiene asignado itemsParent.");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogError("InventoryManager no tiene asignado slotPrefab.");
            return;
        }

        foreach (Transform child in itemsParent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in inventory)
        {
            GameObject newSlot = Instantiate(slotPrefab, itemsParent);
            Image slotImage = newSlot.GetComponentInChildren<Image>();
            if (slotImage != null)
            {
                slotImage.sprite = item.icon;
            }

            ItemSlotUI slotUI = newSlot.GetComponent<ItemSlotUI>();
            if (slotUI == null)
            {
                slotUI = newSlot.AddComponent<ItemSlotUI>();
            }
            slotUI.Setup(item, itemsParent.gameObject);
        }
    }

    private void OnEnable()
    {
        if (inventory.Count > 0) UpdateUI();
    }
}