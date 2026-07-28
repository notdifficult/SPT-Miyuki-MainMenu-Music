//======================================================================================================================//
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using HarmonyLib;
using UnityEngine;
using MiyukiMainMenuMusic; 
//======================================================================================================================//
namespace MiyukiMainMenuMusic.Patches { internal class OnRaidStartPatch : ModulePatch {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), nameof(GameWorld.OnGameStarted));
            //return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }
//======================================================================================================================//
        /*[PatchPostfix] public static void PatchPostfix()
        {
            Plugin.Log.LogInfo("[MiyukiMainMenuMusic] Raid starting");
            Logger.LogWarning("[MiyukiMainMenuMusic] OnRaidStartPatch.cs");
        }*/
        [PatchPostfix] public static void PatchPostfix()
        //[PatchPrefix] private static bool Prefix()
        {
            Logger.LogWarning("[MiyukiMainMenuMusic] OnRaidStartPatch.cs");
            Plugin.Instance.StopMusic();
        }
    }
}