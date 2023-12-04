using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WeaverCore.Settings;

public class SaveData : SaveSpecificSettings
{
    //Public fields will have their data stored to a save file
    public bool bossDefeated;

    //Having the [SerializeField] attribute attached also works
    [SerializeField]
    float testFloat;

    protected override void OnSaveLoaded(int saveFileNumber)
    {
        //Called after a new save file has been loaded, and the data has been loaded from the save file
    }

    protected override void OnSaveUnloaded(int saveFileNumber)
    {
        //Called right before the current data gets saved to a save file
    }
}