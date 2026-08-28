using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PanelPopUp : MonoBehaviour
{
    public static PanelPopUp Instance { get; private set; }

    [Header("Referencias UI")]
    public GameObject popupPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public Image itemImageDisplay;

    private bool isOpen = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

    public void MostrarItem(string nombre, string descripcion, Sprite icono = null)
    {
        if (DialogueManager.Instance != null) DialogueManager.Instance.permitirAvanceManual = false;

        if (titleText != null) titleText.text = nombre;
        if (descriptionText != null) descriptionText.text = descripcion;

        if (itemImageDisplay != null)
        {
            if (icono != null)
            {
                itemImageDisplay.sprite = icono;
                itemImageDisplay.gameObject.SetActive(true);
            }
            else
            {
                itemImageDisplay.gameObject.SetActive(false);
            }
        }

        if (popupPanel != null) popupPanel.SetActive(true);
        isOpen = true;
    }

    public void CerrarPopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        isOpen = false;

        if (DialogueManager.Instance != null) DialogueManager.Instance.permitirAvanceManual = true;
    }
}