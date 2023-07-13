﻿// Core Script of PropHuntPlugin
// Copyright (C) 2022  ugackMiner
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
    public ConfigEntry<int> HidingTime { get; private set; }
    public ConfigEntry<int> MaxMissedKills { get; private set; }
    public ConfigEntry<bool> Infection { get; private set; }
    internal static ManualLogSource Logger;

    // Gameplay Variables
    public static int hidingTime = 30;
    public static int maxMissedKills = 3;
    public static bool infection = true;

    public static int missedKills = 0;

    public static PropHuntPlugin Instance;


    public override void Load()
    {
        Logger = base.Log;
        HidingTime = Config.Bind("Prop Hunt", "Hiding Time", 30);
        MaxMissedKills = Config.Bind("Prop Hunt", "Max Misses", 3);
        Infection = Config.Bind("Prop Hunt", "Infection", true);

        Instance = this;

        Harmony.PatchAll(typeof(CustomRoleSettings));
        Harmony.PatchAll(typeof(Patches));
        Harmony.PatchAll(typeof(RPCPatch));
        Logger.LogInfo("Loaded");
    }

    

    public static class RPCHandler
    {
        public static void RPCPropSync(PlayerControl player, string propIndex)
        {
            GameObject prop = ShipStatus.Instance.AllConsoles[int.Parse(propIndex)].gameObject;
            Logger.LogInfo($"{player.Data.PlayerName} changed their sprite to: {prop.name}");
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

        public static IEnumerator KillConsoleAnimation()
        {
            if (Constants.ShouldPlaySfx())
            {
                SoundManager.Instance.PlaySound(ShipStatus.Instance.SabotageSound, false, 0.8f);
                HudManager.Instance.FullScreen.color = new Color(1f, 0f, 0f, 0.372549027f);
                HudManager.Instance.FullScreen.gameObject.SetActive(true);
                yield return new WaitForSeconds(0.5f);
                HudManager.Instance.FullScreen.gameObject.SetActive(false);
            }
            yield break;
        }

        public static IEnumerator IntroCutsceneHidePatch(IntroCutscene __instance)
        {
            PlayerControl.LocalPlayer.moveable = false;
            yield return new WaitForSeconds(PropHuntPlugin.hidingTime);
            PlayerControl.LocalPlayer.moveable = true;
            Object.Destroy(__instance.gameObject);
        }
    }
}
