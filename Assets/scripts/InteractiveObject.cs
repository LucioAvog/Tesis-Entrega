using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractiveObject : MonoBehaviour
{
    public string objectName = "Objeto Interactivo";

    public DialogueScript defaultDialogue;
    public DialogueScript unlockedDialogue;

    public string requiredItemName = "";
    public ItemData itemToGive;

    public bool isUnlocked = false;
    private bool itemAlreadyGiven = false;

    public InteractiveObject targetObjectToUnlock;

    private void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        if (!string.IsNullOrEmpty(requiredItemName) && !isUnlocked)
        {
            if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredItemName))
            {
                isUnlocked = true;
            }
        }

        if (itemToGive != null && !itemAlreadyGiven)
        {
            if (string.IsNullOrEmpty(requiredItemName) || isUnlocked)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(itemToGive);
                    itemAlreadyGiven = true;
                    isUnlocked = true;
                }
            }
        }

        DialogueScript scriptToPlay = isUnlocked ? unlockedDialogue : defaultDialogue;

        if (scriptToPlay != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.GetConversation(scriptToPlay);
        }

        if (isUnlocked && targetObjectToUnlock != null)
        {
            targetObjectToUnlock.UnlockNewDialogue();
        }
    }

    public void UnlockNewDialogue()
    {
        isUnlocked = true;
    }
}