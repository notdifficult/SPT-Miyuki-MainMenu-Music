//====================================================================================================================//
//using System;
//using System.IO;
using System.Linq;
//using BepInEx;
using BepInEx.Configuration;
//using ConfigurationManager; 
using UnityEngine;
//====================================================================================================================//
namespace NoTDifficult.MiyukiMainMenuMusic
{
    public static class MP3_Settings
    { 
//====================================================================================================================// cfg
        public static ConfigEntry<float> MusicVolume { get; private set; }
        public static ConfigEntry<float> FadeInDuration { get; private set; }
        public static ConfigEntry<KeyCode> PreviousTrackKey { get; private set; }
        public static ConfigEntry<KeyCode> NextTrackKey { get; private set; }
        public static ConfigEntry<string> CurrentScene { get; private set; }
        public static ConfigEntry<string> IgnoredScenesRaw { get; private set; }                                        // private static readonly string[] IgnoredScenePatterns = ["EnvironmentUISceneTue"];
        public static ConfigEntry<string> AllowedSceneSuffix { get; private set; }                                      // private static readonly string AllowedSuffix = "UIScene";
        public static string ExpectedMusicFolderPath => @"BepInEx\plugins\MiyukiMainMenuMusic\music";                   
//====================================================================================================================//        
        public static string[] GetIgnoredScenePatterns()
        {
            if (string.IsNullOrEmpty(IgnoredScenesRaw.Value))
                return System.Array.Empty<string>();

            return IgnoredScenesRaw.Value
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
//====================================================================================================================//          
        public static void Initialize(ConfigFile config)
        {
            //========================================================================================================// general
            MusicVolume = config.Bind(
                "General",
                "1. MusicVolume",
                1f,
                "Громкость музыки (0.0–1.0). Применяется при следующем запуске трека."
            );

            FadeInDuration = config.Bind(
                "General",
                "2. FadeInDuration",
                2f,
                "Длительность плавного нарастания громкости в секундах (0.1–10.0)."
            );
            //========================================================================================================// controls
            PreviousTrackKey = config.Bind(
                "Controls",
                "1. PreviousTrackKey",
                KeyCode.F5,
                "Клавиша для переключения на предыдущий трек."
            );

            NextTrackKey = config.Bind(
                "Controls",
                "2. NextTrackKey",
                KeyCode.F6,
                "Клавиша для переключения на следующий трек."
            );
            //========================================================================================================// debug
            var infoEntry = config.Bind(
                "Debug",
                "1. ExpectedMusicFolderPath",
                ExpectedMusicFolderPath,
                $"Папка, где плагин ожидает найти MP3-файлы. Это справочное значение — оно не предназначено для редактирования."
            );
            
            CurrentScene = config.Bind(
                "Debug", 
                "2. CurrentScene", 
                " ",
                "Текущая загруженная сцена. Это справочное значение — оно не предназначено для редактирования."
            );
            
            IgnoredScenesRaw = config.Bind(
                "Debug", 
                "3. IgnoredScenes", 
                "EnvironmentUISceneTue",
                "Список сцен, где музыка не должна играть. Указывайте названия сцен через запятую (например: SceneA,SceneB)."
            );
            
            AllowedSceneSuffix = config.Bind(
                "Debug", 
                "4. AllowedSceneSuffix", 
                "UIScene",
                "Суффикс имени сцены, для которых музыка может играть (например, 'UIScene')."
            );
            //========================================================================================================//
        }
    }
}
//====================================================================================================================//