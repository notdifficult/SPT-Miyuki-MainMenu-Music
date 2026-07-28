using UnityEngine;
using UnityEngine.UI; 
using System.IO;       
using MiyukiMainMenuMusic.Utils;
//using MiyukiMainMenuMusic.MusicPlayer;

namespace MiyukiMainMenuMusic.MusicPlayer
{
    public class MusicPlayerUI : MonoBehaviour
    {
        private GameObject _uiRoot;
        private Text _trackText;
        private Image _panelImage;
        
        private bool _isInitialized;
        private float _notificationTimer;
        private bool _forceShowNextTrack;

        public void Initialize()
        {
            if (_isInitialized) return;

            _uiRoot = new GameObject("MiyukiTrackNotification");
            DontDestroyOnLoad(_uiRoot);

            var canvas = _uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.enabled = true;

            // ВАЖНО: ScaleWithScreenSize + referenceResolution = автоподстройка под любой экран
            var scaler = _uiRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Панель (фон
            var panelObj = new GameObject("BackgroundPanel");
            panelObj.transform.SetParent(_uiRoot.transform, false);
            panelObj.SetActive(true);

            var rectPanel = panelObj.GetComponent<RectTransform>();
            if (rectPanel == null) rectPanel = panelObj.AddComponent<RectTransform>();

            // --- НАДЁЖНОЕ ПОЗИЦИОНИРОВАНИЕ В ЛЕВОМ НИЖНЕМ УГЛУ ---
            rectPanel.anchorMin = new Vector2(0, 0);
            rectPanel.anchorMax = new Vector2(0, 0);

            // Фиксированный размер панели в пикселях (не будет «сжиматься» на 2K)
            rectPanel.sizeDelta = new Vector2(320, 80);

            // Отступы от края экрана: 40px слева, 40px снизу
            rectPanel.anchoredPosition = new Vector2(260, 120);
            
            // Убираем любые внутренние отступы, которые могли схлопывать размер
            //rectPanel.offsetMin = Vector2.zero;
            //rectPanel.offsetMax = Vector2.zero;
 
            _panelImage = panelObj.GetComponent<Image>();
            if (_panelImage == null) _panelImage = panelObj.AddComponent<Image>();
            _panelImage.color = new Color(0, 0, 0, 0.8f); // чуть темнее для контраста
            
            // 3. Создаём объект текста
            var textObj = new GameObject("TrackText");
            textObj.transform.SetParent(panelObj.transform, false);
            textObj.SetActive(true);

            var rectText = textObj.GetComponent<RectTransform>();
            if (rectText == null) rectText = textObj.AddComponent<RectTransform>();

            // --- РАЗМЕР ТЕКСТА: занимаем почти всю панель, но с простым отступом ---
            // ВАЖНО: фиксированный размер текста, а не отрицательный sizeDelta
            // Это гарантирует, что текст не схлопнется в ноль
            // Фиксированный размер под 2 строки — чтобы точно влезло
            rectText.sizeDelta = new Vector2(290, 68);
            rectText.anchoredPosition = Vector2.zero;

            // Принудительно получаем компонент Text
            _trackText = textObj.GetComponent<Text>();
            if (_trackText == null) _trackText = textObj.AddComponent<Text>();

            // Надёжный шрифт, который точно отрисуется
            _trackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _trackText.fontSize = 12; // чуть крупнее для читаемости
            _trackText.alignment = TextAnchor.MiddleLeft;
            // Принудительно белый с полной непрозрачностью — чтобы точно был виден
            _trackText.color = new Color(1f, 1f, 1f, 1f);
            _trackText.supportRichText = true;
            _trackText.resizeTextForBestFit = false;
            _trackText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _trackText.verticalOverflow = VerticalWrapMode.Truncate;

            // Обводка для контраста
            var outline = textObj.GetComponent<Outline>();
            if (outline == null) outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);

            // Сразу ставим тестовый текст, чтобы убедиться, что он виден
            _trackText.text = "Сейчас играет: (MID) Название трека\nПлейлист: (FID) Название папки";
            
            _isInitialized = true;
            Plugin.Log.LogInfo("[Miyuki UI] Track notification initialized (bottom-left, Arial + Outline).");
        }




        // Передаём данные напрямую, а не через свойства CurrentTrackIndex/Tracks
        public void UpdateStatus(int currentTrackIndex, TrackInfo[] tracks)
        {
            if (!_isInitialized || _trackText == null || _panelImage == null) return;

            bool showNotification = ConfigManager._showTrackNotification.Value;
            float duration = ConfigManager._notificationDuration.Value;

            if (!showNotification)
            {
                // Если пользователь отключил уведомления — сразу скрываем всё
                _panelImage.color = new Color(_panelImage.color.r, _panelImage.color.g, _panelImage.color.b, 0f);
                _trackText.text = "";
                _notificationTimer = 0;
                return;
            }

            // Проверка валидности индекса
            if (currentTrackIndex < 0 || tracks == null || currentTrackIndex >= tracks.Length)
            {
                _panelImage.color = new Color(_panelImage.color.r, _panelImage.color.g, _panelImage.color.b, 0f);
                _trackText.text = "";
                _notificationTimer = 0;
                return;
            }

            var track = tracks[currentTrackIndex];
            string path = track.Path;

            string folderName = "Root";
            if (!string.IsNullOrEmpty(path))
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    folderName = Path.GetFileName(dir);
                }
            }


            string displayText = $"Сейчас играет: ({track.MID}) {track.Name}\nПлейлист: ({track.FID}) {folderName}";
            _trackText.text = displayText;

            // Если принудительно показываем уведомление (TriggerNotification) — сбрасываем таймер
            if (_forceShowNextTrack)
            {
                _notificationTimer = duration;
                _forceShowNextTrack = false;
            }

            // Плавное затухание: уменьшаем альфа канала панели и текста
            if (_notificationTimer > 0)
            {
                _notificationTimer -= Time.deltaTime;

                float alpha = Mathf.Clamp01(_notificationTimer / duration);

                var panelColor = _panelImage.color;
                panelColor.a = alpha * 0.75f; // 0.75 — это базовая прозрачность фона
                _panelImage.color = panelColor;

                var textColor = _trackText.color;
                textColor.a = alpha;
                _trackText.color = textColor;
            }
            else
            {
                // Таймер истёк — полностью скрываем
                var panelColor = _panelImage.color;
                panelColor.a = 0f;
                _panelImage.color = panelColor;

                var textColor = _trackText.color;
                textColor.a = 0f;
                _trackText.color = textColor;
            }
        }



        public void TriggerNotification()
        {
            _forceShowNextTrack = true;
        }

        public bool IsInitialized => _isInitialized;
    }
}
