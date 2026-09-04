using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Configuración de UI")]
    public GameObject inventoryPanel; 
    public Transform itemsParent;
    public GameObject slotPrefab;

    [Header("Notificación de Nuevo Ítem")]
    [Tooltip("Arrastra aquí la imagen/icono de notificación (punto rojo, exclamación, etc.)")]
    public GameObject iconoNotificacion;

    [Header("Controles")]
    [Tooltip("Tecla opcional para abrir/cerrar el inventario desde el teclado")]
    public KeyCode teclaAbrirInventario = KeyCode.I;

    private static readonly List<ItemData> inventory = new List<ItemData>();

    private void Awake()
    {
        // Garantiza que la instancia siempre sea válida en la escena actual
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();

        // Aseguramos que la notificación empiece apagada al iniciar
        if (iconoNotificacion != null)
        {
            iconoNotificacion.SetActive(false);
        }
    }

    private void Update()
    {
        // Si presionas la tecla configurada (ej. la 'I'), abre o cierra el inventario
        if (teclaAbrirInventario != KeyCode.None && Input.GetKeyDown(teclaAbrirInventario))
        {
            ToggleInventario();
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null) return;
        
        if (!inventory.Contains(item))
        {
            inventory.Add(item);
            
            // Si el inventario está cerrado cuando llega el ítem, activa la imagen de notificación
            if (inventoryPanel != null && !inventoryPanel.activeSelf)
            {
                if (iconoNotificacion != null)
                {
                    iconoNotificacion.SetActive(true);
                }
            }
        }
        UpdateUI();
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
        if (itemsParent == null || slotPrefab == null) return;

        foreach (Transform child in itemsParent)
        {
            Destroy(child.gameObject);
        }

        foreach (ItemData item in inventory)
        {
            GameObject newSlot = Instantiate(slotPrefab);
            newSlot.transform.SetParent(itemsParent, false);

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

            GameObject panelToHide = inventoryPanel != null ? inventoryPanel : itemsParent.parent.gameObject;
            slotUI.Setup(item, panelToHide);
        }
    }

    // --- MÉTODOS PARA ABRIR, CERRAR Y ALTERNAR EL INVENTARIO ---

    public void ToggleInventario()
    {
        if (inventoryPanel == null) return;

        if (inventoryPanel.activeSelf)
        {
            CerrarInventario();
        }
        else
        {
            AbrirInventario();
        }
    }

    public void AbrirInventario()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
        }

        // Desactiva automáticamente el icono de notificación al abrir la mochila
        if (iconoNotificacion != null)
        {
            iconoNotificacion.SetActive(false);
        }
    }

    public void CerrarInventario()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }
    }
}