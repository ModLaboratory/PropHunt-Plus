﻿// Core Script of PropHuntPlugin
// Copyright (C) 2022  ugackMiner
global using static PropHunt.Language;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace PropHunt;


[BepInPlugin("com.jiege.prophuntre", "Prop Hunt", "1.0")]
[BepInProcess("Among Us.exe")]
public partial class PropHuntPlugin : BasePlugin
{
    // Backend Variables
    public Harmony Harmony { get; } = new("com.jiege.prophuntre");
    public ConfigEntry<int> HidingTime { get; set; }
    public ConfigEntry<int> MaxMissedKills { get; set; }
    public ConfigEntry<bool> Infection { get; set; }
    public ConfigEntry<bool> Debug { get; set; }
    internal static ManualLogSource Logger;

    // Gameplay Variables
    public static int hidingTime
    {
        get => Instance.HidingTime.Value;
        set 
        {
            Instance.HidingTime.Value = value;
            Instance.Config.Save();
        }
    }
    public static int maxMissedKills
    {
        get => Instance.MaxMissedKills.Value;
        set
        {
            Instance.MaxMissedKills.Value = value;
            Instance.Config.Save();
        }
    }
    public static bool infection
    {
        get => Instance.Infection.Value;
        set
        {
            Instance.Infection.Value = value;
            Instance.Config.Save();
        }
    }

    public static int missedKills = 0;

    public static PropHuntPlugin Instance;


    public override void Load()
    {
        Logger = base.Log;
        HidingTime = Config.Bind("Prop Hunt", "Hiding Time", 30);
        MaxMissedKills = Config.Bind("Prop Hunt", "Max Misses", 3);
        Infection = Config.Bind("Prop Hunt", "Infection", true);
        Debug = Config.Bind("Prop Hunt", "Debug", false);

        Instance = this;

        Harmony.PatchAll(typeof(CustomRoleSettings));
        Harmony.PatchAll(typeof(Patches));
        Harmony.PatchAll(typeof(RPCPatch));
        Harmony.PatchAll(typeof(Language));
        Logger.LogInfo("Loaded");
    }

    

    public static class RPCHandler
    {
        public static void RPCPropSync(PlayerControl player, int propIndex)
        {
            GameObject prop = ShipStatus.Instance.AllConsoles[propIndex].gameObject;
            Logger.LogInfo($"{player.Data.PlayerName} changed sprite to: {prop.name}");
            player.GetComponent<SpriteRenderer>().sprite = prop.GetComponent<SpriteRenderer>().sprite;
            player.transform.localScale = prop.transform.lossyScale;
            player.Visible = false;
        }

        public static void RPCSettingSync(PlayerControl player, int _hidingTime, int _missedKills, bool _infection)
        {
            hidingTime = _hidingTime;
            maxMissedKills = _missedKills;
            infection = _infection;
            Logger.LogInfo("H: " + PropHuntPlugin.hidingTime + ", M: " + PropHuntPlugin.maxMissedKills + ", I: " + PropHuntPlugin.infection);
            if (player == PlayerControl.LocalPlayer && (hidingTime != Instance.HidingTime.Value || maxMissedKills != Instance.MaxMissedKills.Value || infection != Instance.Infection.Value))
            {
                Instance.HidingTime.Value = hidingTime;
                Instance.MaxMissedKills.Value = maxMissedKills;
                Instance.Infection.Value = infection;
                Instance.Config.Save();
            }
        }
    }


    public static class Utility
    {
        public static GameObject FindClosestConsole(GameObject origin, float radius)
        {
            try
            {
                Collider2D bestCollider = null;
                float bestDist = 9999;
                foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin.transform.position, radius))
                {
                    if (collider.GetComponent<Console>() != null)
                    {
                        float dist = Vector2.Distance(origin.transform.position, collider.transform.position);
                        if (dist < bestDist)
                        {
                            bestCollider = collider;
                            bestDist = dist;
                        }
                    }
                }
                return bestCollider.gameObject;
            }
            catch
            {
                Logger.LogError("Error getting nearest console");
                return null;
            }
        }
    }
}
