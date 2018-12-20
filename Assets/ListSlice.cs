using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArraySlice<T> {
    public int lowerBound;
    public int upperBound;
    public T[] array;

    public int length {
        get { return upperBound - lowerBound; }
    }

    public ArraySlice(T[] array, int lowerBound, int upperBound) {
        this.array = array; 
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;

    }
    public T this [int subscript] {
        get {
            int idx = subscript - lowerBound;
            if (idx < 0 || idx > upperBound) {
                Debug.LogError("Index ouf of slice bounds! index: " + subscript + " lower bound: " + lowerBound + " upper bound: " + upperBound);
            }
            return array[idx];
        }
        set {
            int idx = subscript - lowerBound;
            if (idx < 0 || idx > upperBound) {
                Debug.LogError("Index ouf of slice bounds! index: " + subscript + " lower bound: " + lowerBound + " upper bound: " + upperBound);
            }
            array[idx] = value;
        }
    }
}

public struct ListSlice<T> {
    public int lowerBound;
    public int upperBound;
    public List<T> list;

    public int length {
        get { return upperBound - lowerBound + 1; }
    }
    public ListSlice(List<T> list, int lowerBound, int upperBound) {
        this.list = list; 
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
    }
    public T this [int subscript] {
        get {
            int idx = subscript + lowerBound;
            return list[idx];
        }
        set {
            int idx = subscript + lowerBound;
            list[idx] = value;
        }
    }
}