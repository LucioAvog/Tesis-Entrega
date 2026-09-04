using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemDialogueManager : MonoBehaviour
{
    public static ItemDialogueManager Instance { get; private set; }

    [Header("Referencias de la UI (Caja del Ítem)")]
    [SerializeField] private GameObject itemPopupPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Image itemIconImage;

    [Header("Ajustes")]
    [SerializeField] private float textSpeed = 0.03f;
    
    private Queue<string> lineasDeTexto = new Queue<string>(); 
    private Coroutine typingCoroutine;
    private string lineaActual;
    private bool estaInspeccionando = false;
    private bool justOpened = false;

    private void Awake()
    {
        Instance = this;
        if (itemPopupPanel != null) itemPopupPanel.SetActive(false);
    }

    private void Update()
    {
        if (!estaInspeccionando || justOpened) return;

        // Si presionas Espacio o haces Clic mientras se escribe, acelera/autocompleta la línea
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            AvanzarTexto();
        }
    }

    public void MostrarItem(ItemData item)
    {
        if (item == null) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (DialogueManager.Instance != null)
        {
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
        // Si el texto se está escribiendo y haces clic/espacio, muestra todo de golpe
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
            DialogueManager.Instance.permitirAvanceManual = true;
        }
    }
}