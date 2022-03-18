using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

namespace TicTacToe_MCTS_NEAT
{
    public static class SaveLoad
    {
        private static SaveLoadInstance instance = new SaveLoadInstance();

        private static string GetActualPath(string path)
        {
            return "C:\\Users\\Owner\\source\\repos\\TicTacToe - MCTS - NEAT\\Server\\" + path;
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

            private bool SaveToDisk<T>(string path, T value)
            {
                string jsonText = JsonSerializer.Serialize(value);
                File.WriteAllText(path, jsonText);
                return true;
            }

            private bool LoadFromDisk<T>(string path, out object loadedValue)
            {
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
                foreach (KeyValuePair<string, (bool, object)> saveValues in saveDictionary)
                {
                    if (saveValues.Value.Item1 == false)
                    {
                        SaveToDisk(saveValues.Key, saveValues.Value.Item2);
                        saveDictionary[saveValues.Key] = (true, saveValues.Value.Item2);
                    }
                }
            }
        }
    }
}
