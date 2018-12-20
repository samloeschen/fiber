using System;

using UnityEngine;
using System.Collections;

public static class ArrayExtensions {

    public static T GetRandomElement<T>(this T[] array) {
        return array[Mathf.FloorToInt(UnityEngine.Random.value * array.Length)];
    }

    public static int Count<T>(this T[] array, Func<T, bool> countPredicate) {
        var count = 0;
        for (int i = 0; i < array.Length; i++) {
            if (countPredicate(array[i]))
                count++;
        }
        return count;
    }

    public static U[] Map<T, U>(this T[] array, Func<T, U> action) {
        var newArray = new U[array.Length];
        for (int i = 0; i < array.Length; i++) {
            newArray[i] = action(array[i]);
        }
        return newArray;
    }

    public static void ForEach<T>(this T[] array, Action<T> action) {
        for (int i = 0; i < array.Length; i++) {
            action(array[i]);
        }
    }

    public static int IndexOf<T>(this T[] array, T item) {
        for (int i = 0; i < array.Length; i++) {
            if (array[i].Equals(item))
                return i;
        }
        return -1;
    }

    public static bool Contains<T>(this T[] array, T item) {
        for (int i = 0; i < array.Length; i++) {
            if (array[i].Equals(item))
                return true;
        }
        return false;
    }

    public static bool Contains<TArray, TItem>(this TArray[] array, TItem item, Func<TArray, TItem, bool> comparator) {
        for (int i = 0; i < array.Length; i++) {
            if (comparator(array[i], item))
                return true;
        }
        return false;
    }

    public static bool ContainsIgnoreCase(this string[] array, string item) {
        for (int i = 0; i < array.Length; i++) {
            if (String.Compare(array[i], item, StringComparison.OrdinalIgnoreCase) == 0)
                return true;
        }
        return false;
    }

    public static T[] Shuffle<T>(this T[] array){
        for(int i = array.Length - 1; i > 0; i--){
            var j = Mathf.FloorToInt(UnityEngine.Random.value * (i + 1));
            var tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }
        return array;
    }
    public static T[] ShuffleRange<T>(this T[] array, int from, int to){
        to = UnityEngine.Mathf.Min(to, array.Length);
        for(int i = to - 1; i > from; i--){
            var j = Mathf.Clamp(Mathf.FloorToInt(UnityEngine.Random.value * (i + 1)), from, to);
            var tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;  
        }
        return array;
    }
}
