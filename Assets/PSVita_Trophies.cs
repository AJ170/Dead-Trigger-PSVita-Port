using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PSVita_Trophies : MonoBehaviour
{
    public void Start()
    {
        Debug.Log(Application.persistentDataPath);
    }
    public static void GiveMeTrophy(int key2)
    {
        Debug.Log("trophy: " + key2);
#if !UNITY_EDITOR
        Sony.NP.Trophies.AwardTrophy(key2);
#endif
    }

#if !UNITY_EDITOR
    void Awake()
    {
        DontDestroyOnLoad(this);
        Sony.NP.Main.Initialize(Sony.NP.Main.kNpToolkitCreate_NoAgeRestriction);
        if (!Sony.NP.Trophies.TrophiesAreAvailable) {
            //Sony.Vita.Dialog.Common.ShowUserMessage(Sony.Vita.Dialog.Common.EnumUserMessageType.MSG_DIALOG_BUTTON_TYPE_NONE, true, "Please wait, registering trophy pack...");
            Sony.NP.Trophies.OnPackageRegistered += OnRegistered;
            Sony.NP.Trophies.RegisterTrophyPack();
        }
    }

    void Update()
    {
        Sony.NP.Main.Update();
    }

    void OnApplicationQuit()
    {
        Sony.NP.Main.ShutDown();
    }
    public void OnRegistered(Sony.NP.Messages.PluginMessage msg)
    {
        Debug.Log(msg.Text);
        //Sony.Vita.Dialog.Common.Close();
    }
#endif
}