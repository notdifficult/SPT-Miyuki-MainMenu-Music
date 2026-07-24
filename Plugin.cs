//======================================================================================================================//
using System;
using BepInEx;
using BepInEx.Logging;
using MiyukiMainMenuMusic.Patches;
using MiyukiMainMenuMusic.Utils;
using MiyukiMainMenuMusic.MusicPlayer;
using UnityEngine;
//======================================================================================================================//
namespace MiyukiMainMenuMusic
{
    [BepInPlugin(PluginsInfo.GUID, PluginsInfo.NAME, PluginsInfo.VERSION)]                                              
    //[BepInDependency("com.SPT.core", "4.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        
        public static Plugin Instance { get; private set; }
        public MusicPlayerMain _MusicPlayer;
        
        private TrackInfo[] _preparedTracks = Array.Empty<TrackInfo>(); // Храним список, готовый к передаче
//======================================================================================================================//       
        private void Awake()
        {
            Instance = this;
            Log = Logger;
            

            ConfigManager.Initialize(Config);
            
            new OnRaidStartPatch().Enable();
            new OnRaidEndPatch().Enable();
            new OnMainMenuPatch().Enable();
 
            Log.LogInfo(PluginsInfo.Format($"{PluginsInfo.NAME} v{PluginsInfo.VERSION} скрипт загружен, плагин стартует."));
            
            
            _preparedTracks = MusicFolder.ScanMusic();
            Logger.LogInfo($"[Miyuki] Сканирование завершено. Подготовлено треков: {_preparedTracks.Length}");
  
        }
//======================================================================================================================//
        private void Update()
        {
            // Каждый кадр проверяем нажатия клавиш
            MusicControl.Update();
        }
//======================================================================================================================// 
        public TrackInfo[] GetPreparedTracks() => _preparedTracks;
        
//======================================================================================================================//
        public MusicPlayerMain GetOrCreatePlayer()
        {
            // Сначала пробуем найти уже существующий
            var existing = FindObjectOfType<MusicPlayerMain>();
            if (existing != null)
            {
                _MusicPlayer = existing;
                Logger.LogInfo("[Miyuki] Найден существующий плеер.");
                return _MusicPlayer;
            }

            // Если не нашли — создаём новый объект
            var newObj = new GameObject("MiyukiMainMenuMusic");
            
            // Создаём плеер, передавая объект, чтобы он сам сделал DontDestroyOnLoad
            _MusicPlayer = newObj.AddComponent<MusicPlayerMain>();
            _MusicPlayer.CreatePlayer(newObj); // <-- Тут произойдёт магия с DontDestroyOnLoad
            MusicControl.Initialize(_MusicPlayer);
            Logger.LogInfo("[Miyuki] Создан новый объект плеера и AudioSource.");
            return _MusicPlayer;
        }
//======================================================================================================================//      
        
        public void StartMusic()
        {

            Logger.LogError("[Miyuki] public void StartMusic");
 
            if (_MusicPlayer == null)
            {
                Logger.LogError("[Miyuki] Попытка запуска музыки, но плеер не найден!");
                return;
            }
            
            // Передаём заранее подготовленные треки
            _MusicPlayer.SetTracks(_preparedTracks);
            
            // Запускаем асинхронную загрузку файлов в AudioClip
            _MusicPlayer.LoadTracksAsync();
            //_MusicPlayer.MusicPlay();
        }
//======================================================================================================================//
        public void StopMusic()
        {
            Logger.LogError("[Miyuki] public void StopMusic");
            
            _MusicPlayer.MusicStop();
        }
//======================================================================================================================//
    }
}