using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class DummyPlayer : Player
{
    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    DummyStart();
    //}

    protected override void Start()
    {
        base.Start();
        if (IsServer) Invoke(nameof(DummyStart), .25f);
    }

    [Server]
    public void DummyStart()
    {
        GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam_Server(this, -1);
        GameModeManager.INS.PlayerChangeClass_Server(this, GetClassForSelectedTeam());
        GameModeManager.INS.SpawnPlayerByTeam_Server(this);
    }

    int GetClassForSelectedTeam()
    {
        TeamClassData[] classDataArray = GameModeManager.INS.GetClassData();
        List<int> filteredClassData = new List<int>();
        
        int size = classDataArray.Length;
        for (int i = 0; i < size; i++)
        {
            if (classDataArray[i].teamSpecific == playerTeam.Value)
            {
                filteredClassData.Add(i);
                //print($"{playerTeam} - {classDataArray[i]}");
            }
        }

        int index = Random.Range(0, filteredClassData.Count);
        classData = classDataArray[filteredClassData[index]];
        //print($"{index} - {filteredClassData[index]} - {classData}");
        return filteredClassData[index];
    }
}
