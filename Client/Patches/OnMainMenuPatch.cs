//======================================================================================================================//
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
//======================================================================================================================//
namespace MiyukiMainMenuMusic.Patches { internal class OnMainMenuPatch : ModulePatch { protected override MethodBase GetTargetMethod() { return AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.method_5)); }
//======================================================================================================================//
        [PatchPostfix] private static void Postfix(MainMenuControllerClass __instance)
        {
            //Logger.LogWarning("[MiyukiMainMenuMusic] OnMainMenuPatch.cs");                                            //Plugin.Log.LogInfo("[MiyukiMainMenuMusic] MainMenu");  //Plugin.Log.LogInfo("MMCC.method_5 ran");
            if (Plugin.Instance == null) { Logger.LogError("[MiyukiMainMenuMusic] ModMain.Instance ещё не готов!"); return; }
            var player = Plugin.Instance.GetOrCreatePlayer();                                                           // 1. Получаем или создаём плеер (если его ещё нет)
            if (player == null) return;
            player.SetTracks(Plugin.Instance.GetPreparedTracks());
            Plugin.Instance.StartMusic();                                                                               // 3. ЗАПУСКАЕМ МУЗЫКУ Внутри StartMusic произойдёт: передача треков -> загрузка файлов (LoadTracksAsync) -> MusicPlay()
        }
    }
}
//======================================================================================================================//