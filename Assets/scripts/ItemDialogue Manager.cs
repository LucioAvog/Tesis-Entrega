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
    
    // Solo guardamos el texto puro, ignorando paneles y actores
    private Queue<string> lineasDeTexto = new Queue<string>(); 
    private Coroutine typingCoroutine;
    private string lineaActual;
    private bool estaInspeccionando = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (itemPopupPanel != null) itemPopupPanel.SetActive(false);
    }

    private void Update()
    {
        if (!estaInspeccionando) return;

        // Permite avanzar con Espacio o Clic izquierdo (optimizado)
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            AvanzarTexto();
        }
    }

    public void MostrarItem(ItemData item)
    {
        if (item == null) return;

        estaInspeccionando = true;
        lineasDeTexto.Clear();

        if (item.itemDialogue != null && item.itemDialogue.conversationLines.Count > 0)
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

        // Rellenar datos visuales
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

    private void AvanzarTexto()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            if (itemDescriptionText != null) itemDescriptionText.text = lineaActual; // Autocompletar
            return;
        }

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
        if (itemDescriptionText == null)
        {
            typingCoroutine = null;
            yield break;
        }

        itemDescriptionText.text = "";
        foreach (char letra in texto.ToCharArray())
        {
            itemDescriptionText.text += letra;
            yield return new WaitForSeconds(textSpeed);
        }
        typingCoroutine = null;
    }

    public void CerrarPopup()
    {
        estaInspeccionando = false;
        if (itemPopupPanel != null) itemPopupPanel.SetActive(false);
    }
}