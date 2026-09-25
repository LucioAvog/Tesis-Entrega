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

    [System.Serializable]
    public struct Checkpoint
    {
        public string flag;
        public DialogueScript dialogue;
        public string sceneToLoad;
    }

    [Header("Sistema de Vidas")]
    [SerializeField] private int maxHealth = 3;
    private static int currentHealth = -1;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private DialogueScript dialogoAlMorir;
    private DialogueScript ultimoDialogoDeMuerteEjecutado;
    private bool estaMuerto = false;
    private DialogueScript dialogoMuertePendiente = null;

    public bool EstaMuerto => estaMuerto;
    public int VidaActual => currentHealth;

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
    [SerializeField] private float duracionFadePanel3 = 0.5f;
    [SerializeField] private Image gameBackgroundDisplay;

    [Header("Configuración de Texto Animado")]
    [SerializeField] private float textSpeed = 0.03f;
    private Coroutine typingCoroutine;
    private Coroutine panel3FadeCoroutine;
    private string fullTextOfCurrentLine;
    private TextMeshProUGUI activeLineText;

    [Header("Imagen Destacada / Pantalla")]
    [SerializeField] private Image imagenPantallaDisplay;
    [SerializeField] private RectTransform imagenPantallaRect;

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
    private static readonly Dictionary<string, Checkpoint> checkpointsRegistrados = new Dictionary<string, Checkpoint>();
    private static DialogueScript dialogoCheckpointPendiente;
    private Action onDialogueEndCallback;
    public static bool BloquearAvancePorCarrusel { get; set; } = false;
    private float tiempoBusquedaInventario = 0f;

    [Header("Puntos de Guardado")]
    [SerializeField] private List<Checkpoint> checkpoints = new List<Checkpoint>();

    void Start()
    {
        if (currentHealth == -1) currentHealth = maxHealth;
        estaMuerto = false;
        ultimoDialogoDeMuerteEjecutado = null;
        dialogoMuertePendiente = null;
        UpdateHealthUI();

        choicesPanel1.InitializeDefaults();
        choicesPanel2.InitializeDefaults();

        DesactivarTodosLosPaneles(true);
        LimpiarTodosLosTextos();

        if (imagenPantallaDisplay != null) imagenPantallaDisplay.gameObject.SetActive(false);
        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);

        if (characterImageRect != null) defaultCharacterSize = characterImageRect.sizeDelta;
        if (characterImageRect2 != null) defaultCharacterSize2 = characterImageRect2.sizeDelta;
        if (characterImageRect3 != null) defaultCharacterSize3 = characterImageRect3.sizeDelta;

        if (dialogueAudioSource == null) dialogueAudioSource = gameObject.AddComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (!string.IsNullOrEmpty(checkpoint.flag))
            {
                checkpointsRegistrados[checkpoint.flag] = checkpoint;
            }
        }

        if (dialogoCheckpointPendiente != null)
        {
            DialogueScript checkpointDialogue = dialogoCheckpointPendiente;
            dialogoCheckpointPendiente = null;
            GetConversation(checkpointDialogue);
        }
    }

    void Update()
    {
        // Optimización de búsqueda de inventario con temporizador
        if (panelInventario == null && Time.time > tiempoBusquedaInventario)
        {
            panelInventario = GameObject.Find("PanelMochila") ?? GameObject.Find("MochilaPanel") ?? GameObject.Find("InventoryPanel") ?? GameObject.Find("Mochila");
            tiempoBusquedaInventario = Time.time + 1f; 
        }

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

    public void UpdateHealthUI()
    {
        // Optimización: Interpolación de Strings ($"")
        if (healthText != null) healthText.text = $"Vida: {currentHealth}/{maxHealth}";
    }

    public void RestarVida(int cantidad = 1, DialogueScript dialogoMuertePersonalizado = null, bool posponerMuerte = false)
    {
        if (estaMuerto) return;
        currentHealth = Mathf.Clamp(currentHealth - cantidad, 0, maxHealth);
        UpdateHealthUI();

        if (currentHealth == 0)
        {
            if (posponerMuerte) dialogoMuertePendiente = ObtenerDialogoMuerte(dialogoMuertePersonalizado);
            else GameOver(dialogoMuertePersonalizado);
        }
    }

    public void ModificarVida(int cantidad)
    {
        if (estaMuerto) return;
        currentHealth = Mathf.Clamp(currentHealth + cantidad, 0, maxHealth);
        UpdateHealthUI();
        if (currentHealth == 0) GameOver();
    }

    public void EstablecerVida(int nuevaVida)
    {
        currentHealth = Mathf.Clamp(nuevaVida, 0, maxHealth);
        UpdateHealthUI();
        if (currentHealth == 0 && !estaMuerto) GameOver();
    }

    public void ReiniciarVida()
    {
        currentHealth = maxHealth;
        estaMuerto = false;
        dialogoMuertePendiente = null;
        UpdateHealthUI();
    }

    private void GameOver(DialogueScript dialogoMuerteOverride = null)
    {
        DialogueScript dialogoAIniciar = ObtenerDialogoMuerte(dialogoMuerteOverride);
        ForceStopDialogue();
        estaMuerto = true;
        ultimoDialogoDeMuerteEjecutado = dialogoAIniciar;

        // Ejecuta el diálogo de muerte y, al TERMINAR el diálogo, llama a EjecutarRespawn
        if (dialogoAIniciar != null) 
        {
            GetConversation(dialogoAIniciar, EjecutarRespawn);
        }
        else
        {
            EjecutarRespawn();
        }
    }

    private DialogueScript ObtenerDialogoMuerte(DialogueScript dialogoMuerteOverride = null)
    {
        if (dialogoMuerteOverride != null) return dialogoMuerteOverride;
        if (currentConversation != null && currentConversation.dialogoAlPerderUltimaVida != null)
        {
            return currentConversation.dialogoAlPerderUltimaVida;
        }

        return dialogoAlMorir;
    }

    private void EjecutarRespawn()
    {
        Checkpoint checkpoint = ObtenerUltimoCheckpoint();
        if (!string.IsNullOrEmpty(checkpoint.flag))
        {
            currentHealth = 1;
            estaMuerto = false;
            UpdateHealthUI();

            if (!string.IsNullOrEmpty(checkpoint.sceneToLoad) && checkpoint.sceneToLoad != SceneManager.GetActiveScene().name)
            {
                dialogoCheckpointPendiente = checkpoint.dialogue;
                SceneManager.LoadScene(checkpoint.sceneToLoad);
            }
            else if (checkpoint.dialogue != null)
            {
                GetConversation(checkpoint.dialogue);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            return;
        }

        ReiniciarVida();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private Checkpoint ObtenerUltimoCheckpoint()
    {
        string ultimaFlagCheckpoint = DialogueStateManager.Instance.LastCheckpointFlag;
        if (!string.IsNullOrEmpty(ultimaFlagCheckpoint) &&
            checkpointsRegistrados.TryGetValue(ultimaFlagCheckpoint, out Checkpoint checkpoint) &&
            HasFlag(checkpoint.flag))
        {
            return checkpoint;
        }

        return default;
    }

    private bool EsJugadorMuertoYNoEsDialogoDeMuerte(DialogueScript nuevoDialogo = null)
    {
        if (estaMuerto)
        {
            DialogueScript dialogoAComprobar = nuevoDialogo != null ? nuevoDialogo : currentConversation;
            DialogueScript dialogoDeMuerteValido = ultimoDialogoDeMuerteEjecutado != null ? ultimoDialogoDeMuerteEjecutado : dialogoAlMorir;
            if (dialogoDeMuerteValido == null || dialogoAComprobar != dialogoDeMuerteValido) return true;
        }
        return false;
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
        Button[] todosLosBotones = UnityEngine.Object.FindObjectsByType<Button>();
        foreach (Button btn in todosLosBotones)
        {
            if (btn == null || EsBotonDelDialogueManager(btn) || btn.GetComponentInParent<ItemPickup>() != null) continue;
            
            if ((botonesSiemprePermitidos != null && botonesSiemprePermitidos.Contains(btn)) || 
                (excepcionesAdicionales != null && excepcionesAdicionales.Contains(btn)))
            {
                btn.interactable = true; 
                btn.transform.SetAsLastSibling(); 
                continue;
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
        if (string.IsNullOrEmpty(flagName)) return;
        DialogueStateManager.Instance.SetFlag(flagName);
    }

    public bool HasFlag(string flagName) => DialogueStateManager.Instance.HasFlag(flagName);
    
    public void ClearFlags()
    {
        DialogueStateManager.Instance.ClearFlags();
    }

    public void AdvanceDialogue()
    {
        if (EsJugadorMuertoYNoEsDialogoDeMuerte())
        {
            ForceStopDialogue();
            return;
        }

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

        if (EsJugadorMuertoYNoEsDialogoDeMuerte(dialogue))
        {
            ForceStopDialogue();
            return;
        }

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
        if (EsJugadorMuertoYNoEsDialogoDeMuerte())
        {
            ForceStopDialogue();
            return;
        }

        if (lines.Count == 0)
        {
            if (currentConversation != null)
            {
                if (!string.IsNullOrEmpty(currentConversation.flagRequeridaParaSiguiente) && HasFlag(currentConversation.flagRequeridaParaSiguiente) && currentConversation.conversationSiTieneFlag != null)
                { StartDialogueSequence(currentConversation.conversationSiTieneFlag); return; }
                if (currentConversation.nextConversation != null) { StartDialogueSequence(currentConversation.nextConversation); return; }
                if (currentConversation.changeSceneOnEnd && !string.IsNullOrEmpty(currentConversation.sceneToLoadOnEnd))
                {
                    GuardarFlagAlFinalizarConversacion();
                    SceneManager.LoadScene(currentConversation.sceneToLoadOnEnd);
                    return;
                }
            }
            EndDialogue(); return;
        }

        LimpiarTodosLosTextos();
        Line currentLine = lines.Dequeue();

        BloquearBotonesEscena(currentLine.bloquearOtrosBotones);
        if (currentLine.objetoAActivar != null) currentLine.objetoAActivar.SetActive(true);
        if (currentLine.objetoADesactivar != null && currentLine.objetoADesactivar != dialoguePanel3 && (gameBackgroundDisplay == null || currentLine.objetoADesactivar != gameBackgroundDisplay.gameObject))
            currentLine.objetoADesactivar.SetActive(false);

        if (currentLine.agregarItemAlInventario && currentLine.itemAAgregar != null)
        {
            InventoryManager inventoryManager = InventoryManager.Instance;
            if (inventoryManager == null) inventoryManager = UnityEngine.Object.FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
            if (inventoryManager != null) inventoryManager.AddItem(currentLine.itemAAgregar);
        }

        if (nameText != null) nameText.text = currentLine.speaker;
        if (gameBackgroundDisplay != null && currentLine.newBackground != null)
        { gameBackgroundDisplay.gameObject.SetActive(true); gameBackgroundDisplay.sprite = currentLine.newBackground; }

        switch (currentLine.panelType)
        {
            case PanelType.Panel1: activeLineText = lineText; break;
            case PanelType.Panel2: activeLineText = lineText2; break;
            case PanelType.Panel3: activeLineText = lineText3; break;
            case PanelType.Panel4: activeLineText = null; break;
        }

        if (activeLineText != null)
        {
            activeLineText.text = "";
        }

        if (currentLine.ocultarPanelDialogo || currentLine.panelType == PanelType.Panel4)
        {
            DesactivarTodosLosPaneles();
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(currentLine.panelType == PanelType.Panel1);
            if (dialoguePanel2 != null) dialoguePanel2.SetActive(currentLine.panelType == PanelType.Panel2);

            if (dialoguePanel3 != null)
            {
                if (currentLine.panelType == PanelType.Panel3)
                {
                    CancelarFadePanel3();
                    dialoguePanel3.SetActive(true);
                }
                else if (dialoguePanel3.activeInHierarchy)
                {
                    DesvanecerPanel3();
                }
            }
            if (activeLineText != null) activeLineText.gameObject.SetActive(true);
        }

        fullTextOfCurrentLine = currentLine.text;
        
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (activeLineText != null)
        {
            if (currentLine.usarTransicionTexto) typingCoroutine = StartCoroutine(FadeInText(fullTextOfCurrentLine, currentLine.duracionTransicion > 0f ? currentLine.duracionTransicion : 1f));
            else typingCoroutine = StartCoroutine(TypeText(fullTextOfCurrentLine));
        }

        if (currentLine.triggerShake)
        {
            if (activeShakeCoroutine != null) StopCoroutine(activeShakeCoroutine);
            activeShakeCoroutine = StartCoroutine(ScreenShakeRoutine(currentLine.shakeDuration > 0 ? currentLine.shakeDuration : 0.5f, currentLine.shakeIntensity > 0 ? currentLine.shakeIntensity : 10f));
        }

        HandleAudio(currentLine); 
        HandleAllCharacterImages(currentLine); 
        HandleImagenPantalla(currentLine);
        DesactivarOpciones();

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
        activeLineText.text = "";
        RestablecerAlphaTexto(activeLineText);
        foreach (char letter in textToType.ToCharArray()) 
        { 
            activeLineText.text += letter; 
            if (textSpeed > 0) yield return new WaitForSeconds(textSpeed); 
        }
        StopDialogueAudio(); 
        typingCoroutine = null;
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

    private void HandleImagenPantalla(Line currentLine)
    {
        if (imagenPantallaDisplay == null) return;

        if (currentLine.mostrarImagenPantalla && currentLine.imagenPantalla != null)
        {
            imagenPantallaDisplay.gameObject.SetActive(true);
            imagenPantallaDisplay.sprite = currentLine.imagenPantalla;
            
            if (imagenPantallaRect != null)
            {
                imagenPantallaRect.anchoredPosition = currentLine.posicionImagenPantalla;
                if (currentLine.tamanoImagenPantalla != Vector2.zero)
                {
                    imagenPantallaRect.sizeDelta = currentLine.tamanoImagenPantalla;
                }
            }
        }
        else
        {
            imagenPantallaDisplay.gameObject.SetActive(false);
        }
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
        if (show && sprite != null)
        {
            display.gameObject.SetActive(true); display.sprite = sprite; display.preserveAspect = true;
            if (rect != null)
            {
                rect.sizeDelta = (customSize != Vector2.zero) ? customSize : defSize;
                if (pos == ImagePosition.Personalizada || customPos != Vector2.zero) rect.anchoredPosition = customPos;
                else switch (pos) { case ImagePosition.Izquierda: rect.anchoredPosition = positionLeft; break; case ImagePosition.Centro: rect.anchoredPosition = positionCenter; break; case ImagePosition.Derecha: rect.anchoredPosition = positionRight; break; }
            }
        }
        else display.gameObject.SetActive(false);
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

        if (selectedChoice.restarVida)
        {
            int cantidadARestar = selectedChoice.cantidadVidaARestar > 0 ? selectedChoice.cantidadVidaARestar : 1;
            RestarVida(cantidadARestar, selectedChoice.dialogoAlMorirSiPierde, true);
        }

        if (!string.IsNullOrEmpty(selectedChoice.guardarFlag)) SetFlag(selectedChoice.guardarFlag);
        if (selectedChoice.changeScene && !string.IsNullOrEmpty(selectedChoice.sceneToLoad)) { SceneManager.LoadScene(selectedChoice.sceneToLoad); return; }

        if (dialogoMuertePendiente != null)
        {
            DialogueScript m = dialogoMuertePendiente;
            dialogoMuertePendiente = null;
            GameOver(m);
            return; 
        }

        DialogueScript targetDialogue = selectedChoice.nextDialogue;
        if (!string.IsNullOrEmpty(selectedChoice.flagRequerida) && HasFlag(selectedChoice.flagRequerida) && selectedChoice.dialogoSiTieneFlag != null) targetDialogue = selectedChoice.dialogoSiTieneFlag;

        bool debeReiniciar = selectedChoice.reiniciarEnLineaEspecifica;
        int startLine = debeReiniciar ? selectedChoice.lineaDestino : 0;

        if (targetDialogue != null)
        {
            StartDialogueSequence(targetDialogue, startLine);
        }
        else if (debeReiniciar && currentConversation != null)
        {
            StartDialogueSequence(currentConversation, startLine);
        }
        else
        {
            EndDialogue();
        }
    }

    private void DesvanecerPanel3()
    {
        if (dialoguePanel3 != null && dialoguePanel3.activeInHierarchy)
        {
            CancelarFadePanel3();
            panel3FadeCoroutine = StartCoroutine(FadeOutPanel3Routine(duracionFadePanel3));
        }
    }

    private IEnumerator FadeOutPanel3Routine(float duration)
    {
        CanvasGroup cg = dialoguePanel3.GetComponent<CanvasGroup>();
        if (cg == null) cg = dialoguePanel3.AddComponent<CanvasGroup>();
        float startAlpha = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
        dialoguePanel3.SetActive(false);
        cg.alpha = 1f;
        panel3FadeCoroutine = null;
    }

    private void CancelarFadePanel3()
    {
        if (panel3FadeCoroutine != null)
        {
            StopCoroutine(panel3FadeCoroutine);
            panel3FadeCoroutine = null;
        }
        if (dialoguePanel3 != null)
        {
            CanvasGroup cg = dialoguePanel3.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
    }

    public void DesactivarTodosLosPaneles(bool instantaneo = false)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (dialoguePanel2 != null) dialoguePanel2.SetActive(false);

        if (dialoguePanel3 != null && dialoguePanel3.activeInHierarchy)
        {
            if (instantaneo)
            {
                CancelarFadePanel3();
                dialoguePanel3.SetActive(false);
            }
            else DesvanecerPanel3();
        }
    }

    public void LimpiarTodosLosTextos()
    {
        if (lineText != null) { lineText.text = ""; RestablecerAlphaTexto(lineText); }
        if (lineText2 != null) { lineText2.text = ""; RestablecerAlphaTexto(lineText2); }
        if (lineText3 != null) { lineText3.text = ""; RestablecerAlphaTexto(lineText3); }
        if (nameText != null) nameText.text = "";
    }

    public void ForceStopDialogue()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StopDialogueAudio(); StopSFXAudio(); dialogueActive = false; onDialogueEndCallback = null;
        DesactivarTodosLosPaneles(true);
    }

    void EndDialogue()
    {
        dialogueActive = false; awaitingChoice = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        GuardarFlagAlFinalizarConversacion();
        BloquearBotonesEscena(false); StopDialogueAudio(); StopSFXAudio();
        DesactivarTodosLosPaneles(false); LimpiarTodosLosTextos(); DesactivarOpciones();
        if (imagenPantallaDisplay != null) imagenPantallaDisplay.gameObject.SetActive(false);
        if (characterImageDisplay != null) characterImageDisplay.gameObject.SetActive(false);
        if (characterImageDisplay2 != null) characterImageDisplay2.gameObject.SetActive(false);
        if (characterImageDisplay3 != null) characterImageDisplay3.gameObject.SetActive(false);
        Action callback = onDialogueEndCallback; onDialogueEndCallback = null; callback?.Invoke();
    }

    private void GuardarFlagAlFinalizarConversacion()
    {
        if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.flagAlFinalizar))
        {
            SetFlag(currentConversation.flagAlFinalizar);
        }
    }
}