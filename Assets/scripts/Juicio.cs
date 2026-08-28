using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrialManager : MonoBehaviour
{
    public static TrialManager Instance { get; private set; }

    [System.Serializable]
    public struct Statement
    {
        [TextArea(3, 5)]
        public string statementText;
        public string speakerName;
        public Sprite speakerPortrait;

        [Header("Mecánica de Objeción")]
        public bool isContradictory;
        public string correctEvidenceID;

        [Header("Diálogos Vinculados (DialogueScript)")]
        public DialogueScript conversationForStatement;
        public DialogueScript reactionOnObjection;      
        public DialogueScript reactionOnWrongObjection; 
    }

    [Header("Configuración del Juicio")]
    public DialogueScript introDialogue; 
    public List<Statement> testimonies = new List<Statement>();
    private int currentTestimonyIndex = 0;

    [Header("Botones Excepcionales durante Juicio")]
    [Tooltip("Arrastra botones adicionales que SÍ se puedan pulsar durante el juicio")]
    public List<Button> botonesPermitidosEnJuicio = new List<Button>();

    [Header("Estado del Juicio")]
    public int maxHealth = 3;
    private int currentHealth;
    public string selectedEvidenceID;

    [Header("Eventos de Derrota")]
    [Tooltip("Arrastra el DialogueScript que quieres que aparezca al llegar a 0 de vida")]
    public DialogueScript dialogoAlMorir;

    [Header("Referencias de UI")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private GameObject objectionEffectPanel;

    private bool isProcessingEvent = false; 
    private bool estaMuerto = false;

    // Propiedad pública para que DialogueManager pueda saber si moriste
    public bool EstaMuerto => estaMuerto;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentHealth = maxHealth;
        estaMuerto = false;
        UpdateHealthUI();
        StartCoroutine(IniciarSecuenciaConEspera());
    }

    private IEnumerator IniciarSecuenciaConEspera()
    {
        isProcessingEvent = true; 
        yield return new WaitForEndOfFrame();

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("¡Falta el DialogueManager en la escena!");
            yield break;
        }

        DialogueManager.Instance.permitirAvanceManual = true;

        if (introDialogue != null)
        {
            DialogueManager.Instance.GetConversation(introDialogue, () => 
            {
                isProcessingEvent = false;
                ShowCurrentStatement();
            });
        }
        else
        {
            isProcessingEvent = false;
            ShowCurrentStatement();
        }
    }

    public void ShowCurrentStatement()
    {
        if (estaMuerto || testimonies == null || testimonies.Count == 0) return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.permitirAvanceManual = false;
            DialogueManager.Instance.BloquearBotonesEscena(true, botonesPermitidosEnJuicio);
        }

        Statement current = testimonies[currentTestimonyIndex];

        if (current.conversationForStatement != null && DialogueManager.Instance != null)
        {
            isProcessingEvent = true;
            DialogueManager.Instance.GetConversation(current.conversationForStatement, () => {
                isProcessingEvent = false;
            });
        }
        else
        {
            if (speakerText != null) speakerText.text = current.speakerName;
            if (dialogueText != null) dialogueText.text = current.statementText;
        }
    }

    public void PresentEvidence()
    {
        if (estaMuerto || testimonies == null || testimonies.Count == 0) return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.dialogueActive)
        {
            DialogueManager.Instance.ForceStopDialogue();
        }

        Statement current = testimonies[currentTestimonyIndex];
        isProcessingEvent = true;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.permitirAvanceManual = true;
        }

        if (current.isContradictory && selectedEvidenceID == current.correctEvidenceID)
        {
            if (current.reactionOnObjection != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.GetConversation(current.reactionOnObjection, () => 
                {
                    isProcessingEvent = false;
                    NextStatement();
                });
            }
            else
            {
                isProcessingEvent = false;
                NextStatement();
            }
        }
        else
        {
            RestarVidaDirecta();

            if (!estaMuerto && current.reactionOnWrongObjection != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.GetConversation(current.reactionOnWrongObjection, () => 
                {
                    EvaluarEstadoDespuesDeFallo();
                });
            }
            else if (!estaMuerto)
            {
                EvaluarEstadoDespuesDeFallo();
            }
        }
    }

    public void RestarVidaDirecta(int cantidad = 1)
    {
        if (estaMuerto) return;

        currentHealth = Mathf.Clamp(currentHealth - cantidad, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth == 0)
        {
            GameOver();
        }
    }

    private void EvaluarEstadoDespuesDeFallo()
    {
        if (estaMuerto) return;

        isProcessingEvent = false;
        ShowCurrentStatement();
    }

    public void NextStatement()
    {
        if (estaMuerto || isProcessingEvent) return;
        currentTestimonyIndex = (currentTestimonyIndex + 1) % testimonies.Count;
        ShowCurrentStatement();
    }

    public void PreviousStatement()
    {
        if (estaMuerto || isProcessingEvent) return;
        currentTestimonyIndex--;
        if (currentTestimonyIndex < 0) currentTestimonyIndex = testimonies.Count - 1;
        ShowCurrentStatement();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = "Vida: " + currentHealth + "/" + maxHealth;
    }

    private void GameOver()
    {
        if (estaMuerto) return;
        estaMuerto = true;
        isProcessingEvent = false;

        Debug.Log("Has perdido el juicio.");

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.ForceStopDialogue();

            if (dialogoAlMorir != null)
            {
                DialogueManager.Instance.GetConversation(dialogoAlMorir);
            }
        }
    }
    
}