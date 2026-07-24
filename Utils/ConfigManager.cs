using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using System.Linq;
using System;
using MiyukiMainMenuMusic; 
using MiyukiMainMenuMusic.MusicPlayer;


namespace MiyukiMainMenuMusic.Utils
{
    public static class ConfigManager
    {
        
        public static ConfigEntry<KeyCode> _keyPlay;
        public static ConfigEntry<KeyCode> _keyPause;
        public static ConfigEntry<KeyCode> _keyPrev;
        public static ConfigEntry<KeyCode> _keyNext;
        public static ConfigEntry<KeyCode> _keyPlaylistNext;
        public static ConfigEntry<KeyCode> _keyPlaylistPrev;
        public static ConfigEntry<KeyCode> _keyStop;
        public static ConfigEntry<KeyCode> _keyRescan;
        
        public static ConfigEntry<string> _currentFID;
        public static ConfigEntry<string> _currentMID;
        public static ConfigEntry<string> _totalSongsCount;
        public static ConfigEntry<string> _foldersCount;
        public static ConfigEntry<string> _musicFolderPath;
        public static ConfigEntry<string> _currentFileName;
        public static ConfigEntry<string> _currentFolderName;
        
        public static ConfigEntry<bool>  Enabled;
        private static readonly List<ConfigEntryBase> ConfigEntries = [];
        // === Громкость ===
        public static ConfigEntry<float> _masterVolume;
        // === НАСТРОЙКИ ЗАТУХАНИЯ/СТАРТА ===
        public static ConfigEntry<float> _fadeInDuration;      // Длительность плавного старта (сек)
        public static ConfigEntry<float> _fadeOutDuration;     // Длительность плавного затухания (сек)
        public static ConfigEntry<float> _fadeStep;            // Шаг изменения громкости за кадр (чем меньше — тем плавнее, но дольше)
        
        public static void Initialize(ConfigFile config)
        {
            const string musicSection = "1. Music Controls";
            const string musicConfig = "2. Music Config";
            const string debugStatus = "3. Debug Info";
            
            _keyPlay = config.Bind(musicSection, "Play", KeyCode.F5, "Клавиша: Играть");
            _keyPause = config.Bind(musicSection, "Pause", KeyCode.F6, "Клавиша: Пауза");
            _keyPrev = config.Bind(musicSection, "Previous", KeyCode.F7, "Клавиша: Предыдущий трек");
            _keyNext = config.Bind(musicSection, "Next", KeyCode.F8, "Клавиша: Следующий трек");
            _keyPlaylistNext = config.Bind(musicSection, "FolderUp", KeyCode.F9, "Клавиша: Папка вверх");
            _keyPlaylistPrev = config.Bind(musicSection, "FolderDown", KeyCode.F10, "Клавиша: Папка вниз");
            _keyStop = config.Bind(musicSection, "Stop", KeyCode.F4, "Клавиша: Стоп");
            _keyRescan = config.Bind(musicSection, "Rescan", KeyCode.F3, "Клавиша: Пересканировать папки и файлы");
            
            // Инициализируем ReadOnly поля
            _currentFID = config.Bind(
                debugStatus, "1. Current FID", "N/A",
                new ConfigDescription("FID текущего трека (авто)", null)); 

            _currentMID = config.Bind(
                debugStatus, "2. Current MID", "N/A",
                new ConfigDescription("MID текущего трека (авто)", null));

            _totalSongsCount = config.Bind(
                debugStatus, "3. Total Songs", "0",
                new ConfigDescription("Количество найденных песен (авто)", null));

            _foldersCount = config.Bind(
                debugStatus, "4. Folders Count", "0",
                new ConfigDescription("Количество папок (авто)", null));

            _musicFolderPath = config.Bind(
                debugStatus, "0. Music Folder Path", @"BepInEx\plugins\MiyukiMainMenuMusic\music",
                new ConfigDescription("Путь к папке с музыкой (авто)", null));

            _currentFileName = config.Bind(
                debugStatus, "5. Current File Name", "N/A",
                new ConfigDescription("Текущий файл (авто)", null));

            _currentFolderName = config.Bind(
                debugStatus, "6. Current Folder Name", "N/A",
                new ConfigDescription("Текущая папка (авто)", null));
            
            _masterVolume = config.Bind(
                musicConfig, "1. Volume", 0.5f,
                new ConfigDescription(
                    "Громкость музыки (0–100%)",
                    new AcceptableValueRange<float>(0.0f, 1.0f),                                                        // <-- ЭТО задаёт диапазон
                    new ConfigurationManagerAttributes                                                                  // <-- ЭТО управляет отображением
                    {
                        ShowRangeAsPercent = true                                                                       // <-- делает ползунок в процентах
                    }
                )
            );
            // === Затухание/Старт ===
            _fadeInDuration = config.Bind(
                musicConfig,
                "2. Fade In Duration",
                1.0f,
                new ConfigDescription(
                    "Длительность плавного старта музыки (сек)",
                    new AcceptableValueRange<float>(0.1f, 10.0f)
                )
            );

            _fadeOutDuration = config.Bind(
                musicConfig,
                "3. Fade Out Duration",
                1.0f,
                new ConfigDescription(
                    "Длительность плавного затухания музыки (сек)",
                    new AcceptableValueRange<float>(0.1f, 10.0f)
                )
            );

            _fadeStep = config.Bind(
                musicConfig,
                "4. Fade Step",
                0.01f,
                new ConfigDescription(
                    "Шаг изменения громкости за кадр (меньше = плавнее)",
                    new AcceptableValueRange<float>(0.001f, 0.1f)
                )
            );
            
        }
 
        public static void UpdateStatus(
            string currentFID,
            string currentMID,
            string totalSongs,
            string foldersCount,
            string musicFolderPath,
            string currentFileName,
            string currentFolderName)
        {
            _currentFID.Value = currentFID;
            _currentMID.Value = currentMID;
            _totalSongsCount.Value = totalSongs;
            _foldersCount.Value = foldersCount;
            _musicFolderPath.Value = musicFolderPath;
            _currentFileName.Value = currentFileName;
            _currentFolderName.Value = currentFolderName;
        }
    }
}