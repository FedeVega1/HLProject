using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

public class UITeamClassSelection : MonoBehaviour
{
    struct ButtonClassData
    {
        public TeamClassData data;
        public int index;

        public ButtonClassData(TeamClassData _data, int _index)
        {
            data = _data;
            index = _index;
        }
    }

    [SerializeField] CanvasGroup classSelectionCanvasGroup;
    //[SerializeField] GameObject classButtonsPrefab;
    [SerializeField] Transform buttonsHolder;
    [SerializeField] Button spawnButton;
    [SerializeField] TMP_Text lblRespawnTime;

    UIClassSelectionButton currentSelectedClass;
    List<UIClassSelectionButton> spawnedButtons;

    bool onRespawn;
    double respawnTime;
    Player playerScript;

    AsyncOperationHandle<GameObject> btnClassSelectionHandle;

    void Awake()
    {
        btnClassSelectionHandle = Addressables.LoadAssetAsync<GameObject>("UIPrefabs/btnClassSelection");
        btnClassSelectionHandle.Completed += OnbtnClassSelectionHandleCompleted;
    }

    void OnbtnClassSelectionHandleCompleted(AsyncOperationHandle<GameObject> operation)
    {
        if (operation.Status == AsyncOperationStatus.Failed)
            Debug.LogErrorFormat("Couldn't load classButton Prefab: {0}", operation.OperationException);
    }

    void OnDestroy()
    {
        Addressables.Release(btnClassSelectionHandle);
    }

    void Update()
    {
        if (!onRespawn)
        {
            if (playerScript != null && playerScript.IsDead && !spawnButton.interactable)
                ToggleSpawnButton(true);
            return;
        }

            double time = respawnTime - NetworkTime.time;
        if (time <= 0)
        {
            respawnTime = 0;
            onRespawn = false;
            lblRespawnTime.text = $"You can respawn now";
            ToggleSpawnButton(true);
            return;
        }

        lblRespawnTime.text = $"You can respawn in: {System.Math.Round(time, 0)}";
    }

    public void Init(PlayerCanvas canvasScript, IList<TeamClassData> classData, int playerTeam, ref Player player)
    {
        playerScript = player;

        int size;
        if (spawnedButtons != null)
        {
            size = spawnedButtons.Count;
            for (int i = 0; i < size; i++) Destroy(spawnedButtons[i].gameObject);

            spawnedButtons.Clear();
            currentSelectedClass = null;
        }
        else
        {
            spawnedButtons = new List<UIClassSelectionButton>();
        }

        List<ButtonClassData> teamClassData = new List<ButtonClassData>();
        size = classData.Count;
        for (int i = 0; i < size; i++)
        {
            if (classData[i].teamSpecific != 0 && classData[i].teamSpecific != playerTeam)
                continue;

            teamClassData.Add(new ButtonClassData(classData[i], i));
        }

        size = teamClassData.Count;
        for (int i = 0; i < size; i++)
        {
            UIClassSelectionButton classButton = Instantiate(btnClassSelectionHandle.Result, buttonsHolder).GetComponent<UIClassSelectionButton>();
            classButton.Init(teamClassData[i].data, teamClassData[i].index, canvasScript.SelectClass);

            if (classButton != null) spawnedButtons.Add(classButton);
            else Destroy(classButton.gameObject);
        }

        lblRespawnTime.text = $"Select a class";
    }

    public void ToggleSpawnButton(bool toggle)
    {
        spawnButton.interactable = toggle;
    }

    public void ClassSelected(int classIndex)
    {
        if (currentSelectedClass != null) currentSelectedClass.ToggleSelection(false);

        int size = spawnedButtons.Count;
        for (int i = 0; i < size; i++)
        {
            if (spawnedButtons[i].ClassIndex == classIndex)
            {
                spawnedButtons[i].ToggleSelection(true);
                currentSelectedClass = spawnedButtons[i];
                break;
            }
        }

        if (!onRespawn && respawnTime <= 0) lblRespawnTime.text = $"You can respawn now";
    }

    public void ToggleClassSelection(bool toggle)
    {
        classSelectionCanvasGroup.alpha = toggle ? 1 : 0;
        classSelectionCanvasGroup.interactable = classSelectionCanvasGroup.blocksRaycasts = toggle;
        if (currentSelectedClass != null) currentSelectedClass.ToggleSelection(toggle);
        if (!onRespawn && playerScript.IsDead) ToggleSpawnButton(true);
    }

    public void TryRespawn()
    {
        respawnTime = 0;
        lblRespawnTime.text = "";
        spawnButton.interactable = false;
        onRespawn = false;
    }

    public void ShowRespawnTimer(double time)
    {
        respawnTime = time;
        onRespawn = true;
        spawnButton.interactable = false;
    }
}
