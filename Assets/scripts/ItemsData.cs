using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "NovelaVisual/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea(3, 5)] public string description;
    public DialogueScript itemDialogue;
}