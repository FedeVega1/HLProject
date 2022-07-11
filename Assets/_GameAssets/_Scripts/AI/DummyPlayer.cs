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
        if (isServer) Invoke(nameof(DummyStart), .25f);
    }

    [Server]
    public void DummyStart()
    {
        GameModeManager.INS.TeamManagerInstance.PlayerSelectedTeam(this, -1);
        GameModeManager.INS.PlayerChangeClass(this, GetClassForSelectedTeam());
        GameModeManager.INS.SpawnPlayerByTeam(this);
    }

    int GetClassForSelectedTeam()
    {
        TeamClassData[] classDataArray = GameModeManager.INS.GetClassData();
        List<int> filteredClassData = new List<int>();
        
        int size = classDataArray.Length;
        for (int i = 0; i < size; i++)
        {
            if (classDataArray[i].teamSpecific == playerTeam)
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
