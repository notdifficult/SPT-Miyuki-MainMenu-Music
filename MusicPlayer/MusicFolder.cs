//======================================================================================================================//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
//using BepInEx;
//======================================================================================================================//
namespace MiyukiMainMenuMusic.MusicPlayer
{
    // Структура для JSON: папки и треки внутри них
    public class FolderInfo
    {
        public int FID;                                                                                                 // ID папки
        public string Name;                                                                                             // Имя папки (для отладки)
        public List<TrackInfo> Tracks = new List<TrackInfo>();                                                          // Треки внутри папки
        
    }
//======================================================================================================================//
    public class MusicDatabase
    {
        public List<FolderInfo> Folders = new List<FolderInfo>();
    }
//======================================================================================================================//
    public static class MusicFolder
    {
        private const string RelativeMusicPath = @"BepInEx\plugins\MiyukiMainMenuMusic\music";
        private static readonly string JsonFileName = "MusicInfo.json";
        
//======================================================================================================================//
        /// <summary>
        /// Главная функция: сканирует папку, создаёт/обновляет JSON, возвращает плоский список TrackInfo.
        /// Этот список потом передаётся в MusicPlayerMain.SetTracks().
        /// </summary>
        public static TrackInfo[] ScanMusic()
        {
            //string musicPath = Path.Combine(Paths.BepInExRootPath, RelativeMusicPath);
            string musicPath = @"BepInEx\plugins\MiyukiMainMenuMusic\music";
            string jsonPath = Path.Combine(musicPath, JsonFileName);

            // Создаём папку, если нет
            if (!Directory.Exists(musicPath))
            {
                Directory.CreateDirectory(musicPath);
                Debug.Log($"[Miyuki] Папка с музыкой создана: {musicPath}");
            }

            // 1. Пробуем прочитать JSON, если есть
            var db = LoadDatabase(jsonPath);
            if (db == null)
            {
                db = new MusicDatabase();
            }

            bool isDirty = false;                                                                                       // Флаг: нужно ли переписать JSON

            // Получаем файлы в корне (FID = 0)
            var rootFiles = Directory.GetFiles(musicPath, "*.mp3", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(musicPath, "*.MP3", SearchOption.TopDirectoryOnly))
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            // Получаем подпапки (для FID = 1, 2, 3...)
            var subFolders = Directory.GetDirectories(musicPath)
                .Where(d => !d.EndsWith(JsonFileName))
                .OrderBy(d => d)
                .ToArray();

            // Корневая папка (FID=0)
            FolderInfo rootFolder = db.Folders.FirstOrDefault(f => f.FID == 0);
            if (rootFolder == null)
            {
                rootFolder = new FolderInfo { FID = 0, Name = "root" };
                db.Folders.Add(rootFolder);
                isDirty = true;
            }

            // Обновляем треки в корневой папке
            rootFolder.Tracks.Clear();
            for (int i = 0; i < rootFiles.Length; i++)
            {
                var path = rootFiles[i];
                var name = Path.GetFileName(path);
                var track = new TrackInfo
                {
                    FID = 0,
                    MID = i + 1,
                    Path = path,
                    Name = name
                };
                rootFolder.Tracks.Add(track);
            }

            // Подпапки (FID=1,2,3...)
            for (int fIdx = 0; fIdx < subFolders.Length; fIdx++)
            {
                int fid = fIdx + 1;
                string folderPath = subFolders[fIdx];
                string folderName = Path.GetFileName(folderPath);

                FolderInfo folder = db.Folders.FirstOrDefault(x => x.FID == fid);
                if (folder == null)
                {
                    folder = new FolderInfo { FID = fid, Name = folderName };
                    db.Folders.Add(folder);
                    isDirty = true;
                }
                else
                {
                    // Если папка уже была, но имя изменилось — обновляем имя
                    if (folder.Name != folderName)
                    {
                        folder.Name = folderName;
                        isDirty = true;
                    }
                }

                folder.Tracks.Clear();
                var files = Directory.GetFiles(folderPath, "*.mp3", SearchOption.TopDirectoryOnly)
                    .Concat(Directory.GetFiles(folderPath, "*.MP3", SearchOption.TopDirectoryOnly))
                    .Distinct()
                    .OrderBy(p => p)
                    .ToArray();

                for (int tIdx = 0; tIdx < files.Length; tIdx++)
                {
                    var path = files[tIdx];
                    var name = Path.GetFileName(path);
                    var track = new TrackInfo
                    {
                        FID = fid,
                        MID = tIdx + 1,
                        Path = path,
                        Name = name
                    };
                    folder.Tracks.Add(track);
                }
            }

            // Удаляем папки, которых больше нет на диске
            var foldersToRemove = db.Folders
                .Where(f => f.FID != 0 && !subFolders.Any(sf => Path.GetFileName(sf) == f.Name))
                .ToList();

            foreach (var f in foldersToRemove)
            {
                db.Folders.Remove(f);
                isDirty = true;
            }

            // Сохраняем JSON, если структура изменилась
            if (isDirty)
            {
                SaveDatabase(db, jsonPath);
                Debug.Log("[Miyuki] MusicInfo.json обновлён.");
            }
            else
            {
                Debug.Log("[Miyuki] MusicInfo.json не изменился, используем существующую структуру.");
            }

            // Формируем плоский массив TrackInfo (сортировка по FID, внутри папки по MID)
            var flatList = new List<TrackInfo>();
            foreach (var folder in db.Folders.OrderBy(f => f.FID))
            {
                foreach (var track in folder.Tracks.OrderBy(t => t.MID))
                {
                    flatList.Add(track);
                }
            }

            Debug.Log($"[Miyuki] Найдено треков: {flatList.Count}");
            return flatList.ToArray();
        }
//======================================================================================================================//
        
        
        
        
        
//======================================================================================================================//  
        private static MusicDatabase LoadDatabase(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<MusicDatabase>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Miyuki] Ошибка чтения MusicInfo.json: {e.Message}");
                return null;
            }
        }
//======================================================================================================================//
        private static void SaveDatabase(MusicDatabase db, string path)
        {
            try
            {
                string json = JsonConvert.SerializeObject(db, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Miyuki] Ошибка записи MusicInfo.json: {e.Message}");
            }
        }
//======================================================================================================================//        
    }
}
