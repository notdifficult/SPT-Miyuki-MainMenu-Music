using System.Reflection;
using SPT.Reflection.Patching;
using EFT;


namespace MiyukiMainMenuMusic.Patches
{
    internal sealed class OnRaidEndPatch : ModulePatch
    {
        
        private const BindingFlags GameWorldMethodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly string[] RaidEndMethodNames = ["OnGameSessionEnd", "OnGameEnded", "Dispose"];
    
        
        protected override MethodBase GetTargetMethod()
        {
            foreach(var name in RaidEndMethodNames)
            {
                var method = typeof(GameWorld).GetMethod(name, GameWorldMethodFlags);

                if(method != null) return method;
            }
            Plugin.Log.LogInfo("[MiyukiMainMenuMusic] Raid over1");
            
            Logger.LogWarning("[MiyukiMainMenuMusic] OnRaidEndPatch.cs");
            return null;
        }

        [PatchPostfix]
        public static void PatchPostfix()
        {
            Plugin.Log.LogInfo("[MiyukiMainMenuMusic] Raid over2");
            Logger.LogWarning("[MiyukiMainMenuMusic] OnRaidEndPatch.cs");
            //Lifecycle.OnRaidEnded();
            
        }
    }
}