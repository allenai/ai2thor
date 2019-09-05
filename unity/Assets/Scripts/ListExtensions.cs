// MIT License
// 
// Copyright (c) 2017 Justin Larrabee <justonia@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class ListExtensions
{
	public static IList<T> Shuffle<T>(this IList<T> list) {
		return list.ShallowCopy().Shuffle_();
	}

    public static IList<T> Shuffle_<T>(this IList<T> list) {
		int n = list.Count;
		while (n > 1) {  
			n--;  
			int k = UnityEngine.Random.Range(0, n + 1); 
			T value = list[k];  
			list[k] = list[n];  
			list[n] = value;  
		}
		return list;
	}

	public static IList<T> Shuffle_<T>(this IList<T> list, int seed) {
		System.Random rng = new System.Random(seed);

		int n = list.Count;
		while (n > 1) {  
			n--;  
			int k = rng.Next(n + 1); 
			T value = list[k];  
			list[k] = list[n];  
			list[n] = value;  
		}
		return list;
	}

	public static IList<T> ShallowCopy<T>(this IList<T> listToCopy)
    {
        return listToCopy.Select(item => item).ToList();
    }
}
