using System.Collections.Generic;
using TMPro;
using TMPEffects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Unity.Cinemachine;
using AYellowpaper.SerializedCollections;
using System.Collections;
using TMPEffects.Components;
using UnityEngine.Localization.SmartFormat.Utilities;

[System.Serializable] struct DialogueEvent
{
    [Tooltip("Event that gets triggered when this cutscene key gets triggered. Use for custom logic.")]
    [SerializeField] public UnityEvent OnEventTriggered;
    
    [Tooltip("Camera to switch to. Leave empty to disable.")]
    [SerializeField] public CinemachineCamera camera;

    [Tooltip("Animator to animate. Leave empty to disable.")]
    [SerializeField] public Animator animator;

    [Tooltip("Name of the AnimationController trigger. Has to be exact.")]
    [SerializeField] public string trigger;
}

public class DialogueShower : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private InputAction nextDialogAction;
    [Header("Camera")]
    [SerializeField] private CinemachineCamera mainDialogueCamera;
    [SerializedDictionary("Dialogue index", "Camera")] [SerializeField] private SerializedDictionary<int, DialogueEvent> dialogueEvents;
    [Tooltip("Switches to the main cinemachine camera when enabled.")]
    [SerializeField] private bool returnCameraOnDisable = true;
    [Header("Dialogue")]
    [SerializeField] private TableReference tableReference;
    [SerializeField] private bool triggerOnSceneStart;

    [Tooltip("If this is enabled, the dialogue will NOT get enabled when the player enters the hitbox. Instead, call the Enable() function from another script or an event.")]
    [SerializeField] private bool triggerManually;
   

    private StringTable d;
    private bool active = false;
    private bool ended = false;
    private bool started = false;
    private string characterName;

    private TMP_Text characterNameText;
    private TMP_Text dialogueLineUI;
    private TMPWriter lineScrollEffect;
    private GameObject dialogueUIParent;
    private bool canSkipDialogue = false;
    [Header("Events")]
    public UnityEvent OnDialogueStarted = new();
    public UnityEvent OnDialogueEnabled = new();
    public UnityEvent OnDialogueDisabled = new();
    public UnityEvent OnDialogueEnded = new();

    private Dictionary<string, int> codeToInt = new()
    { //this is really fragile. too bad :(
        {"nl", 0},
        {"ger", 1},
        {"en", 2},
        {"bg", 3},
        {"ro", 4},
        {"tr", 5},
    };

    private int count = 0;
    void UpdateDialogueUI()
    {
        d = LocalizationSettings.StringDatabase.GetTable(tableReference);
        characterName = d.GetEntry("name").GetLocalizedString();
        string line = GetCurrentLine();
        dialogueLineUI.text = line;
        Logger.LogDebug($"dialogueLineUI.enabled = {dialogueLineUI.enabled}\nCurrent line:{line}, count = {count}", transform);
        characterNameText.text = characterName;
    }
    IEnumerator Start()
    {
        dialogueUIParent = GameManager.Instance.UIManager.DialogueUIParent;
        dialogueLineUI = GameManager.Instance.UIManager.DialogueLineText;
        characterNameText = GameManager.Instance.UIManager.DialogueCharacterText;
        lineScrollEffect = dialogueLineUI.GetComponent<TMPWriter>();

        lineScrollEffect.OnFinishWriter.AddListener(WriterFinished);

        UpdateDialogueUI();

        if (triggerOnSceneStart)
        {
            yield return new WaitForSeconds(0.2f);
            //idk what were waiting for lol but it works this way.
            //probably some shit with initializing DDOL or something.
        }
        Enable(triggerOnSceneStart);
    }

    void WriterFinished(TMPWriter arg0)
    {
        if(active)
            canSkipDialogue = true;
    }

    void Update()
    {
        if (!active) return; //dont listen to input if not active
        Logger.LogDebug(nextDialogAction.enabled.ToString(), transform);
        if (d.LocaleIdentifier.Code != LocalizationSettings.SelectedLocale.Identifier.Code)
        { //locale setting changed
            UpdateDialogueUI();
        }
        if (nextDialogAction.WasReleasedThisFrame())
        {
            Logger.LogDebug("Pressed", transform);
            if (canSkipDialogue)
            {
                if(GameManager.Instance.instantDialogue){
                    InstantDialogue();
                    return;
                }

                Logger.LogDebug("Next dialogue", transform);
                dialogueLineUI.text = NextLine();
                characterNameText.text = characterName;
                canSkipDialogue = false;
            }
            else
            {
                Logger.LogDebug("Skipping writer", transform);
                lineScrollEffect.SkipWriter();
            }
        }
    }
    string NextLine()
    {
        count++;
        if (dialogueEvents.ContainsKey(count))
        {
            ExecuteDialogueEvent(dialogueEvents[count]);
        }
        else
        {
            CameraManager.SwitchCamera(mainDialogueCamera);
        }

        string currentLine = GetCurrentLine();

        if (currentLine.Contains('|')) // '|' is the indicator for a new character in dialogue 
        {
            string[] lineStrings = currentLine.Split('|');
            characterName = lineStrings[0];
            currentLine = lineStrings[1];
        }

        return currentLine;
    }

    string GetCurrentLine()
    {
        if(count >= d.Count-1 || count < 0)
        { //THE DIALOGUE ENDED
            FinishDialogue();
            return d.GetEntry((d.Count-2).ToString()).GetLocalizedString();
        }   
        return d.GetEntry(count.ToString()).GetLocalizedString();
    }

    void InstantDialogue()
    {
        int totalLines = d.Count-1; //-1 for the name
        for(int i = 0; i < totalLines; i++)
        {
            NextLine();
        }
        /*foreach(DialogueEvent e in dialogueEvents.Values)
        {
            ExecuteDialogueEvent(e);
        }*/

        FinishDialogue();
        if (returnCameraOnDisable)
        {
            CameraManager.SwitchCameraToMain();
        }
    }

    void FinishDialogue()
    {
        Logger.Log("DIALOGUE ENDED", transform, LogLevel.Info);
        ended = true;
        Enable(false);
        OnDialogueEnded?.Invoke();
        if (returnCameraOnDisable)
        {
            CameraManager.SwitchCameraToMain();
        }
    }

    void ExecuteDialogueEvent(DialogueEvent e)
    {
        if(e.camera != null)
        {
            CameraManager.SwitchCamera(e.camera);
        }
        else
        {
            CameraManager.SwitchCamera(mainDialogueCamera);
        }
        if(e.animator != null && !string.IsNullOrEmpty(e.trigger))
        {
            e.animator.SetTrigger(e.trigger);
        }
        e.OnEventTriggered?.Invoke();
    }

    public void Enable(bool enable)
    {
        if(ended && enable){ return; }//dont re-enable once the dialogue ended.

        if(enable)  nextDialogAction.Enable();
        else nextDialogAction.Disable();

        ToggleDialogueCamera(enable);

        active = enable;
        dialogueLineUI.enabled = enable;
        characterNameText.enabled = enable;
        Logger.Log("Enable:"+enable, transform, LogLevel.Debug);
        dialogueUIParent.SetActive(enable);

        GameManager.Instance.UIManager.SetJoystickVisible(!enable);

        if(enable) OnDialogueEnabled?.Invoke();
        else OnDialogueDisabled?.Invoke();

        if(enable)  UpdateDialogueUI(); //update the dialogue when enabled.

        if (!started && enable)
        {
            started = true;
            OnDialogueStarted.Invoke();
        }
    }
    
    public void ToggleDialogueCamera(bool enable)
    {
        /*if(dialogueEvents.ContainsKey(count) && dialogueEvents[count].camera != null && enable)
        {
            CameraManager.SwitchCamera(dialogueEvents[count].camera);
        }*/
        if (enable && mainDialogueCamera != null)
        {
            CameraManager.SwitchCamera(mainDialogueCamera);
        }
        else if(returnCameraOnDisable && !enable)
        {
            CameraManager.SwitchCameraToMain();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Player" && !triggerManually)
        {
            Enable(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player" && !triggerManually)
        {
            Enable(false);
        }
    }
}