using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ItemDialogueManager : MonoBehaviour
{
    public static ItemDialogueManager Instance { get; private set; }

    [Header("Referencias de la UI (Caja del Ítem)")]
    [SerializeField] private GameObject itemPopupPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Image itemIconImage;

    [Header("Ajustes")]
    [SerializeField] private float textSpeed = 0.05f;
    
    private Queue<string> lineasDeTexto = new Queue<string>(); 
    private Coroutine typingCoroutine;
    private string lineaActual;
    private bool estaInspeccionando = false;
    private bool justOpened = false;
    private bool permitirAvanceManualAnterior = true;

    public bool EstaInspeccionando => estaInspeccionando;

    private void Awake()
    {
        Instance = this;
        ResolverReferenciasDeEscena();
        if (itemPopupPanel != null) itemPopupPanel.SetActive(false);
    }

    private void ResolverReferenciasDeEscena()
    {
        GameObject[] sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject popupWithController = null;
        GameObject popupByName = null;

        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject.name != "ItemsPickupPopUp 1" || sceneObject.scene != gameObject.scene)
            {
                continue;
            }

            popupByName = sceneObject;
            if (sceneObject.GetComponent<PanelPopUp>() != null)
            {
                popupWithController = sceneObject;
                break;
            }
        }

        if (popupWithController != null)
        {
            itemPopupPanel = popupWithController;
        }
        else if (itemPopupPanel == null || itemPopupPanel.scene != gameObject.scene)
        {
            itemPopupPanel = popupByName;
        }

        if (itemPopupPanel == null)
        {
            Debug.LogError("No se encontró el panel ItemsPickupPopUp 1 en la escena.", this);
            return;
        }

        PanelPopUp popupController = itemPopupPanel.GetComponent<PanelPopUp>();
        if (popupController != null)
        {
            if (popupController.popupPanel != null &&
                popupController.popupPanel.scene == gameObject.scene)
            {
                itemPopupPanel = popupController.popupPanel;
            }

            itemNameText = EsComponenteDeEstaEscena(popupController.titleText)
                ? popupController.titleText
                : BuscarHijo<TextMeshProUGUI>("ItemName");
            itemDescriptionText = EsComponenteDeEstaEscena(popupController.descriptionText)
                ? popupController.descriptionText
                : BuscarHijo<TextMeshProUGUI>("ItemDescription");
            itemIconImage = EsComponenteDeEstaEscena(popupController.itemImageDisplay)
                ? popupController.itemImageDisplay
                : BuscarHijo<Image>("Item Icon");
        }
        else
        {
            itemNameText = BuscarHijo<TextMeshProUGUI>("ItemName");
            itemDescriptionText = BuscarHijo<TextMeshProUGUI>("ItemDescription");
            itemIconImage = BuscarHijo<Image>("Item Icon");
        }

        if (itemNameText == null || itemDescriptionText == null)
        {
            Debug.LogError(
                $"El popup '{itemPopupPanel.name}' no tiene referencias válidas para el nombre y la descripción.",
                this);
            return;
        }

    }

    private bool EsComponenteDeEstaEscena(Component component)
    {
        return component != null && component.gameObject.scene == gameObject.scene;
    }

    private T BuscarHijo<T>(string childName) where T : Component
    {
        T[] components = itemPopupPanel.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component.gameObject.name == childName)
            {
                return component;
            }
        }

        return null;
    }

    private void Update()
    {
        if (!estaInspeccionando || justOpened) return;

        bool presionoTeclaAvance = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        #if !ENABLE_INPUT_SYSTEM_ONLY
        if (Input.GetKeyDown(KeyCode.E)) presionoTeclaAvance = true;
        #endif

        if (presionoTeclaAvance)
        {
            AvanzarTexto();
        }
    }

    public void MostrarItem(ItemData item)
    {
        if (item == null) return;
        if (itemPopupPanel == null || itemNameText == null || itemDescriptionText == null)
        {
            Debug.LogError("No se puede mostrar el ítem: faltan referencias de la interfaz.", this);
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (!estaInspeccionando && DialogueManager.Instance != null)
        {
            permitirAvanceManualAnterior = DialogueManager.Instance.permitirAvanceManual;
            DialogueManager.Instance.permitirAvanceManual = false;
        }

        estaInspeccionando = true;
        justOpened = true;
        StartCoroutine(CooldownInput());

        lineasDeTexto.Clear();
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = "";
        }

        if (item.itemDialogue != null && item.itemDialogue.conversationLines != null && item.itemDialogue.conversationLines.Count > 0)
        {
            foreach (var line in item.itemDialogue.conversationLines)
            {
                lineasDeTexto.Enqueue(line.text); 
            }
        }
        else
        {
            lineasDeTexto.Enqueue(item.description);
        }

        if (itemNameText != null) itemNameText.text = item.itemName;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = item.icon;
            itemIconImage.gameObject.SetActive(item.icon != null);
        }

        if (itemPopupPanel != null)
        {
            itemPopupPanel.SetActive(true);
            itemPopupPanel.transform.SetAsLastSibling();
        }
        
        AvanzarTexto();
    }

    private IEnumerator CooldownInput()
    {
        yield return null; 
        justOpened = false;
    }

    private void AvanzarTexto()
    {
        // Si el texto se está escribiendo, muestra toda la línea de golpe.
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            if (itemDescriptionText != null) 
            {
                itemDescriptionText.maxVisibleCharacters = lineaActual.Length; 
            }
            return;
        }

        // Si terminó de escribir, pasa a la siguiente línea o cierra la ventana
        if (lineasDeTexto.Count > 0)
        {
            lineaActual = lineasDeTexto.Dequeue();
            typingCoroutine = StartCoroutine(EscribirTexto(lineaActual));
        }
        else
        {
            CerrarPopup();
        }
    }

    private IEnumerator EscribirTexto(string texto)
    {
        if (itemDescriptionText == null) yield break;

        // Configuración inicial del texto y cálculo de malla en TextMeshPro
        itemDescriptionText.text = texto;
        itemDescriptionText.ForceMeshUpdate(); // ¡CLAVE! Obliga a TMP a procesar la estructura del texto antes de animar
        itemDescriptionText.maxVisibleCharacters = 0;

        // Revela letra por letra automáticamente
        for (int i = 0; i <= texto.Length; i++)
        {
            itemDescriptionText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(textSpeed);
        }
        
        typingCoroutine = null;
    }

    public void CerrarPopup()
    {
        estaInspeccionando = false;
        justOpened = false;
        if (itemPopupPanel != null) itemPopupPanel.SetActive(false);

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.permitirAvanceManual = permitirAvanceManualAnterior;
        }
    }
}