using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

namespace TicTacToe_MCTS_NEAT
{
    public static class SaveLoad
    {
        private static SaveLoadInstance instance = new SaveLoadInstance();
        private const string dataPath = "D:\\Users\\Owner\\GitHubRepos\\DataSave\\";

        private static string GetActualPath(string path)
        {
            if (path.StartsWith(dataPath))
                return path;
            return dataPath + path;
        }

        public static void SaveData(string path, object data)
        {
            path = GetActualPath(path);
            lock (instance)
            {
                instance.SaveData(path, data);
            }
        }

        public static bool LoadData<T>(string path, out object loadedValue)
        {
            path = GetActualPath(path);
            lock (instance)
            {
                return instance.LoadData<T>(path, out loadedValue);
            }
        }

        private static void SaveAllDataToDisk()
        {
            lock (instance)
            {
                instance.SaveAllDataToDisk();
            }
        }

        public static void SaveAllDataToDiskInThread()
        {
            Thread saveThread = new Thread(SaveAllDataToDisk);
            saveThread.Start();
        }

        public static string[] GetAllFilesInDirectory(string path)
        {
            path = GetActualPath(path);
            System.Console.WriteLine(path);
            string[] paths;
            try
            {
                paths = Directory.GetFiles(path);
                return paths;
            }
            catch
            {
                return null;
            }
        }

        private class SaveLoadInstance
        {
            private static Dictionary<string, (bool, object)> saveDictionary = new Dictionary<string, (bool, object)>();

            private void MakeSurePathExists(string path)
            {
                if (PathExists(path))
                    return;
                System.Console.WriteLine("IDK PATH:" + path);
                int index = path.LastIndexOf('\\');
                string prevPath = path.Substring(0, index);
                Directory.CreateDirectory(prevPath);
            }

            private bool SaveToDisk<T>(string path, T value)
            {
                MakeSurePathExists(path);
                System.Console.WriteLine($"The value type {value.GetType()}, save to disk: {value}");
                string jsonText = JsonSerializer.Serialize(value);
                System.Console.WriteLine("The saved data: " + jsonText);
                File.WriteAllText(path, jsonText);
                return true;
            }

            private bool LoadFromDisk<T>(string path, out object loadedValue)
            {
                System.Console.WriteLine("Loading from path: " + path);
                if (PathExists(path))
                {
                    string jsonText = File.ReadAllText(path);
                    loadedValue = JsonSerializer.Deserialize<T>(jsonText);
                    return true;
                }
                loadedValue = default(T);
                return false;
            }

            private bool PathExists(string path)
            {
                return File.Exists(path);
            }

            public void SaveData(string path, object data)
            {
                if (saveDictionary.ContainsKey(path))
                    saveDictionary[path] = (false, data);
                else
                    saveDictionary.Add(path, (false, data));
            }

            public bool LoadData<T>(string path, out object loadedValue)
            {
                if (saveDictionary.ContainsKey(path)) // do i already contain this data
                {
                    loadedValue = saveDictionary[path];
                    return true;
                }
                else // i dont...
                {
                    if (LoadFromDisk<T>(path, out loadedValue)) // try load the data from the disk
                    {
                        saveDictionary.Add(path, (true, loadedValue));
                        return true;
                    }
                    else
                        return false;
                }
            }

            public void SaveAllDataToDisk()
            {
                var keys = saveDictionary.Keys;
                List<(string, object)> changeStuff = new List<(string, object)>();
                foreach (string key in keys)
                {
                    (bool, object) value = saveDictionary[key];
                    if (value.Item1 == false)
                    {
                        SaveToDisk(key, value.Item2);
                        changeStuff.Add((key, value.Item2));
                    }
                }
                foreach ((string, object) item in changeStuff)
                    saveDictionary[item.Item1] = (true, item.Item2);
            }
        }
    }
}
