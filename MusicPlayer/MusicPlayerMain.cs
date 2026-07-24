//======================================================================================================================//
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using MiyukiMainMenuMusic.Utils;
//======================================================================================================================//
namespace MiyukiMainMenuMusic.MusicPlayer
{
    public class TrackInfo                                                                                              // Хранит метаданные трека: папка (FID), номер в папке (MID), сам клип и путь
    {
        public int FID;                                                                                                 // ID папки: 0 = корень, 1+ = подпапки
        public int MID;                                                                                                 // Номер трека внутри папки
        public AudioClip Clip;                                                                                          // Сам аудиоклип
        public string Path;                                                                                             // Абсолютный путь к файлу (для отладки/перезагрузки)
        public string Name;                                                                                             // Имя файла
    }
//======================================================================================================================//
    public class MusicPlayerMain : MonoBehaviour
    {
        private AudioSource _audioSource;
        private readonly System.Random _random = new System.Random();
        private TrackInfo[] _tracks = Array.Empty<TrackInfo>();                                                         // Плоский список всех треков, отсортированный по FID: сначала FID=0, потом FID=1 и т.д.
        private int _currentTrackIndex = -1;                                                                            // Индекс текущего трека в плоском массиве
        private bool _isPaused = false;
        private Coroutine _loadCoroutine;
        private Coroutine _fadeCoroutine;
        private bool _isFading = false;                                                                                 // Флаг: сейчас идёт анимация громкости
        private float _targetVolume = 0f;                                                                               // Куда мы идём (0 или master)
        
        
        
//======================================================================================================================//  
        private void Update()
        {
            // Если ничего не играет — ничего не делаем
            if (_audioSource == null || !_audioSource.isPlaying) { return; }

            // Проверяем, не дошёл ли трек до конца
            if (_currentTrackIndex >= 0 && _currentTrackIndex < _tracks.Length)
            {
                var track = _tracks[_currentTrackIndex];
                if (track.Clip != null && _audioSource.time >= track.Clip.length - 0.1f)
                {
                    PlayRandomFromCurrentFolder();                                                                      // Трек почти закончился (с небольшим запасом). Запускаем следующий рандомный трек ИЗ ТОЙ ЖЕ ПАПКИ (FID)
                }
            }
            
            
            // === ПРИМЕНЯЕМ ГРОМКОСТЬ ИЗ КОНФИГА ===
            if (_audioSource != null)
            {
                float volume = ConfigManager._masterVolume.Value;
                _audioSource.volume = Mathf.Clamp01(volume);                                                            // защита на всякий случай
            }
            UpdateConfigStatus();
        }
        
        private void UpdateConfigStatus()
        {
            string currentFIDStr = "N/A";
            string currentMIDStr = "N/A";
            string fileName = "N/A";
            string folderName = "N/A";

            if (_currentTrackIndex >= 0 && _currentTrackIndex < _tracks.Length)
            {
                var t = _tracks[_currentTrackIndex];
                currentFIDStr = t.FID.ToString();
                currentMIDStr = t.MID.ToString(); 
                //fileName = t.Name;
                fileName = System.IO.Path.GetFileName(t.Path);
                // Папка: можно взять из Path (если FID соответствует папкам) или хранить отдельно.
                // Самый простой вариант — имя папки из пути:
                //folderName = System.IO.Path.GetDirectoryName(t.Path) ?? "Root";
                string fullDir = System.IO.Path.GetDirectoryName(t.Path);
                if (!string.IsNullOrEmpty(fullDir))
                {
                    folderName = System.IO.Path.GetFileName(fullDir);
                }
                else
                {
                    folderName = "Root";
                }
            }

            int totalSongs = _tracks.Length;
    
            // Количество папок: считаем уникальные FID
            int foldersCount = _tracks
                .Select(x => x.FID)
                .Distinct()
                .Count();

            string musicPath = @"BepInEx\plugins\MiyukiMainMenuMusic\music";                                            // или подставь свой путь из конфига/переменной

            ConfigManager.UpdateStatus(
                currentFID: currentFIDStr,
                currentMID: currentMIDStr,
                totalSongs: totalSongs.ToString(),
                foldersCount: foldersCount.ToString(),
                musicFolderPath: musicPath,
                currentFileName: fileName,
                currentFolderName: folderName
            );
        }
//======================================================================================================================//  
        private void PlayRandomFromCurrentFolder()
        {
            if (_currentTrackIndex < 0 || _currentTrackIndex >= _tracks.Length)
                return;

            int currentFID = _tracks[_currentTrackIndex].FID;

            // Собираем все треки из текущей папки
            var tracksInFolder = _tracks
                .Where(t => t.FID == currentFID)
                .ToArray();

            if (tracksInFolder.Length == 0)
                return;

            // Выбираем случайный трек из этой папки, но не тот, который только что играл
            int randomIndex;
            do
            {
                randomIndex = _random.Next(tracksInFolder.Length);
            } while (tracksInFolder[randomIndex].Path == _tracks[_currentTrackIndex].Path && tracksInFolder.Length > 1);

            // Находим индекс этого трека в общем массиве _tracks
            var nextTrack = tracksInFolder[randomIndex];
            _currentTrackIndex = Array.FindIndex(_tracks, t => t.Path == nextTrack.Path);
            var track = _tracks[_currentTrackIndex];
            Plugin.Log.LogInfo($"[Miyuki PlayRandom] Сейчас играет: \"{track.Name}\" | FID: {track.FID} | MID: {track.MID}");
            PlayCurrentTrackByIndex(_currentTrackIndex);
        }
//======================================================================================================================// 
        
        
        
        
        
        
//======================================================================================================================//       
        /// <summary>
        /// 1 - Функция Создать Плеер Создаёт AudioSource, настраивает параметры, ничего не играет.
        /// Вызывать один раз при старте мода.
        /// </summary>
        public void CreatePlayer(GameObject targetObject)
        {
            if (_audioSource != null)
            {
                Plugin.Log.LogInfo($"[Miyuki] Плеер уже создан.");
                return;
            }
            // Сначала вешаем DontDestroyOnLoad на сам объект (критично!)
            DontDestroyOnLoad(targetObject);
            _audioSource = targetObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;                                                                                  // Зацикливать трек
            _audioSource.spatialBlend = 0f;                                                                             // 2D-звук (не зависит от позиции камеры)
            _audioSource.ignoreListenerPause = true;                                                                    // Не глушится при паузе игры
            _audioSource.volume = 0f;                                                                                   // Начинаем с 0, чтобы корректно работал Fade In

            Plugin.Log.LogInfo("[Miyuki] Плеер создан.");
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//        
        /// <summary>
        /// Устанавливает список треков (результат работы MusicFolder.ScanMusic).
        /// Вызывать после сканирования папки с музыкой.
        /// </summary>
        public void SetTracks(TrackInfo[] tracks)
        {
            _tracks = tracks ?? Array.Empty<TrackInfo>();
            _currentTrackIndex = -1;
            _isPaused = false;
            Plugin.Log.LogInfo($"[Miyuki] Список треков обновлён. Треков: {_tracks.Length}");
        }
//======================================================================================================================//
        
//======================================================================================================================//        
        /// <summary>
        /// Вспомогательный: получить FID текущего трека
        /// Работает потому, что _tracks отсортированы по FID.
        /// </summary>
        private int GetCurrentFID()
        {
            if (_currentTrackIndex < 0 || _currentTrackIndex >= _tracks.Length)
                return -1;
            return _tracks[_currentTrackIndex].FID;
        }
//======================================================================================================================//     
        
//======================================================================================================================//      
        /// <summary>
        /// 7 - Функция Музыка Рандом (внутренняя)
        /// Случайно выбирает трек из всего списка.
        /// Возвращает индекс в плоском массиве.
        /// </summary>
        private int GetRandomTrackIndex()
        {
            if (_tracks.Length == 0) return -1;
            return _random.Next(_tracks.Length);
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//
        /// <summary>
        /// 2 - Функция Музыка Стоп
        /// Полностью останавливает воспроизведение, сбрасывает позицию, очищает текущий трек.
        /// НЕ удаляет клипы — можно будет снова играть.
        /// </summary>
        public void MusicStop()
        {
            if (_isFading) return;                                                                                      // Если уже идёт затухание — ничего не делаем

            if (_audioSource != null)
            {
                _audioSource.Stop();
                if (_audioSource.clip != null)
                {
                    _isFading = true;
                    _targetVolume = 0f;
                    _audioSource.time = 0f;
                }
            }
            _isPaused = false;
            _currentTrackIndex = -1;
            Plugin.Log.LogInfo("[Miyuki] Музыка полностью остановлена.");
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
//======================================================================================================================// 
        /// <summary>
        /// 3 - Функция Музыка Играй
        /// Если была пауза — продолжить. Если ничего не играло или был стоп — выбрать случайный трек.
        /// </summary>
        public void MusicPlay()
        {
            if (_audioSource == null)
            {
                Plugin.Log.LogInfo($"[Miyuki] Плеер не создан! Вызови CreatePlayer().");
                return;
            }

            // Если была пауза и есть активный клип — просто продолжаем
            if (_isPaused && _currentTrackIndex >= 0 && _currentTrackIndex < _tracks.Length)
            {
                _audioSource.UnPause();
                _isPaused = false;
                Plugin.Log.LogInfo($"[Miyuki] Воспроизведение продолжено (с паузы).");
                return;
            }

            // Иначе — выбираем случайный трек
            int index = GetRandomTrackIndex();
            if (index < 0)
            {
                Plugin.Log.LogInfo($"[Miyuki] Нет доступных треков для воспроизведения.");
                return;
            }

            _currentTrackIndex = index;
            PlayCurrentTrackByIndex(_currentTrackIndex);
            var track = _tracks[_currentTrackIndex];
            Plugin.Log.LogInfo($"[Miyuki MusicPlay] Сейчас играет: \"{track.Name}\" | FID: {track.FID} | MID: {track.MID}");
            // Плавный старт
            if (!_isFading && _audioSource.clip != null)
            {
                _isFading = true;
                _targetVolume = ConfigManager._masterVolume.Value;
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeInCoroutine());
            }
        }
//======================================================================================================================//
        /// <summary>
        /// Играет трек по индексу в плоском списке.
        /// Используется внутри других функций переключения.
        /// </summary>
        private void PlayCurrentTrackByIndex(int index)
        {
            if (index < 0 || index >= _tracks.Length) return;

            var track = _tracks[index];
            if (track.Clip == null)
            {
                // ВАЖНО: если клип не загружен — не пытаемся играть, а запускаем загрузку
                Plugin.Log.LogInfo($"[Miyuki] Клип не загружен для трека: {track.Name}. Запускаю загрузку всех треков...");
                LoadTracksAsync();
                return;
            }

            _audioSource.clip = track.Clip;
            _audioSource.Play();
            _isPaused = false;
            Plugin.Log.LogInfo($"[Miyuki MusicCurrentByIndex] Сейчас играет: \"{track.Name}\" | FID: {track.FID} | MID: {track.MID}");
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//
        /// <summary>
        /// 4 - Функция Музыка Далее
        /// Следующий трек в общем списке (циклично).
        /// </summary>
        public void MusicNext()
        {
            if (_tracks.Length <= 1)
            {
                Plugin.Log.LogInfo($"[Miyuki] Недостаточно треков для переключения.");
                return;
            }

            int nextIndex = (_currentTrackIndex + 1) % _tracks.Length;
            _currentTrackIndex = nextIndex;
            // Сначала затухание текущего, потом новый трек
            if (_isFading)
            {
                var track = _tracks[_currentTrackIndex];
                Plugin.Log.LogInfo(
                    $"[Miyuki MusicNext] Сейчас играет: \"{track.Name}\" | FID: {track.FID} | MID: {track.MID}");
                PlayCurrentTrackByIndex(_currentTrackIndex);
            }
            else
            {
                MusicStop(); // Запускаем затухание
                // После завершения FadeOutCoroutine мы должны запустить новый трек.
                // Самый простой способ — сделать это внутри FadeOutCoroutine, когда громкость станет 0.
            }
        }
//======================================================================================================================//
        
        
//======================================================================================================================//
        /// <summary>
        /// 5 - Функция Музыка Назад
        /// Предыдущий трек в общем списке (циклично).
        /// </summary>
        public void MusicPrevious()
        {
            if (_tracks.Length <= 1)
            {
                Plugin.Log.LogInfo($"[Miyuki] Недостаточно треков для переключения.");
                return;
            }

            int prevIndex = (_currentTrackIndex - 1 + _tracks.Length) % _tracks.Length;
            _currentTrackIndex = prevIndex;
            if (_isFading)
            {
                var track = _tracks[_currentTrackIndex];
                Plugin.Log.LogInfo($"[Miyuki MusicPrevious] Сейчас играет: \"{track.Name}\" | FID: {track.FID} | MID: {track.MID}");
                PlayCurrentTrackByIndex(_currentTrackIndex);
            }
            else
            {
                MusicStop();
            }
        }
//======================================================================================================================//
        
        
//======================================================================================================================//
        /// <summary>
        /// 6 - Функция Музыка Пауза
        /// Переключает: если играет — пауза, если на паузе — продолжить.
        /// Не сбрасывает трек и позицию.
        /// </summary>
        public void MusicPause()
        {
            if (_audioSource == null || _audioSource.clip == null)
            {
                Plugin.Log.LogInfo($"[Miyuki] Невозможно поставить на паузу: нет активного клипа.");
                return;
            }

            if (_isPaused)
            {
                _audioSource.UnPause();
                _isPaused = false;
                Plugin.Log.LogInfo($"[Miyuki] Воспроизведение возобновлено.");
            }
            else
            {
                _audioSource.Pause();
                _isPaused = true;
                Plugin.Log.LogInfo($"[Miyuki] Воспроизведение поставлено на паузу.");
            }
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//
        public void RescanTracks()
        {
            // Тут вызывается логика сканирования (ScanMusic / LoadTracksAsync и т.п.)
            var newTracks = MusicFolder.ScanMusic();
            SetTracks(newTracks);
            Plugin.Log.LogInfo($"[Miyuki] Сканирование завершено. Треков: {newTracks.Length}"); 
        }   
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//
        /// <summary>
        /// 3 (доп.) - Смена плейлиста: перейти на следующую папку (FID + 1), циклично.
        /// При переходе ставится первый трек в новой папке.
        /// </summary>
        public void PlaylistNext()
        {
            
            if (_tracks.Length == 0)
            {
                Plugin.Log.LogInfo($"[Miyuki] Нет треков для переключения плейлиста.");
                return;
            }

            int currentFID = GetCurrentFID();
            if (currentFID < 0) currentFID = _tracks[0].FID; // Если ничего не играло — берём первую папку

            // Ищем первую папку с FID > currentFID. Если не нашли — берём FID = 0 (цикл)
            int targetFID = -1;
            foreach (var t in _tracks)
            {
                if (t.FID > currentFID)
                {
                    targetFID = t.FID;
                    break;
                }
            }

            // Цикл: если не нашли большую — берём самую первую (FID 0 или минимальный)
            if (targetFID == -1)
            {
                targetFID = _tracks[0].FID;
            }

            // Находим первый трек с этим FID
            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i].FID == targetFID)
                {
                    _currentTrackIndex = i;
                    PlayCurrentTrackByIndex(_currentTrackIndex);
                    Plugin.Log.LogInfo($"[Miyuki] Переключён плейлист: FID={targetFID}");
                    return;
                }
            }

            Plugin.Log.LogInfo($"[Miyuki] Не удалось найти папку с треками.");
        }
//======================================================================================================================//
        
        
        
//======================================================================================================================//
        /// <summary>
        /// 4 (доп.) - Смена плейлиста: перейти на предыдущую папку (FID - 1), циклично.
        /// При переходе ставится первый трек в этой папке.
        /// </summary>
        public void PlaylistPrev()
        {
            if (_tracks.Length == 0)
            {
                Plugin.Log.LogInfo($"[Miyuki] Нет треков для переключения плейлиста.");
                return;
            }

            int currentFID = GetCurrentFID();
            if (currentFID < 0) currentFID = _tracks[_tracks.Length - 1].FID; // Если ничего не играло — считаем, что мы в последней папке

            // Ищем последнюю папку с FID < currentFID
            int targetFID = -1;
            for (int i = _tracks.Length - 1; i >= 0; i--)
            {
                if (_tracks[i].FID < currentFID)
                {
                    targetFID = _tracks[i].FID;
                    break;
                }
            }
            
            // Цикл: если не нашли меньшую — берём максимальный FID (последнюю папку)
            if (targetFID == -1)
            {
                // Находим максимальный FID в списке
                targetFID = _tracks.Max(t => t.FID);
            }

            // Находим первый трек с этим FID
            for (int i = 0; i < _tracks.Length; i++)
            {
                if (_tracks[i].FID == targetFID)
                {
                    _currentTrackIndex = i;
                    PlayCurrentTrackByIndex(_currentTrackIndex);
                    Plugin.Log.LogInfo($"[Miyuki] Переключён плейлист: FID={targetFID}");
                    return;
                }
            }

            Plugin.Log.LogInfo($"[Miyuki] Не удалось найти папку с треками.");
        }
//======================================================================================================================//
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
//======================================================================================================================//
        /// <summary>
        /// Загружает все треки из списка в AudioClip через UnityWebRequestMultimedia.
        /// Асинхронная загрузка всех треков в AudioClip.
        /// Запускает MusicPlay() после загрузки.
        /// </summary>
        public void LoadTracksAsync()
        {
            if (_tracks.Length == 0)
            {
                Plugin.Log.LogInfo($"[Miyuki] Нет треков для загрузки!");
                return;
            }

            // Если уже грузим — останавливаем старую загрузку и запускаем новую
            if (_loadCoroutine != null) StopCoroutine(_loadCoroutine);
            _loadCoroutine = StartCoroutine(LoadTracksRoutine());
        }
//======================================================================================================================//
        
        
        
        
        
        // Корутина плавного старта
        private IEnumerator FadeInCoroutine()
        {
            float startVolume = _audioSource.volume;
            float target = _targetVolume;
            float duration = ConfigManager._fadeInDuration.Value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Можно использовать Mathf.Lerp или Mathf.SmoothStep для более плавной кривой
                float newVol = Mathf.Lerp(startVolume, target, t);
                _audioSource.volume = Mathf.Clamp01(newVol);
                yield return null;
            }

            _audioSource.volume = target;
            _isFading = false;
            Plugin.Log.LogInfo($"[Miyuki] Плавный старт завершён.");
        }

        // Корутина плавного затухания
        private IEnumerator FadeOutCoroutine()
        {
            float startVolume = _audioSource.volume;
            float target = _targetVolume; // 0
            float duration = ConfigManager._fadeOutDuration.Value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float newVol = Mathf.Lerp(startVolume, target, t);
                _audioSource.volume = Mathf.Clamp01(newVol);
                yield return null;
            }

            _audioSource.volume = target;
            _audioSource.Stop(); // Останавливаем после затухания
            _isFading = false;

            Plugin.Log.LogInfo($"[Miyuki] Плавное затухание завершено.");

            // Если мы делали MusicStop(), то всё ок.
            // Если это была смена трека — нужно запустить новый трек после затухания.
            // Но в MusicNext/MusicPrevious мы уже вызвали PlayCurrentTrackByIndex ДО остановки.
            // Проблема: PlayCurrentTrackByIndex мог поставить новый клип, но он сразу начал играть с громкостью 0.
            // Чтобы после затухания пошёл новый трек, нужно либо:
            //   а) Запускать новый трек только ПОСЛЕ завершения FadeOut (сложнее).
            //   б) Или просто оставить логику как есть: новый клип уже назначен, и когда фейд закончится, громкость поднимется через FadeIn.
            
            // Вариант «б» проще: после FadeOut мы НЕ делаем Play(), потому что PlayCurrentTrackByIndex уже сделал Play().
            // А FadeIn запустится отдельно, если мы вызовем MusicPlay().
            
            // Для смены трека нам нужно, чтобы после FadeOut автоматически пошёл FadeIn для нового трека.
            // Поэтому здесь мы просто запускаем MusicPlay(), но аккуратно, чтобы не было двойного вызова.
            if (_currentTrackIndex >= 0 && _currentTrackIndex < _tracks.Length && _tracks[_currentTrackIndex].Clip != null)
            {
                // Запускаем плавный старт для текущего (нового) трека
                if (!_isFading)
                {
                    _isFading = true;
                    _targetVolume = ConfigManager._masterVolume.Value;
                    if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                    _fadeCoroutine = StartCoroutine(FadeInCoroutine());
                }
            }
        }
//======================================================================================================================//
        private System.Collections.IEnumerator LoadTracksRoutine()
        {
            Plugin.Log.LogInfo($"[Miyuki] Начинаю загрузку {_tracks.Length} треков...");

            foreach (var track in _tracks)
            {
                string url = "file:///" + track.Path.Replace('\\', '/');
                using (var request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        track.Clip = DownloadHandlerAudioClip.GetContent(request);
                        Plugin.Log.LogInfo($"[Miyuki] Загружен трек: {track.Name}");
                    }
                    else
                    {
                        Plugin.Log.LogInfo($"[Miyuki] Ошибка загрузки трека: {track.Path} | {request.error}");
                    }
                }
            }

            Plugin.Log.LogInfo("[Miyuki] Все треки загружены. Запускаю музыку.");
            MusicPlay();
        }
    }
}
//======================================================================================================================//