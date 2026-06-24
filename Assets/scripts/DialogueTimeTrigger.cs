using System.Collections;
using UnityEngine;

public class DialogueTimeTrigger : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private DialogueScript dialogueToTrigger;

    [Header("Configuraci�n de Tiempo")]
    [SerializeField] private float delayInSeconds = 3f; 
    [SerializeField] private bool triggerOnStart = true; 

    void Start()
    {
        
        if (dialogueManager == null)
        {
            dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        }

        if (triggerOnStart)
        {
            StartTimer();
        }
    }

    public void StartTimer()
    {
        if (dialogueToTrigger != null)
        {
            StartCoroutine(WaitAndTriggerRoutine());
        }
    }

    private IEnumerator WaitAndTriggerRoutine()
    {
        // Espera la cantidad de segundos configurada
        yield return new WaitForSeconds(delayInSeconds);

        if (dialogueManager != null)
        {
            dialogueManager.GetConversation(dialogueToTrigger);
        }
        else
        {
            Debug.LogError("No se puede iniciar el di�logo porque falta el DialogueManager.");
        }
    }
}