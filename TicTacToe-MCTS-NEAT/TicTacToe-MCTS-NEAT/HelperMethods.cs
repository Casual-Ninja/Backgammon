using System;
using System.Collections.Generic;

namespace HelperSpace
{
    public static class HelperMethods
    {
        private static Random privateRandom = new Random();
        public static int GetRandomSeed()
        {
           return RandomValue(0, int.MaxValue, privateRandom);
        } 

        public static int RandomValue(int minInclusive, int maxExclusive, Random rnd)
        {
            return rnd.Next(minInclusive, maxExclusive);
        }

        public static double RandomValue(double min, double max, Random rnd)
        {
            return rnd.NextDouble() * (max - min) + min;
        }

        public static float RandomValue(float min, float max, Random rnd)
        {
            return (float)rnd.NextDouble() * (max - min) + min;
        }

        public static T GetRandomFromList<T>(List<T> list, Random rnd)
        {
            return list[rnd.Next(list.Count)];
        }

        public static T GetRandomFromArray<T>(T[] array, Random rnd)
        {
            return array[rnd.Next(array.Length)];
        }

        public static int GetRandomIndexFromList<T>(List<T> list, Random rnd)
        {
            return rnd.Next(list.Count);
        }

        public static T[] CopyArray<T>(T[] array)
        {
            T[] copy = new T[array.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = array[i];
            return copy;
        }

        public static string ArrayToString<T>(T[] array)
        {
            if (array == null)
                return "null";
            string s = "[";

            foreach (T t in array)
            {
                s += $" {t.ToString()},";
            }
            s = s.Substring(0, s.Length - 1);
            return s + " ]";
        }

        public static string ListToString<T>(List<T> list)
        {
            if (list.Count == 0)
                return "[ ]";
            string s = "[";

            foreach (T t in list)
            {
                s += $" {t.ToString()},";
            }
            s = s.Substring(0, s.Length - 1);
            return s + " ]";

        }

        public static string ListToString<T>(List<List<T>> list)
        {
            string s = "[";

            foreach (List<T> t in list)
            {
                s += " { " + ListToString(t) + " },";
            }
            s = s.Substring(0, s.Length - 1);
            return s + " ]";
        }

        public static string DictionaryToString<T1, T2>(Dictionary<T1, T2> dict)
        {
            string s = "";

            foreach (KeyValuePair<T1, T2> pair in dict)
            {
                s += $"{pair.Key.ToString()}={pair.Value.ToString()}, ";
            }

            return s;
        }

        public static int InsertToList(List<byte> list, byte newItem)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] >= newItem)
                {
                    list.Insert(i, newItem);
                    return i;
                }
            list.Add(newItem);
            return list.Count - 1;
        }

        public static int InsertToList(List<byte> list, int startSearch, byte newItem)
        {
            for (; startSearch < list.Count; startSearch++)
            {
                if (list[startSearch] >= newItem)
                {
                    list.Insert(startSearch, newItem);
                    return startSearch;
                }
            }
            list.Add(newItem);
            return startSearch; // equals to list.count - 1
        }

        public static int RemoveFromList(List<byte> list, byte removeItem)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == removeItem)
                {
                    list.RemoveAt(i);
                    return i;
                }
            }
            return -1;
        }

        public static int RemoveFromList(List<byte> list, int startSearch, byte removeItem)
        {
            for (; startSearch < list.Count; startSearch++)
            {
                if (list[startSearch] == removeItem)
                {
                    list.RemoveAt(startSearch);
                    return startSearch;
                }
            }
            return -1;
        }

        public static bool HoldSameItems<T>(List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count) // have to be same length
            {
                Console.WriteLine($"Not same length: {list1.Count} vs {list2.Count}");
                return false;
            }

            List<T> copyList2 = new List<T>(list2); // copy it so i don't destroy it

            foreach (T item in list1)
            {
                // check if this items exists in copy
                bool found = false;
                for (int j = 0; j < copyList2.Count; j++)
                {
                    if (item.Equals(copyList2[j]))
                    {
                        found = true;
                        copyList2.RemoveAt(j);
                        break;
                    }
                }
                if (!found)
                {
                    Console.WriteLine("not found: " + item.ToString());

                    Console.WriteLine("The second list:");
                    Console.WriteLine(ListToString(list2));


                    return false;
                }
            }
            return true;
        }

        public static bool HoldSameItems<T>(List<List<T>> list1, List<List<T>> list2)
        {
            if (list1.Count != list2.Count) // have to be same length
                return false;

            List<List<T>> copyList2 = new List<List<T>>(list2); // copy it so i don't destroy it

            foreach (List<T> item in list1)
            {
                // check if this items exists in copy
                bool found = false;
                for (int j = 0; j < copyList2.Count; j++)
                {
                    if (HoldSameItems(item, copyList2[j]))
                    {
                        found = true;
                        copyList2.RemoveAt(j);
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        public static void QuickSort<T>(int[] array, params T[][] arrays)
        {
            QuickSort(array, 0, array.Length - 1, arrays);
        }

        // A utility function to swap two elements
        private static void Swap<T>(int[] arr, int i, int j, params T[][] arrays)
        {
            int temp = arr[i];
            arr[i] = arr[j];
            arr[j] = temp;

            if (arrays != null)
            {
                foreach (T[] array in arrays)
                {
                    T temp2 = array[i];
                    array[i] = array[j];
                    array[j] = temp2;
                }
            }
        }

        /* This function takes last element as pivot, places
        the pivot element at its correct position in sorted
        array, and places all smaller (smaller than pivot)
        to left of pivot and all greater elements to right
        of pivot */
        private static int Partition<T>(int[] arr, int low, int high, params T[][] arrays)
        {
            // pivot
            int pivot = arr[high];

            // Index of smaller element and
            // indicates the right position
            // of pivot found so far
            int i = (low - 1);

            for (int j = low; j <= high - 1; j++)
            {
                // If current element is smaller
                // than the pivot
                if (arr[j] < pivot)
                {
                    // Increment index of
                    // smaller element
                    i++;
                    Swap(arr, i, j, arrays);
                }
            }
            Swap(arr, i + 1, high, arrays);
            return (i + 1);
        }

        /* 
        The main function that implements QuickSort
        arr[] --> Array to be sorted,
        low --> Starting index,
        high --> Ending index
        */
        private static void QuickSort<T>(int[] arr, int low, int high, params T[][] arrays)
        {
            if (low < high)
            {
                // pi is partitioning index, arr[p]
                // is now at right place
                int pi = Partition(arr, low, high, arrays);

                // Separately sort elements before
                // partition and after partition
                QuickSort(arr, low, pi - 1, arrays);
                QuickSort(arr, pi + 1, high, arrays);
            }
        }

        public static void FlipArray<T>(T[] array)
        {
            if (array == null)
                return;

            T[] newArray = new T[array.Length];

            for (int i = 0; i < newArray.Length; i++)
                newArray[i] = array[i];

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = newArray[array.Length - 1 - i];
            }
        }
    }
}
