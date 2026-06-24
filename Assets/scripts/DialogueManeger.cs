using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 

public class DialogueManager : MonoBehaviour
{
    [System.Serializable]
    public struct ChoicePanelGroup
    {
        public GameObject choicePanel;
        public Button buttonA, buttonB;
        public TextMeshProUGUI textA, textB;

        [HideInInspector] public Sprite defaultSpriteA, defaultSpriteB;

        public void InitializeDefaults()
        {
            if (buttonA != null && buttonA.image != null) defaultSpriteA = buttonA.image.sprite;
            if (buttonB != null && buttonB.image != null) defaultSpriteB = buttonB.image.sprite;
        }

        public void SetActive(bool active)
        {
            if (choicePanel != null) choicePanel.SetActive(active);
        }
    }

    [Header("UI General")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lineText;
    [SerializeField] private TextMeshProUGUI lineText2;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject dialoguePanel2;
    [SerializeField] private Image gameBackgroundDisplay;

    [Header("Configuración de Texto Animado")]
    [SerializeField] private float textSpeed = 0.03f;
    private Coroutine typingCoroutine;
    private string fullTextOfCurrentLine;
    private TextMeshProUGUI activeLineText;

    [Header("Imagen del Personaje")]
    [SerializeField] private Image characterImageDisplay;
    [SerializeField] private RectTransform characterImageRect;
    [SerializeField] private Vector2 positionLeft = new Vector2(-350, -90);
    [SerializeField] private Vector2 positionCenter = new Vector2(0, -90);
    [SerializeField] private Vector2 positionRight = new Vector2(350, -90);

    [Header("Efectos Especiales - Temblor")]
    [SerializeField] private Transform objectToShake;
    private Coroutine activeShakeCoroutine;

    [Header("Efectos de Audio (Canal Voz/Texto)")]
    [SerializeField] private AudioSource dialogueAudioSource;

    [Header("Efectos de Audio (Canal SFX Independiente)")]
    [SerializeField] private AudioSource sfxAudioSource; 
    private Coroutine sfxDurationCoroutine;              

    [Header("Paneles de Decisiones Separados")]
    [SerializeField] private ChoicePanelGroup choicesPanel1;
    [SerializeField] private ChoicePanelGroup choicesPanel2;

    private bool dialogueActive;
    private bool awaitingChoice;
    private Queue<Line> lines = new Queue<Line>();

    private DialogueScript currentConversation;
    private Vector2 defaultCharacterSize;

    public static bool BloquearAvancePorCarrusel { get; set; } = false;

    void Start()
    {
        choicesPanel1.InitializeDefaults();
        choicesPanel2.InitializeDefaults();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);
        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);

        if (characterImageRect != null) defaultCharacterSize = characterImageRect.sizeDelta;

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);

        if (dialogueAudioSource == null)
        {
            dialogueAudioSource = GetComponent<AudioSource>();
            if (dialogueAudioSource == null) dialogueAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1) sfxAudioSource = sources[1];
            else sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void AdvanceDialogue()
    {
        if (BloquearAvancePorCarrusel) return;

        if (dialogueActive && !awaitingChoice)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                if (activeLineText != null) activeLineText.text = fullTextOfCurrentLine;
                StopDialogueAudio();
            }
            else
            {
                ProduceNextLine();
            }
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AdvanceDialogue();
        }
    }

    public void GetConversation(DialogueScript dialogue)
    {
        if (dialogue.conversationLines == null || dialogue.conversationLines.Count == 0)
        {
            Debug.LogError("¡El DialogueScript no tiene líneas de texto agregadas!");
            return;
        }
        
        if (dialogueActive) return;
        StartDialogueSequence(dialogue);
    }

    private void StartDialogueSequence(DialogueScript dialogue)
    {
        currentConversation = dialogue;
        lines.Clear();
        foreach (var line in dialogue.conversationLines) lines.Enqueue(line);

        dialogueActive = true;
        awaitingChoice = false;

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);

        ProduceNextLine();
    }

    void ProduceNextLine()
    {
        if (lines.Count == 0)
        {
            if (currentConversation != null && currentConversation.nextConversation != null)
            {
                StartDialogueSequence(currentConversation.nextConversation);
            }
            else if (currentConversation != null && currentConversation.changeSceneOnEnd && !string.IsNullOrEmpty(currentConversation.sceneToLoadOnEnd))
            {
                SceneManager.LoadScene(currentConversation.sceneToLoadOnEnd);
            }
            else
            {
                EndDialogue();
            }
            return;
        }

        Line currentLine = lines.Dequeue();
        if (nameText != null) nameText.text = currentLine.speaker;

        if (currentLine.newBackground != null && gameBackgroundDisplay != null)
        {
            gameBackgroundDisplay.sprite = currentLine.newBackground;
        }

        activeLineText = (currentLine.panelType == PanelType.Panel1) ? lineText : lineText2;

        if (currentLine.ocultarPanelDialogo)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(currentLine.panelType == PanelType.Panel1);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(currentLine.panelType == PanelType.Panel2);
        }

        fullTextOfCurrentLine = currentLine.text;
        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(fullTextOfCurrentLine));

        if (currentLine.triggerShake)
        {
            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            float duration = currentLine.shakeDuration > 0 ? currentLine.shakeDuration : 0.5f;
            float intensity = currentLine.shakeIntensity > 0 ? currentLine.shakeIntensity : 10f;
            activeShakeCoroutine = StartCoroutine(ScreenShakeRoutine(duration, intensity));
        }

        HandleAudio(currentLine);
        HandleCharacterImage(currentLine);

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);

        if (currentLine.choices != null && currentLine.choices.Count > 0)
        {
            SetupChoices(currentLine.choices, currentLine.choicePanelType);
        }
        else
        {
            awaitingChoice = false;
        }

        if (currentLine.dispararCarruselPaneles)
        {
            BridgePanelesDialogo bridge = UnityEngine.Object.FindAnyObjectByType<BridgePanelesDialogo>(FindObjectsInactive.Include);
            if (bridge != null)
            {
                bridge.ActivarPanelesVN();
            }
        }
    }

    private void HandleAudio(Line currentLine)
    {
        StopDialogueAudio(); 
        if (currentLine.playSound && currentLine.soundClip != null && dialogueAudioSource != null)
        {
            dialogueAudioSource.clip = currentLine.soundClip;
            dialogueAudioSource.Play();
        }

        StopSFXAudio();
        if (currentLine.playSFX && currentLine.sfxClip != null && sfxAudioSource != null)
        {
            sfxAudioSource.clip = currentLine.sfxClip;
            sfxAudioSource.Play();

            if (currentLine.sfxDuration > 0f)
            {
                sfxDurationCoroutine = StartCoroutine(CutSFXAfterDelay(currentLine.sfxDuration));
            }
        }
    }

    private void StopDialogueAudio()
    {
        if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
        {
            dialogueAudioSource.Stop();
        }
    }

    private void StopSFXAudio()
    {
        if (sfxAudioSource != null && sfxAudioSource.isPlaying)
        {
            sfxAudioSource.Stop();
        }
        if (sfxDurationCoroutine != null)
        {
            StopCoroutine(sfxDurationCoroutine);
            sfxDurationCoroutine = null;
        }
    }

    private IEnumerator CutSFXAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxAudioSource != null && sfxAudioSource.isPlaying)
        {
            sfxAudioSource.Stop();
        }
        sfxDurationCoroutine = null;
    }

    private IEnumerator TypeText(string textToType)
    {
        if (activeLineText == null)
        {
            typingCoroutine = null;
            yield break;
        }

        activeLineText.text = "";
        char[] letters = textToType.ToCharArray();
        
        for (int i = 0; i < letters.Length; i++)
        {
            activeLineText.text += letters[i];
            if (textSpeed > 0)
                yield return new WaitForSeconds(textSpeed);
        }
        
        StopDialogueAudio();
        typingCoroutine = null; 
    }

    private IEnumerator ScreenShakeRoutine(float duration, float intensity)
    {
        if (objectToShake == null) yield break;

        Vector3 originalPosition = objectToShake.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
            float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
            objectToShake.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        objectToShake.localPosition = originalPosition;
        activeShakeCoroutine = null;
    }

    private void HandleCharacterImage(Line currentLine)
    {
        if (characterImageDisplay == null) return;

        if (currentLine.showImage && currentLine.characterImage != null)
        {
            characterImageDisplay.gameObject.SetActive(true);
            characterImageDisplay.sprite = currentLine.characterImage;
            characterImageDisplay.preserveAspect = true;

            if (characterImageRect != null)
            {
                if (currentLine.characterImageSize != Vector2.zero)
                {
                    characterImageRect.sizeDelta = currentLine.characterImageSize;
                }
                else
                {
                    characterImageRect.sizeDelta = defaultCharacterSize;
                }

                switch (currentLine.imagePosition)
                {
                    case ImagePosition.Izquierda: characterImageRect.anchoredPosition = positionLeft; break;
                    case ImagePosition.Centro: characterImageRect.anchoredPosition = positionCenter; break;
                    case ImagePosition.Derecha: characterImageRect.anchoredPosition = positionRight; break;
                }
            }
        }
        else
        {
            characterImageDisplay.gameObject.SetActive(false);
        }
    }

    // AHORA SOLO CONFIGURA HASTA UN MÁXIMO DE 2 BOTONES
    void SetupChoices(List<Choice> choices, PanelType panelType)
    {
        awaitingChoice = true;
        ChoicePanelGroup activeGroup = (panelType == PanelType.Panel1) ? choicesPanel1 : choicesPanel2;
        activeGroup.SetActive(true);

        if (activeGroup.buttonA != null) activeGroup.buttonA.gameObject.SetActive(false);
        if (activeGroup.buttonB != null) activeGroup.buttonB.gameObject.SetActive(false);

        if (activeGroup.buttonA != null) activeGroup.buttonA.onClick.RemoveAllListeners();
        if (activeGroup.buttonB != null) activeGroup.buttonB.onClick.RemoveAllListeners();

        // Primera Opción
        if (choices.Count > 0 && activeGroup.buttonA != null)
        {
            activeGroup.buttonA.gameObject.SetActive(true);
            if (activeGroup.textA != null) activeGroup.textA.text = choices[0].buttonText;
            if (activeGroup.buttonA.image != null) activeGroup.buttonA.image.sprite = choices[0].buttonBackground != null ? choices[0].buttonBackground : activeGroup.defaultSpriteA;
            Choice choiceA = choices[0];
            activeGroup.buttonA.onClick.AddListener(() => OnChoiceSelected(choiceA));
        }

        // Segunda Opción
        if (choices.Count > 1 && activeGroup.buttonB != null)
        {
            activeGroup.buttonB.gameObject.SetActive(true);
            if (activeGroup.textB != null) activeGroup.textB.text = choices[1].buttonText;
            if (activeGroup.buttonB.image != null) activeGroup.buttonB.image.sprite = choices[1].buttonBackground != null ? choices[1].buttonBackground : activeGroup.defaultSpriteB;
            Choice choiceB = choices[1];
            activeGroup.buttonB.onClick.AddListener(() => OnChoiceSelected(choiceB));
        }
    }

    void OnChoiceSelected(Choice selectedChoice)
    {
        awaitingChoice = false;
        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (selectedChoice.changeScene && !string.IsNullOrEmpty(selectedChoice.sceneToLoad))
        {
            SceneManager.LoadScene(selectedChoice.sceneToLoad);
            return;
        }

        if (selectedChoice.nextDialogue != null) StartDialogueSequence(selectedChoice.nextDialogue);
        else EndDialogue();
    }

    void EndDialogue()
    {
        dialogueActive = false;
        awaitingChoice = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        StopDialogueAudio();
        StopSFXAudio();

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);
        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);
        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
    }
}