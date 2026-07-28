using UnityEngine;
using MiyukiMainMenuMusic; 
using MiyukiMainMenuMusic.Utils;


namespace MiyukiMainMenuMusic.MusicPlayer
{
    public static class MusicControl
    {
        private static MusicPlayerMain _player;

        public static void Initialize(MusicPlayerMain player)
        {
            _player = player;
        }
        
        public static void Update()
        {
            // ГЛАВНАЯ ЗАЩИТА: если плеер не инициализирован — выходим сразу
            if (_player == null)
            {
                // Эта строка будет появляться КАЖДЫЙ КАДР, пока ошибка не исправлена.
                // Если видишь её в консоли SPT — значит, Initialize не сработал или сработал с null.
                return; 
            }

            try
            {

                if (Input.GetKeyDown(ConfigManager._keyPlay.Value)) _player.MusicPlay();
                if (Input.GetKeyDown(ConfigManager._keyPause.Value)) _player.MusicPause();
                if (Input.GetKeyDown(ConfigManager._keyPrev.Value)) _player.MusicPrevious();
                if (Input.GetKeyDown(ConfigManager._keyNext.Value)) _player.MusicNext();
                if (Input.GetKeyDown(ConfigManager._keyPlaylistNext.Value)) _player.PlaylistNext();
                if (Input.GetKeyDown(ConfigManager._keyPlaylistPrev.Value)) _player.PlaylistPrev();
                if (Input.GetKeyDown(ConfigManager._keyStop.Value)) _player.MusicStop();
                if (Input.GetKeyDown(ConfigManager._keyRescan.Value))
                {
                    Debug.Log($"[Miyuki] Сканируем треки заново");
                    _player.RescanTracks();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Miyuki] Ошибка в MusicControl.Update(): {ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}