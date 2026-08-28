using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; 

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

    [Header("Control de Avance")]
    public bool permitirAvanceManual = true; 

    [Header("Control de UI Global")]
    [SerializeField] private List<Button> botonesSiemprePermitidos = new List<Button>();
    [SerializeField] private GameObject panelInventario;

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

    [Header("Posiciones Predefinidas")]
    [SerializeField] private Vector2 positionLeft = new Vector2(-350, -90);
    [SerializeField] private Vector2 positionCenter = new Vector2(0, -90);
    [SerializeField] private Vector2 positionRight = new Vector2(350, -90);

    [Header("Efectos")]
    [SerializeField] private Transform objectToShake;
    private Coroutine activeShakeCoroutine;
    [SerializeField] private AudioSource dialogueAudioSource;
    [SerializeField] private AudioSource sfxAudioSource; 
    private Coroutine sfxDurationCoroutine; 

    [Header("Paneles de Decisiones Separados")]
    [SerializeField] private ChoicePanelGroup choicesPanel1;
    [SerializeField] private ChoicePanelGroup choicesPanel2;

    public bool dialogueActive { get; private set; }
    private bool awaitingChoice;
    private Queue<Line> lines = new Queue<Line>();
    private DialogueScript currentConversation;
    private Vector2 defaultCharacterSize, defaultCharacterSize2, defaultCharacterSize3;
    private HashSet<string> playerFlags = new HashSet<string>();
    
    private Action onDialogueEndCallback;
    public static bool BloquearAvancePorCarrusel { get; set; } = false;

    void Start()
    {
        choicesPanel1.InitializeDefaults();
        choicesPanel2.InitializeDefaults();

        DesactivarTodosLosPaneles();
        LimpiarTodosLosTextos();

        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);

        if (characterImageRect != null) defaultCharacterSize = characterImageRect.sizeDelta;
        if (characterImageRect2 != null) defaultCharacterSize2 = characterImageRect2.sizeDelta;
        if (characterImageRect3 != null) defaultCharacterSize3 = characterImageRect3.sizeDelta;

        if (dialogueAudioSource == null) dialogueAudioSource = gameObject.AddComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (panelInventario == null)
            panelInventario = GameObject.Find("PanelMochila") ?? GameObject.Find("MochilaPanel") ?? GameObject.Find("InventoryPanel") ?? GameObject.Find("Mochila");

        bool inventarioAbierto = panelInventario != null && panelInventario.activeInHierarchy;
        ActualizarRaycastOpciones(!inventarioAbierto);

        bool presionoEspacio = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) presionoEspacio = true;
        #if !ENABLE_INPUT_SYSTEM_ONLY
        if (Input.GetKeyDown(KeyCode.Space)) presionoEspacio = true;
        #endif

        if (presionoEspacio && !inventarioAbierto && permitirAvanceManual) 
        {
            AdvanceDialogue();
        }
    }

    private void ActualizarRaycastOpciones(bool permitir)
    {
        AplicarCanvasGroup(choicesPanel1.choicePanel, permitir);
        AplicarCanvasGroup(choicesPanel2.choicePanel, permitir);
    }

    private void AplicarCanvasGroup(GameObject panel, bool permitir)
    {
        if (panel == null) return;
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.interactable = permitir; cg.blocksRaycasts = permitir;
    }

    public void BloquearBotonesEscena(bool bloquear, List<Button> excepcionesAdicionales = null)
    {
        Button[] todosLosBotones = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        foreach (Button btn in todosLosBotones)
        {
            if (btn == null || EsBotonDelDialogueManager(btn) || btn.GetComponentInParent<ItemPickup>() != null) continue;
            if (botonesSiemprePermitidos != null && botonesSiemprePermitidos.Contains(btn))
            {
                btn.interactable = true; btn.transform.SetAsLastSibling(); continue;
            }
            if (excepcionesAdicionales != null && excepcionesAdicionales.Contains(btn))
            {
                btn.interactable = true; btn.transform.SetAsLastSibling(); continue;
            }
            btn.interactable = !bloquear;
        }
    }

    private bool EsBotonDelDialogueManager(Button btn)
    {
        return btn == choicesPanel1.buttonA || btn == choicesPanel1.buttonB ||
               btn == choicesPanel2.buttonA || btn == choicesPanel2.buttonB;
    }

    public void SetFlag(string flagName)
    {
        if (!string.IsNullOrEmpty(flagName) && !playerFlags.Contains(flagName)) playerFlags.Add(flagName);
    }
    public bool HasFlag(string flagName) => !string.IsNullOrEmpty(flagName) && playerFlags.Contains(flagName);
    public void ClearFlags() => playerFlags.Clear();

    public void AdvanceDialogue()
    {
        if (BloquearAvancePorCarrusel || (panelInventario != null && panelInventario.activeInHierarchy)) return;
        if (!permitirAvanceManual && !awaitingChoice) return;

        if (dialogueActive && !awaitingChoice)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
                if (activeLineText != null) { activeLineText.text = fullTextOfCurrentLine; RestablecerAlphaTexto(activeLineText); }
                StopDialogueAudio();
            }
            else ProduceNextLine();
        }
    }

    public void GetConversation(DialogueScript dialogue, Action onComplete = null)
    {
        if (dialogue == null || dialogue.conversationLines == null || dialogue.conversationLines.Count == 0) return;
        onDialogueEndCallback = onComplete;
        StartDialogueSequence(dialogue);
    }

    private void StartDialogueSequence(DialogueScript dialogue, int startAtLineIndex = 0)
    {
        currentConversation = dialogue;
        lines.Clear();
        int startIndex = Mathf.Clamp(startAtLineIndex, 0, Mathf.Max(0, dialogue.conversationLines.Count - 1));
        for (int i = startIndex; i < dialogue.conversationLines.Count; i++) lines.Enqueue(dialogue.conversationLines[i]);

        dialogueActive = true; awaitingChoice = false; DesactivarOpciones();
        ProduceNextLine();
    }

    void ProduceNextLine()
    {
        if (lines.Count == 0)
        {
            if (currentConversation != null)
            {
                if (!string.IsNullOrEmpty(currentConversation.flagRequeridaParaSiguiente) && HasFlag(currentConversation.flagRequeridaParaSiguiente) && currentConversation.conversationSiTieneFlag != null)
                { StartDialogueSequence(currentConversation.conversationSiTieneFlag); return; }
                if (currentConversation.nextConversation != null) { StartDialogueSequence(currentConversation.nextConversation); return; }
                if (currentConversation.changeSceneOnEnd && !string.IsNullOrEmpty(currentConversation.sceneToLoadOnEnd))
                { SceneManager.LoadScene(currentConversation.sceneToLoadOnEnd); return; }
            }
            EndDialogue(); return;
        }

        LimpiarTodosLosTextos();
        Line currentLine = lines.Dequeue();

        BloquearBotonesEscena(currentLine.bloquearOtrosBotones);
        if (currentLine.objetoAActivar != null) currentLine.objetoAActivar.SetActive(true);
        if (currentLine.objetoADesactivar != null && currentLine.objetoADesactivar != dialoguePanel3 && (gameBackgroundDisplay == null || currentLine.objetoADesactivar != gameBackgroundDisplay.gameObject)) 
            currentLine.objetoADesactivar.SetActive(false);

        if (nameText != null) nameText.text = currentLine.speaker;
        if (gameBackgroundDisplay != null && currentLine.newBackground != null) 
        { gameBackgroundDisplay.gameObject.SetActive(true); gameBackgroundDisplay.sprite = currentLine.newBackground; }

        switch (currentLine.panelType)
        {
            case PanelType.Panel1: activeLineText = lineText; break;
            case PanelType.Panel2: activeLineText = lineText2; break;
            case PanelType.Panel3: activeLineText = lineText3; break;
        }

        if (currentLine.ocultarPanelDialogo) DesactivarTodosLosPaneles();
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(currentLine.panelType == PanelType.Panel1);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(currentLine.panelType == PanelType.Panel2);
            if (dialoguePanel3 != null) dialoguePanel3.SetActive(currentLine.panelType == PanelType.Panel3);
            if (activeLineText != null) activeLineText.gameObject.SetActive(true);
        }

        fullTextOfCurrentLine = currentLine.text;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        if (currentLine.usarTransicionTexto) typingCoroutine = StartCoroutine(FadeInText(fullTextOfCurrentLine, currentLine.duracionTransicion > 0f ? currentLine.duracionTransicion : 1f));
        else typingCoroutine = StartCoroutine(TypeText(fullTextOfCurrentLine));

        if (currentLine.triggerShake)
        {
            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            activeShakeCoroutine = StartCoroutine(ScreenShakeRoutine(currentLine.shakeDuration > 0 ? currentLine.shakeDuration : 0.5f, currentLine.shakeIntensity > 0 ? currentLine.shakeIntensity : 10f));
        }

        HandleAudio(currentLine); HandleAllCharacterImages(currentLine); DesactivarOpciones();

        if (currentLine.choices != null && currentLine.choices.Count > 0) SetupChoices(currentLine.choices, currentLine.choicePanelType, currentLine.bloquearOtrosBotones);
        else awaitingChoice = false;

        if (currentLine.dispararCarruselPaneles)
        {
            BridgePanelesDialogo bridge = UnityEngine.Object.FindAnyObjectByType<BridgePanelesDialogo>(FindObjectsInactive.Include);
            if (bridge != null) bridge.ActivarPanelesVN();
        }
    }

    private void HandleAudio(Line currentLine)
    {
        StopDialogueAudio(); 
        if (currentLine.playSound && currentLine.soundClip != null && dialogueAudioSource != null) { dialogueAudioSource.clip = currentLine.soundClip; dialogueAudioSource.Play(); }
        StopSFXAudio();
        if (currentLine.playSFX && currentLine.sfxClip != null && sfxAudioSource != null) { sfxAudioSource.clip = currentLine.sfxClip; sfxAudioSource.Play(); if (currentLine.sfxDuration > 0f) sfxDurationCoroutine = StartCoroutine(CutSFXAfterDelay(currentLine.sfxDuration)); }
    }
    private void StopDialogueAudio() { if (dialogueAudioSource != null && dialogueAudioSource.isPlaying) dialogueAudioSource.Stop(); }
    private void StopSFXAudio() { if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop(); if (sfxDurationCoroutine != null) { StopCoroutine(sfxDurationCoroutine); sfxDurationCoroutine = null; } }
    private IEnumerator CutSFXAfterDelay(float delay) { yield return new WaitForSeconds(delay); if (sfxAudioSource != null) sfxAudioSource.Stop(); sfxDurationCoroutine = null; }

    private IEnumerator TypeText(string textToType)
    {
        if (activeLineText == null) yield break;
        RestablecerAlphaTexto(activeLineText); activeLineText.text = "";
        foreach (char letter in textToType.ToCharArray()) { activeLineText.text += letter; if (textSpeed > 0) yield return new WaitForSeconds(textSpeed); }
        StopDialogueAudio(); typingCoroutine = null; 
    }
    private IEnumerator FadeInText(string textToType, float duration)
    {
        if (activeLineText == null) yield break;
        activeLineText.text = textToType; Color c = activeLineText.color; float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; activeLineText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(elapsed / duration)); yield return null; }
        RestablecerAlphaTexto(activeLineText); StopDialogueAudio(); typingCoroutine = null;
    }
    private void RestablecerAlphaTexto(TextMeshProUGUI tmpText) { if (tmpText != null) tmpText.color = new Color(tmpText.color.r, tmpText.color.g, tmpText.color.b, 1f); }

    private IEnumerator ScreenShakeRoutine(float duration, float intensity)
    {
        if (objectToShake == null) yield break;
        Vector3 orig = objectToShake.localPosition; float elapsed = 0.0f;
        while (elapsed < duration) { objectToShake.localPosition = orig + (Vector3)(UnityEngine.Random.insideUnitCircle * intensity); elapsed += Time.deltaTime; yield return null; }
        objectToShake.localPosition = orig; activeShakeCoroutine = null;
    }

    private void HandleAllCharacterImages(Line currentLine)
    {
        ProcessSingleCharacter(currentLine.showImage, currentLine.characterImage, currentLine.imagePosition, currentLine.characterImagePosition, currentLine.characterImageSize, characterImageDisplay, characterImageRect, defaultCharacterSize);
        ProcessSingleCharacter(currentLine.showImage2, currentLine.characterImage2, currentLine.imagePosition2, currentLine.characterImagePosition2, currentLine.characterImageSize2, characterImageDisplay2, characterImageRect2, defaultCharacterSize2);
        ProcessSingleCharacter(currentLine.showImage3, currentLine.characterImage3, currentLine.imagePosition3, currentLine.characterImagePosition3, currentLine.characterImageSize3, characterImageDisplay3, characterImageRect3, defaultCharacterSize3);
    }
    private void ProcessSingleCharacter(bool show, Sprite sprite, ImagePosition pos, Vector2 customPos, Vector2 customSize, Image display, RectTransform rect, Vector2 defSize)
    {
        if (display == null) return;
        if (show && sprite != null) {
            display.gameObject.SetActive(true); display.sprite = sprite; display.preserveAspect = true;
            if (rect != null) { rect.sizeDelta = (customSize != Vector2.zero) ? customSize : defSize;
                if (pos == ImagePosition.Personalizada || customPos != Vector2.zero) rect.anchoredPosition = customPos;
                else switch (pos) { case ImagePosition.Izquierda: rect.anchoredPosition = positionLeft; break; case ImagePosition.Centro: rect.anchoredPosition = positionCenter; break; case ImagePosition.Derecha: rect.anchoredPosition = positionRight; break; }
            }
        } else display.gameObject.SetActive(false);
    }

    private void DesactivarOpciones() { choicesPanel1.SetActive(false); choicesPanel2.SetActive(false); }

    void SetupChoices(List<Choice> choices, PanelType panelType, bool bloquearOtros)
    {
        awaitingChoice = true; BloquearBotonesEscena(bloquearOtros);
        ChoicePanelGroup activeGroup = panelType == PanelType.Panel2 ? choicesPanel2 : choicesPanel1;
        activeGroup.SetActive(true);
        if (activeGroup.buttonA != null) { activeGroup.buttonA.gameObject.SetActive(false); activeGroup.buttonA.onClick.RemoveAllListeners(); }
        if (activeGroup.buttonB != null) { activeGroup.buttonB.gameObject.SetActive(false); activeGroup.buttonB.onClick.RemoveAllListeners(); }
        if (choices.Count > 0 && activeGroup.buttonA != null) SetupChoiceButton(activeGroup.buttonA, activeGroup.textA, choices[0], activeGroup.defaultSpriteA);
        if (choices.Count > 1 && activeGroup.buttonB != null) SetupChoiceButton(activeGroup.buttonB, activeGroup.textB, choices[1], activeGroup.defaultSpriteB);
    }

    private void SetupChoiceButton(Button btn, TextMeshProUGUI txt, Choice choice, Sprite defSprite)
    {
        btn.gameObject.SetActive(true); btn.interactable = true;
        if (txt != null) txt.text = choice.buttonText;
        if (btn.image != null) btn.image.sprite = choice.buttonBackground != null ? choice.buttonBackground : defSprite;
        btn.onClick.AddListener(() => OnChoiceSelected(choice));
    }

    void OnChoiceSelected(Choice selectedChoice)
    {
        if (panelInventario != null && panelInventario.activeInHierarchy) return;
        awaitingChoice = false; DesactivarOpciones();
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        // Descontar vida si la opción está configurada como incorrecta
        if (selectedChoice.restarVida) 
        { 
            TrialManager trial = UnityEngine.Object.FindAnyObjectByType<TrialManager>(); 
            if (trial != null) 
            {
                trial.RestarVidaDirecta();

                // Detiene la secuencia si el jugador llega a 0 vidas
                if (trial.EstaMuerto)
                {
                    return; 
                }
            }
        }

        if (!string.IsNullOrEmpty(selectedChoice.guardarFlag)) SetFlag(selectedChoice.guardarFlag);
        if (selectedChoice.changeScene && !string.IsNullOrEmpty(selectedChoice.sceneToLoad)) { SceneManager.LoadScene(selectedChoice.sceneToLoad); return; }

        DialogueScript targetDialogue = selectedChoice.nextDialogue;
        if (!string.IsNullOrEmpty(selectedChoice.flagRequerida) && HasFlag(selectedChoice.flagRequerida) && selectedChoice.dialogoSiTieneFlag != null) targetDialogue = selectedChoice.dialogoSiTieneFlag;

        int startLine = selectedChoice.reiniciarEnLineaEspecifica ? selectedChoice.lineaDestino : 0;
        if (targetDialogue != null) StartDialogueSequence(targetDialogue, startLine);
        else if (selectedChoice.reiniciarEnLineaEspecifica && currentConversation != null) StartDialogueSequence(currentConversation, startLine);
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
        RestablecerAlphaTexto(lineText); RestablecerAlphaTexto(lineText2); RestablecerAlphaTexto(lineText3);
        if (lineText != null) lineText.text = ""; if (lineText2 != null) lineText2.text = ""; if (lineText3 != null) lineText3.text = ""; if (nameText != null) nameText.text = "";
    }
    public void ForceStopDialogue()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StopDialogueAudio(); StopSFXAudio(); dialogueActive = false; onDialogueEndCallback = null;
    }
    void EndDialogue()
    {
        dialogueActive = false; awaitingChoice = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        BloquearBotonesEscena(false); StopDialogueAudio(); StopSFXAudio();
        DesactivarTodosLosPaneles(); LimpiarTodosLosTextos(); DesactivarOpciones();
        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);
        Action callback = onDialogueEndCallback; onDialogueEndCallback = null; callback?.Invoke();
    }
}