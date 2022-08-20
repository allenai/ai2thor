using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class FastList<T> {

    /// <summary>
    /// Comparison function should return -1 if left is less than right, 1 if left is greater than right, and 0 if they match.
    /// </summary>
    public delegate int CompareFunc(T left, T right);


    public T[]      array = null;
    public int      size = 0;

    public FastList () {
    }

    public FastList(int size) {
        if (size > 0) {
            this.size = 0;
            array = new T[size];
        }
        else {
            this.size = 0;
        }
    }

    public int Count {
        get { return size;}
        set { }
    }

    public T this[int i] {
        get { return array[i];}
        set { array[i] = value;}
    }

    //Add item to end of list.
    public void Add(T item) {
        if (array == null || size == array.Length) {
            Allocate();
        }
        array[size] = item;
        size++;
    }

	//Add item to end of list if it is unique.
	public void AddUnique( T item ) {
		if ( array == null || size == array.Length ) {
			Allocate();
		}
		if ( !Contains( item ) ) {
			array[size] = item;
			size++;
		}
	}

	//Add items to the end of the list
	public void AddRange( IEnumerable<T> items ) {
		foreach ( T item in items ) {
			Add( item );
		}
	}

	//Insert item at specified index
	public void Insert(int index, T item) {
        if (array == null || size == array.Length) {
            Allocate();
        }
        if (index < size) {
            //move things back 1
            for (int i = size; i > index; i--) {
                array[i] = array[i-1];
            }
            array[index] = item;
            size++;
        }
        else Add(item);
    }

    //Removes specified item and keeps everything else in order
    public bool Remove(T item) {
        if (array != null) {
            for (int i = 0; i < size; i++) {
                if (item.Equals(array[i])) { //found it, push everything up
                    size--;
                    for (int j = i; j < size; j++) {
                        array[j] = array[j+1];
                    }
                    array[size] = default(T);
                    return true;
                }
            }
        }
        return false;
    }

    //Removes item at specified index while keeping everything else in order
    //O(n)
    public void RemoveAt(int index) {
        if (array != null && size > 0 && index < size) {
            size--;
            for (int i = index; i < size; i++) {
                array[i] = array[i+1];
            }
            array[size] = default(T);
        }
    }

    //Removes the specified item from the list and replaces with last item. Return true if removed, false if not found.
    public bool RemoveFast(T item) {
        if (array != null) {
            for (int i = 0; i < size; i++) {
				if ( item.Equals( array[i] )) { //found
                    //Move last item here
                    if (i < (size - 1)) {
                        T lastItem = array[size-1];
                        array[size-1] = default(T);
                        array[i] = lastItem;
                    } else {
                        array[i] = default(T);
                    }
                    size--;
                    return true;
                }
            }
        }
        return false;
    }

    //Removes item at specified index and replace with last item.
    public void RemoveAtFast(int index) {
        if (array != null && index < size && index >= 0) {
            //last element
            if (index == size - 1) {
                array[index] = default(T);
            }
            else {
                T lastItem = array[size - 1];
                array[index] = lastItem;
                array[size - 1] = default(T);
            }
            size--;

        }
    }

    //Return whether an item is contained within the list
    //O(n)
    public bool Contains(T item) {
        if (array == null || size <= 0 ) return false;
        for (int i = 0; i < size; i++) {
            if (array[i].Equals(item)) { return true;}
        }
        return false;
    }

    //Returns index of specified item, or -1 if not found.
    //O(n)
    public int IndexOf(T item) {
        if (size <= 0 || array == null) { return -1;}
        for (int i = 0; i < size; i++) {
            if (item.Equals(array[i])) { return i;}
        }
        return -1;
    }

    public T Pop() {
        if (array != null && size > 0) {
            T lastItem = array[size-1];
            array[size-1] = default(T);
            size--;
            return lastItem;
        }

        return default(T);
    }

    public T[] ToArray() {
        Trim();
        return array;
    }

    public void Sort (CompareFunc comparer) {
        int start = 0;
        int end = size - 1;
        bool changed = true;

        while (changed) {
            changed = false;

            for (int i = start; i < end; i++) {

                if (comparer(array[i], array[i + 1]) > 0) {
                    T temp = array[i];
                    array[i] = array[i+1];
                    array[i+1] = temp;
                    changed = true;
                }
                else if (!changed) {
                    start = (i==0) ? 0 : i-1;
                }
            }
        }
    }

    public void InsertionSort(CompareFunc comparer) {
        for (int i = 1; i < size; i++) {
            T curr = array[i];
            int j = i;
            while (j > 0 && comparer(array[j - 1], curr) > 0) {
                array[j] = array[j-1];
                j--;
            }
            array[j] = curr;
        }
    }

    public IEnumerator<T> GetEnumerator() {
        if (array != null) {
            for (int i = 0; i < size; i++) {
                yield return array[i];
            }
        }
    }

    public T Find(Predicate<T> match) {
        if (match != null) {
            if (array != null) {
                for (int i = 0; i < size; i++) {
                    if (match(array[i])) { return array[i];}
                }
            }
        }
        return default(T);
    }
    
    //Allocate more space to internal array. 
    void Allocate() {
        T[] newArray;
        if (array == null) {
            newArray = new T[32];
        }
        else {
            newArray = new T[Mathf.Max(array.Length << 1, 32)];
        }
        if (array != null && size > 0) {
            array.CopyTo(newArray, 0);
        }

        array = newArray;
    }


    void Trim() {
        if (size > 0) {
            T[] newArray = new T[size];
            for (int i = 0; i < size; i++) {
                newArray[i] = array[i];
            }
            array = newArray;
        }
        else {
            array = null;
        }
    }

    //Set size to 0, does not delete array from memory
    public void Clear() {
        size = 0;
    }

    //Delete array from memory
    public void Release() {
        Clear();
        array = null;
    }



}
