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
    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
    [SerializeField] private TextMeshProUGUI lineText3; 
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject dialoguePanel2;
    [SerializeField] private GameObject dialoguePanel3; 
    [SerializeField] private Image gameBackgroundDisplay;

    [Header("Configuración de Texto Animado")]
    [SerializeField] private float textSpeed = 0.03f;
    private Coroutine typingCoroutine;
    private string fullTextOfCurrentLine;
    private TextMeshProUGUI activeLineText;

    [Header("Imágenes de Personaje")]
    [SerializeField] private Image characterImageDisplay;
    [SerializeField] private RectTransform characterImageRect;

    [SerializeField] private Image characterImageDisplay2;
    [SerializeField] private RectTransform characterImageRect2;

    [SerializeField] private Image characterImageDisplay3;
    [SerializeField] private RectTransform characterImageRect3;

    [Header("Posiciones Predefinidas de Personajes")]
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
    [SerializeField] private ChoicePanelGroup choicesPanel3;

    private bool dialogueActive;
    private bool awaitingChoice;
    private Queue<Line> lines = new Queue<Line>();

    private DialogueScript currentConversation;
    private Vector2 defaultCharacterSize;
    private Vector2 defaultCharacterSize2;
    private Vector2 defaultCharacterSize3;

    public static bool BloquearAvancePorCarrusel { get; set; } = false;

    void Start()
    {
        choicesPanel1.InitializeDefaults();
        choicesPanel2.InitializeDefaults();
        choicesPanel3.InitializeDefaults();

        DesactivarTodosLosPaneles();
        LimpiarTodosLosTextos();

        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);

        if (characterImageRect != null) defaultCharacterSize = characterImageRect.sizeDelta;
        if (characterImageRect2 != null) defaultCharacterSize2 = characterImageRect2.sizeDelta;
        if (characterImageRect3 != null) defaultCharacterSize3 = characterImageRect3.sizeDelta;

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);
        choicesPanel3.SetActive(false);

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

                if (activeLineText != null)
                {
                    activeLineText.text = fullTextOfCurrentLine;
                    RestablecerAlphaTexto(activeLineText);
                }

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
        choicesPanel3.SetActive(false);

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

        LimpiarTodosLosTextos();

        Line currentLine = lines.Dequeue();
        if (nameText != null) nameText.text = currentLine.speaker;

        if (currentLine.newBackground != null && gameBackgroundDisplay != null)
        {
            gameBackgroundDisplay.sprite = currentLine.newBackground;
        }

        // 1. Asignar componente de texto activo
        switch (currentLine.panelType)
        {
            case PanelType.Panel1: activeLineText = lineText; break;
            case PanelType.Panel2: activeLineText = lineText2; break;
            case PanelType.Panel3: activeLineText = lineText3; break;
        }

        // 2. Control de Paneles y Activación Explícita del GameObject del Texto
        if (currentLine.ocultarPanelDialogo)
        {
            DesactivarTodosLosPaneles();
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(currentLine.panelType == PanelType.Panel1);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(currentLine.panelType == PanelType.Panel2);
            if (dialoguePanel3 != null) dialoguePanel3.SetActive(currentLine.panelType == PanelType.Panel3);

            // Asegurar la activación explícita del GameObject del texto asignado
            if (activeLineText != null && !activeLineText.gameObject.activeSelf)
            {
                activeLineText.gameObject.SetActive(true);
            }
        }

        fullTextOfCurrentLine = currentLine.text;
        
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (currentLine.usarTransicionTexto)
        {
            float duration = currentLine.duracionTransicion > 0f ? currentLine.duracionTransicion : 1f;
            typingCoroutine = StartCoroutine(FadeInText(fullTextOfCurrentLine, duration));
        }
        else
        {
            typingCoroutine = StartCoroutine(TypeText(fullTextOfCurrentLine));
        }

        if (currentLine.triggerShake)
        {
            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            float duration = currentLine.shakeDuration > 0 ? currentLine.shakeDuration : 0.5f;
            float intensity = currentLine.shakeIntensity > 0 ? currentLine.shakeIntensity : 10f;
            activeShakeCoroutine = StartCoroutine(ScreenShakeRoutine(duration, intensity));
        }

        HandleAudio(currentLine);
        HandleAllCharacterImages(currentLine);

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);
        choicesPanel3.SetActive(false);

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

        RestablecerAlphaTexto(activeLineText);
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

    private IEnumerator FadeInText(string textToType, float duration)
    {
        if (activeLineText == null)
        {
            typingCoroutine = null;
            yield break;
        }

        activeLineText.text = textToType;
        
        Color originalColor = activeLineText.color;
        activeLineText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            activeLineText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        RestablecerAlphaTexto(activeLineText);
        StopDialogueAudio();
        typingCoroutine = null;
    }

    private void RestablecerAlphaTexto(TextMeshProUGUI tmpText)
    {
        if (tmpText != null)
        {
            Color c = tmpText.color;
            tmpText.color = new Color(c.r, c.g, c.b, 1f);
        }
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

    private void HandleAllCharacterImages(Line currentLine)
    {
        ProcessSingleCharacter(currentLine.showImage, currentLine.characterImage, currentLine.imagePosition, currentLine.characterImagePosition, currentLine.characterImageSize, characterImageDisplay, characterImageRect, defaultCharacterSize);
        ProcessSingleCharacter(currentLine.showImage2, currentLine.characterImage2, currentLine.imagePosition2, currentLine.characterImagePosition2, currentLine.characterImageSize2, characterImageDisplay2, characterImageRect2, defaultCharacterSize2);
        ProcessSingleCharacter(currentLine.showImage3, currentLine.characterImage3, currentLine.imagePosition3, currentLine.characterImagePosition3, currentLine.characterImageSize3, characterImageDisplay3, characterImageRect3, defaultCharacterSize3);
    }

    private void ProcessSingleCharacter(bool show, Sprite sprite, ImagePosition position, Vector2 customPosition, Vector2 customSize, Image display, RectTransform rect, Vector2 defaultSize)
    {
        if (display == null) return;

        if (show && sprite != null)
        {
            display.gameObject.SetActive(true);
            display.sprite = sprite;
            display.preserveAspect = true;

            if (rect != null)
            {
                rect.sizeDelta = (customSize != Vector2.zero) ? customSize : defaultSize;

                if (position == ImagePosition.Personalizada || customPosition != Vector2.zero)
                {
                    rect.anchoredPosition = customPosition;
                }
                else
                {
                    switch (position)
                    {
                        case ImagePosition.Izquierda: rect.anchoredPosition = positionLeft; break;
                        case ImagePosition.Centro: rect.anchoredPosition = positionCenter; break;
                        case ImagePosition.Derecha: rect.anchoredPosition = positionRight; break;
                    }
                }
            }
        }
        else
        {
            display.gameObject.SetActive(false);
        }
    }

    void SetupChoices(List<Choice> choices, PanelType panelType)
    {
        awaitingChoice = true;
        ChoicePanelGroup activeGroup = choicesPanel1;
        if (panelType == PanelType.Panel2) activeGroup = choicesPanel2;
        else if (panelType == PanelType.Panel3) activeGroup = choicesPanel3;

        activeGroup.SetActive(true);

        if (activeGroup.buttonA != null) activeGroup.buttonA.gameObject.SetActive(false);
        if (activeGroup.buttonB != null) activeGroup.buttonB.gameObject.SetActive(false);

        if (activeGroup.buttonA != null) activeGroup.buttonA.onClick.RemoveAllListeners();
        if (activeGroup.buttonB != null) activeGroup.buttonB.onClick.RemoveAllListeners();

        if (choices.Count > 0 && activeGroup.buttonA != null)
        {
            activeGroup.buttonA.gameObject.SetActive(true);
            if (activeGroup.textA != null) activeGroup.textA.text = choices[0].buttonText;
            if (activeGroup.buttonA.image != null) activeGroup.buttonA.image.sprite = choices[0].buttonBackground != null ? choices[0].buttonBackground : activeGroup.defaultSpriteA;
            Choice choiceA = choices[0];
            activeGroup.buttonA.onClick.AddListener(() => OnChoiceSelected(choiceA));
        }

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
        choicesPanel3.SetActive(false);

        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (selectedChoice.changeScene && !string.IsNullOrEmpty(selectedChoice.sceneToLoad))
        {
            SceneManager.LoadScene(selectedChoice.sceneToLoad);
            return;
        }

        if (selectedChoice.nextDialogue != null) StartDialogueSequence(selectedChoice.nextDialogue);
        else EndDialogue();
    }

    public void DesactivarTodosLosPaneles()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);
        if (dialoguePanel3 != null) dialoguePanel3.SetActive(false);
    }

    public void LimpiarTodosLosTextos()
    {
        RestablecerAlphaTexto(lineText);
        RestablecerAlphaTexto(lineText2);
        RestablecerAlphaTexto(lineText3);

        if (lineText != null) lineText.text = "";
        if (lineText2 != null) lineText2.text = "";
        if (lineText3 != null) lineText3.text = "";
        if (nameText != null) nameText.text = "";
    }

    void EndDialogue()
    {
        dialogueActive = false;
        awaitingChoice = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        
        StopDialogueAudio();
        StopSFXAudio();

        DesactivarTodosLosPaneles();
        LimpiarTodosLosTextos();

        choicesPanel1.SetActive(false);
        choicesPanel2.SetActive(false);
        choicesPanel3.SetActive(false);

        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);
    }
}