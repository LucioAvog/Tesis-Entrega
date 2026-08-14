using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PickableItem : MonoBehaviour
{
    public ItemData itemData;
    public DialogueScript pickupDialogue; 

    private void OnMouseDown()
    {
        if (itemData != null)
        {
            InventoryManager.Instance.AddItem(itemData);

            if (pickupDialogue != null && DialogueManager.Instance != null)
            {
                DialogueManager.Instance.GetConversation(pickupDialogue);
            }

            Destroy(gameObject);
        }
    }
}