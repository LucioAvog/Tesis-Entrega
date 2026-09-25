using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueStateManager : MonoBehaviour
{
    private const string FlagsPreferenceKey = "DialogueStateManager.Flags";
    private const string LastCheckpointPreferenceKey = "DialogueStateManager.LastCheckpoint";
    private static DialogueStateManager instance;
    private readonly HashSet<string> flags = new HashSet<string>();

    [Serializable]
    private class FlagCollection
    {
        public List<string> values = new List<string>();
    }

    public static DialogueStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DialogueStateManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(DialogueStateManager));
                    instance = managerObject.AddComponent<DialogueStateManager>();
                }
            }

            return instance;
        }
    }

    public string LastCheckpointFlag { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentManager()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadState();
    }

    public void SetFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName)) return;

        flags.Add(flagName);
        LastCheckpointFlag = flagName;
        SaveState();
    }

    public bool HasFlag(string flagName)
    {
        return !string.IsNullOrEmpty(flagName) && flags.Contains(flagName);
    }

    public void ClearFlags()
    {
        flags.Clear();
        LastCheckpointFlag = null;
        PlayerPrefs.DeleteKey(FlagsPreferenceKey);
        PlayerPrefs.DeleteKey(LastCheckpointPreferenceKey);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        flags.Clear();
        if (PlayerPrefs.HasKey(FlagsPreferenceKey))
        {
            FlagCollection savedFlags = JsonUtility.FromJson<FlagCollection>(PlayerPrefs.GetString(FlagsPreferenceKey));
            if (savedFlags?.values != null)
            {
                foreach (string flag in savedFlags.values)
                {
                    if (!string.IsNullOrWhiteSpace(flag)) flags.Add(flag);
                }
            }
        }

        LastCheckpointFlag = PlayerPrefs.GetString(LastCheckpointPreferenceKey, null);
    }

    private void SaveState()
    {
        FlagCollection savedFlags = new FlagCollection { values = new List<string>(flags) };
        PlayerPrefs.SetString(FlagsPreferenceKey, JsonUtility.ToJson(savedFlags));

        if (string.IsNullOrEmpty(LastCheckpointFlag))
        {
            PlayerPrefs.DeleteKey(LastCheckpointPreferenceKey);
        }
        else
        {
            PlayerPrefs.SetString(LastCheckpointPreferenceKey, LastCheckpointFlag);
        }

        PlayerPrefs.Save();
    }
}
