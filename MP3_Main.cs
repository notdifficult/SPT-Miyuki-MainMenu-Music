//====================================================================================================================//
using BepInEx;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UnityEngine.Networking;
using System.Collections;
using Random = UnityEngine.Random;
//====================================================================================================================//
namespace NoTDifficult.MiyukiMainMenuMusic
{
    [BepInPlugin(PluginsInfo.GUID, PluginsInfo.NAME, PluginsInfo.VERSION)]                                              //[BepInPlugin("NoTDifficult.MiyukiMainMenuMusic", "MiyukiMainMenuMusic", "1.0.0")]
//====================================================================================================================//
    public class MP3_Main : BaseUnityPlugin
    {
        private readonly string _musicFolderPath = @"BepInEx\plugins\MiyukiMainMenuMusic\music";
        
        private AudioSource _audioSource;
        private bool _isLoading;
        private string _lastPlayedPath = "";
        private string[] _mp3Files;                                                                                     // Список всех MP3-файлов в папке
        private int _currentTrackIndex = -1;                                                                            // НОВЫЙ: индекс текущего трека в массиве _mp3Files
        private bool _userRequestedChange = false;                                                                      // Флаг: пользователь запросил смену трека вручную
        
        private void Awake()
        {
            MP3_Settings.Initialize(Config);

            string fullPath = Path.Combine(AppContext.BaseDirectory, _musicFolderPath);                                 // Logger.LogInfo($"[MiyukiMainMenuMusic] Ищем музыку в: {fullPath}");

            if (Directory.Exists(fullPath))
            {
                _mp3Files = Directory.GetFiles(fullPath, "*.mp3")                                            // Logger.LogInfo($"Найдено MP3 файлов: {_mp3Files.Length}");
                    .Where(f => !string.IsNullOrEmpty(f))
                    .ToArray();
                
                if (_mp3Files.Length == 0)
                    Logger.LogWarning("[MiyukiMainMenuMusic] В папке нет MP3 файлов! Звук не будет играть.");
            }
            else
            {
                Logger.LogError($"[MiyukiMainMenuMusic] Папка не найдена: {fullPath}. Музыка не будет играть.");
                _mp3Files = System.Array.Empty<string>();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            UpdateCurrentSceneInfo();                                                                                   // Инициализируем начальное значение сцены
        }
//====================================================================================================================//
        private void Update()
        {
            if (Input.GetKeyDown(MP3_Settings.PreviousTrackKey.Value))
                PlayPrevTrack();

            if (Input.GetKeyDown(MP3_Settings.NextTrackKey.Value))
                PlayNextTrack();
            
            if (_mp3Files == null || _mp3Files.Length == 0 || _audioSource == null)                                  // Защита: если нет файлов или аудио источника — не проверяем клавиши
                return;
        }
//====================================================================================================================//        
        private void UpdateCurrentSceneInfo()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            MP3_Settings.CurrentScene.Value = currentSceneName;                                                         // Обновляем значение в конфиге (оно сохранится в config.cfg)
            Logger.LogDebug($"[MiyukiMainMenuMusic] Текущая сцена обновлена: {currentSceneName}");
        }
//====================================================================================================================//
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            string name = scene.name;
            Logger.LogDebug($"[MiyukiMainMenuMusic] Загружена сцена: '{name}'");
            
            UpdateCurrentSceneInfo();                                                                                   // Сначала обновляем информацию о сцене
            var ignoredPatterns = MP3_Settings.GetIgnoredScenePatterns();                                         // Получаем актуальный список игнорируемых сцен из настроек
            bool isIgnored = ignoredPatterns.Any(p => name.Contains(p, System.StringComparison.OrdinalIgnoreCase));// Проверка на игнорируемые сцены
            if (isIgnored)
            {
                Logger.LogInfo($"[MiyukiMainMenuMusic] '{name}' в списке игнорируемых — музыка не запускается/останавливается.");
                if (_audioSource != null)
                {
                    _audioSource = null;                                                                                // Logger.LogInfo($"'{name}' в списке игнорируемых!! _audioSource = null;");
                }
                _userRequestedChange = false;
                return;
            }
            
            string allowedSuffix = MP3_Settings.AllowedSceneSuffix.Value;                                               // Используем суффикс из конфига вместо жёстко заданного значения
            bool isAllowed = !string.IsNullOrEmpty(allowedSuffix) && name.EndsWith(allowedSuffix, System.StringComparison.OrdinalIgnoreCase);

            if (isAllowed)
            {
                if (_audioSource != null)
                {
                    Logger.LogDebug($"[MiyukiMainMenuMusic] Сцена '{name}' разрешена, музыка продолжает играть.");
                    return;
                }

                Logger.LogInfo($"[MiyukiMainMenuMusic] Сцена '{name}' загружена — запускаем музыку.");
                _currentTrackIndex = -1;
                StartCoroutine(PlayRandomTrackLoop(name));
            }
            else
            {
                if (_audioSource != null)
                {
                    _audioSource = null;
                    Logger.LogInfo($"[MiyukiMainMenuMusic] '{name}' не является UI сценой — звук остановлен.");
                }
                _userRequestedChange = false;                                                                           // Сбрасываем флаг при смене сцены
            }
            UpdateCurrentSceneInfo(); 
        }
//======================================================================================================================// Воспроизводит предыдущий трек (с зацикливанием)
        public void PlayPrevTrack()
        {
            if (_mp3Files == null || _mp3Files.Length == 0 || _audioSource == null) return;
            _userRequestedChange = true;                                                                                // Блокируем автоматический случайный выбор в цикле
            // Логика зацикливания: если текущий 0, то предыдущий - это последний
            if (_currentTrackIndex <= 0)
                _currentTrackIndex = _mp3Files.Length - 1;
            else
                _currentTrackIndex--;
            
            StartCoroutine(LoadAndPlaySingleTrack(_mp3Files[_currentTrackIndex]));                                // Logger.LogInfo($"Переключение на предыдущий трек: {Path.GetFileName(_mp3Files[_currentTrackIndex])}");
        }
        // Воспроизводит следующий трек (с зацикливанием)
        public void PlayNextTrack()
        {
            if (_mp3Files == null || _mp3Files.Length == 0 || _audioSource == null) return;
            _userRequestedChange = true;                                                                                // Блокируем автоматический случайный выбор в цикле
            _currentTrackIndex++;
            if (_currentTrackIndex >= _mp3Files.Length)
                _currentTrackIndex = 0;
            
            StartCoroutine(LoadAndPlaySingleTrack(_mp3Files[_currentTrackIndex]));                                // Logger.LogInfo($"Переключение на следующий трек: {Path.GetFileName(_mp3Files[_currentTrackIndex])}");
        }
//======================================================================================================================// Цикл: играем случайный трек → ждём конца → играем следующий случайный
        private IEnumerator PlayRandomTrackLoop(string sceneName)
        {
            if (_isLoading) yield break;
            _isLoading = true;
            
            var go = new GameObject("UISoundPlayer_" + sceneName);                                                 // Создаём объект для звука (удалится вместе со сценой)
            _audioSource = go.AddComponent<AudioSource>();
            _audioSource.volume = 0f;                                                                                   // Начинаем с 0 для плавного старта
            _audioSource.loop = false;                                                                                  // Важно: не зацикливаем сам клип
            _audioSource.spatialBlend = 0f;                                                                             // 2D звук
            _audioSource.playOnAwake = false;

            while (true)
            {
                if (!_userRequestedChange)                                                                              // Если пользователь только что нажал кнопку (F5/F6), мы НЕ выбираем случайный трек. Цикл просто ждёт, пока закончится текущий трек, который уже запущен кнопкой.
                {
                    string nextPath = GetRandomTrackPath();
                    if (string.IsNullOrEmpty(nextPath))
                    {
                        Logger.LogWarning("[MiyukiMainMenuMusic] Нет доступных треков для воспроизведения.");
                        break;
                    }

                    yield return StartCoroutine(LoadAndPlaySingleTrack(nextPath));
                }
                else                                                                                                    // Если была команда пользователя, сбрасываем флаг, чтобы следующий проход цикла мог снова выбрать случайный, если пользователь больше не нажимает кнопки.
                {
                    _userRequestedChange = false;
                    
                    while (_audioSource != null && _audioSource.isPlaying)                                           // Просто ждём окончания текущего трека (он уже играет благодаря кнопке)
                    {
                        yield return null;
                    }
                }
                if (_audioSource == null) break;                                                                     // Если аудио источник был уничтожен (сцена сменилась) — выходим из цикла
                
                yield return new WaitForSeconds(0.5f);                                                                  // Небольшая пауза перед следующим треком (опционально)
            }

            _isLoading = false;
        }
//======================================================================================================================// Выбираем случайный трек (не тот, что только что играл)
        private string GetRandomTrackPath()
        {
            if (_mp3Files == null || _mp3Files.Length == 0) return "";

            var candidates = _mp3Files
                .Where(p => !string.Equals(p, _lastPlayedPath, System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (candidates.Length == 0)
                candidates = _mp3Files;

            int index = Random.Range(0, candidates.Length);
            _lastPlayedPath = candidates[index];
            _currentTrackIndex = System.Array.IndexOf(_mp3Files, _lastPlayedPath);
            return candidates[index];
        }
//======================================================================================================================// Загружает и играет один трек, ждёт его окончания
        private IEnumerator LoadAndPlaySingleTrack(string path)
        {
            if (_audioSource == null) yield break;                                                                   // Быстрая проверка: если источник уже удалён, выходим сразу

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                yield break;                                                                                            //Logger.LogError($"Файл не найден: {fullPath}");
            }

            string uri = "file:///" + fullPath.Replace("\\", "/");

            using var uwr = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG);
            yield return uwr.SendWebRequest();
            
            if (_audioSource == null) yield break;                                                                   // КРИТИЧЕСКИ ВАЖНО: проверяем, жив ли AudioSource после ожидания загрузки

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(uwr);

                if (_audioSource == null || clip == null || clip.length <= 0) yield break;                        // Ещё одна проверка перед присваиванием
                _audioSource.clip = clip;
                _lastPlayedPath = path;                                                                                 // Запоминаем последний игравший путь
                Logger.LogInfo($"[MiyukiMainMenuMusic] Воспроизводим трек: {Path.GetFileName(fullPath)} ({clip.length:F1} сек)");
                _audioSource.Play();
                _audioSource.volume = 0f;
                
                
                float targetVolume = MP3_Settings.MusicVolume.Value;                                                    // Плавное нарастание до значения из конфига
                float duration = Mathf.Max(0.1f, MP3_Settings.FadeInDuration.Value);
                float elapsed = 0f;                                                                                     // Плавное нарастание громкости
                
                while (elapsed < duration && _audioSource != null)                                                   // while (elapsed < FadeInDuration && _audioSource != null)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    _audioSource.volume = Mathf.Lerp(0f, targetVolume, t);                                              //_audioSource.volume = Mathf.Lerp(0f, 1f, t); теперь конфиг
                    yield return null;
                }
                
                if (_audioSource != null)
                    _audioSource.volume = targetVolume;                                                                 // _audioSource.volume = 0.5f; теперь конфиг
                
                while (_audioSource != null && _audioSource.isPlaying &&                                             // Ждём окончания трека, но постоянно проверяем, не удалили ли сцену
                       _audioSource.time < _audioSource.clip.length)
                {
                    yield return null;
                }
            }
            else
            {
                Logger.LogError($"[MiyukiMainMenuMusic] Ошибка загрузки: {uwr.error}");
            }
        }
    }
}
//====================================================================================================================//