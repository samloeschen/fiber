﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ListDict<TKey, TValue> {
    public readonly List<TKey> Keys;
    public readonly List<TValue> Values;
    public int Count { get { return Keys.Count; } }

    public ListDict(int capacity = 0) {
        Keys = new List<TKey>(capacity);
        Values = new List<TValue>(capacity);
    }

    public void Add(TKey key, TValue value) {
        var index = Keys.IndexOf(key);
        if (index < 0) {
            Keys.Add(key);
            Values.Add(default(TValue));
            index = Keys.Count - 1;
        }
        Values[index] = value;
    }

    public bool Remove(TKey key) {
        return RemoveAt(Keys.IndexOf(key));
    }

    public bool RemoveAt(int index) {
        if (index < 0 || index >= Count) {
            return false;
        }
        Keys.RemoveAt(index);
        Values.RemoveAt(index);
        return true;
    }

    public void Clear() {
        Keys.Clear();
        Values.Clear();
    }

    public bool ContainsKey(TKey key) {
        return Keys.IndexOf(key) >= 0;
    }
    
    public TValue this[TKey key] {
        get { return Values[Keys.IndexOf(key)]; }
        set { Add(key, value); }
    }

    public override string ToString() {
        return string.Format("Count = {0}", Count);
    }
}