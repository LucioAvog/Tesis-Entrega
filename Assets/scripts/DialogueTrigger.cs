using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Configuraci�n del Di�logo")]
    [SerializeField] private DialogueScript myDialogue;

    [Header("Referencias")]
    
    [SerializeField] private DialogueManager dialogueManager;

    public void TriggerDialogue()
    {
        if (dialogueManager != null && myDialogue != null)
        {
            dialogueManager.GetConversation(myDialogue);
        }
        else
        {
            Debug.LogWarning("�Falta asignar el DialogueManager o el DialogueScript en el bot�n!");
        }
    }
}