using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ItemsPickUpPopUp : MonoBehaviour
{
    public static ItemsPickUpPopUp Instance { get; private set; }

    [Header("Objeto Padre Popup")]
    [Tooltip("Arrastra aquí el objeto ItemsPickupPopUp")]
    public GameObject popupPanel; 

    [Header("Hijos de UI (Textos e Imagen)")]
    public TextMeshProUGUI itemNameText;        // Hijo: ItemName
    public TextMeshProUGUI itemDescriptionText; // Hijo: ItemDescription
    public Image itemIconImage;                // Hijo: Item Icon

    private bool isOpen = false;
    private DialogueScript pendingItemDialogue;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GameObject exitPrompt = GameObject.Find("Presiona E para salir");
        if (exitPrompt != null && exitPrompt.transform.parent != transform)
        {
            exitPrompt.transform.SetParent(transform, true);
        }
        
        // El popup inicia desactivado para no estorbar en pantalla
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    void Update()
    {
        if (!isOpen) return;

        bool presionoEspacio = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) presionoEspacio = true;
        #if !ENABLE_INPUT_SYSTEM_ONLY
        if (Input.GetKeyDown(KeyCode.Space)) presionoEspacio = true;
        #endif

        if (presionoEspacio)
        {
            CerrarPopup();
        }
    }

    // Recibe directamente el ScriptableObject ItemData para escribir en sus hijos
    public void MostrarItem(ItemData item)
    {
        if (item == null) return;

        // Guardamos si el ítem tiene un diálogo propio asignado
        pendingItemDialogue = item.itemDialogue;

        // Bloqueamos el avance del diálogo de fondo para que presionar Espacio solo cierre este popup
        if (DialogueManager.Instance != null) 
        {
            DialogueManager.Instance.permitirAvanceManual = false;
        }

        // Escribimos la información de ItemData en los componentes hijos
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemDescriptionText != null) itemDescriptionText.text = item.description;
        
        if (itemIconImage != null)
        {
            if (item.icon != null)
            {
                itemIconImage.sprite = item.icon;
                itemIconImage.gameObject.SetActive(true);
            }
            else
            {
                itemIconImage.gameObject.SetActive(false);
            }
        }

        // Activamos el objeto contenedor sin tocar/ocultar los paneles de diálogo de fondo
        if (popupPanel != null) popupPanel.SetActive(true);
        isOpen = true;
    }

    private void CerrarPopup()
    {
        // Ocultamos ÚNICAMENTE el panel del ítem
        if (popupPanel != null) popupPanel.SetActive(false);
        isOpen = false;

        // Devolvemos el control al DialogueManager para que el diálogo de fondo continúe intacto
        if (DialogueManager.Instance != null) 
        {
            DialogueManager.Instance.permitirAvanceManual = true;

            // Si el ItemData tenía un diálogo específico asignado, lo iniciamos ahora
            if (pendingItemDialogue != null)
            {
                DialogueManager.Instance.GetConversation(pendingItemDialogue);
                pendingItemDialogue = null;
            }
        }
    }
}