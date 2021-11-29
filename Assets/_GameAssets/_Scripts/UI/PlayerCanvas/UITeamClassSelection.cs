using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] GameObject classButtonsPrefab;
    [SerializeField] Transform buttonsHolder;
    [SerializeField] Button spawnButton;

    UIClassSelectionButton currentSelectedClass;
    List<UIClassSelectionButton> spawnedButtons;

    public void Init(PlayerCanvas canvasScript, TeamClassData[] classData, int playerTeam)
    {
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
        size = classData.Length;
        for (int i = 0; i < size; i++)
        {
            if (classData[i].teamSpecific != 0 && classData[i].teamSpecific != playerTeam)
                continue;

            teamClassData.Add(new ButtonClassData(classData[i], i));
        }

        size = teamClassData.Count;
        for (int i = 0; i < size; i++)
        {
            UIClassSelectionButton classButton = Instantiate(classButtonsPrefab, buttonsHolder).GetComponent<UIClassSelectionButton>();
            classButton.Init(teamClassData[i].data, teamClassData[i].index, canvasScript.SelectClass);

            if (classButton != null) spawnedButtons.Add(classButton);
            else Destroy(classButton.gameObject);
        }
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
    }

    public void ToggleClassSelection(bool toggle)
    {
        classSelectionCanvasGroup.alpha = toggle ? 1 : 0;
        classSelectionCanvasGroup.interactable = classSelectionCanvasGroup.blocksRaycasts = toggle;
        currentSelectedClass.ToggleSelection(toggle);
    }
}
