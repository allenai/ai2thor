/*
* Copyright (c) <2018> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Generic serializable Dictionary.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	[System.Serializable]
	public class HEU_SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, UnityEngine.ISerializationCallbackReceiver
	{
		[System.NonSerialized]
		private Dictionary<TKey, TValue> _dictionary;

		[SerializeField]
		private TKey[] _keys;

		[SerializeField]
		private TValue[] _values;


		public TValue this[TKey key]
		{
			get
			{
				if(_dictionary == null)
				{
					throw new KeyNotFoundException();
				}
				return _dictionary[key];
			}
			set
			{
				if(_dictionary == null)
				{
					_dictionary = new Dictionary<TKey, TValue>();
				}
				_dictionary[key] = value;
			}
		}

		public ICollection<TKey> Keys
		{
			get
			{
				if(_dictionary == null)
				{
					_dictionary = new Dictionary<TKey, TValue>();
				}
				return _dictionary.Keys;
			}
		}

		public ICollection<TValue> Values
		{
			get
			{
				if(_dictionary == null)
				{
					_dictionary = new Dictionary<TKey, TValue>();
				}
				return _dictionary.Values;
			}
		}

		public int Count
		{
			get { return (_dictionary != null) ? _dictionary.Count : 0; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Add(TKey key, TValue value)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<TKey, TValue>();
			}
			_dictionary.Add(key, value);
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<TKey, TValue>();
			}
			(_dictionary as ICollection<KeyValuePair<TKey, TValue>>).Add(item);
		}

		public void Clear()
		{
			if(_dictionary != null)
			{
				_dictionary.Clear();
			}
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary == null)
			{
				return false;
			}
			return (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).Contains(item);
		}

		public bool ContainsKey(TKey key)
		{
			if (_dictionary == null)
			{
				return false;
			}
			return _dictionary.ContainsKey(key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (_dictionary == null)
			{
				return;
			}
			(_dictionary as ICollection<KeyValuePair<TKey, TValue>>).CopyTo(array, arrayIndex);
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			if (_dictionary == null)
			{
				return default(Dictionary<TKey, TValue>.Enumerator);
			}
			return _dictionary.GetEnumerator();
		}

		public bool Remove(TKey key)
		{
			if (_dictionary == null)
			{
				return false;
			}
			return _dictionary.Remove(key);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (_dictionary == null)
			{
				return false;
			}
			return (_dictionary as ICollection<KeyValuePair<TKey, TValue>>).Remove(item);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			if(_dictionary == null)
			{
				value = default(TValue);
				return false;
			}
			return _dictionary.TryGetValue(key, out value);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			if(_dictionary == null)
			{
				_dictionary = new Dictionary<TKey, TValue>();
			}
			return _dictionary.GetEnumerator();
		}

		public void OnAfterDeserialize()
		{
			if (_keys != null && _values != null)
			{
				// Read keys and values array into dictionary
				if(_dictionary == null)
				{
					_dictionary = new Dictionary<TKey, TValue>(_keys.Length);
				}
				else
				{
					_dictionary.Clear();
				}

				for(int i = 0; i < _keys.Length; ++i)
				{
					if(i < _values.Length)
					{
						_dictionary[_keys[i]] = _values[i];
					}
					else
					{
						_dictionary[_keys[i]] = default(TValue);
					}
				}
			}

			_keys = null;
			_values = null;
		}

		public void OnBeforeSerialize()
		{
			if (_dictionary == null || _dictionary.Count == 0)
			{
				_keys = null;
				_values = null;
			}
			else
			{
				// Copy dictionary into keys and values array
				int itemCount = _dictionary.Count;
				_keys = new TKey[itemCount];
				_values = new TValue[itemCount];

				int index = 0;
				var enumerator = _dictionary.GetEnumerator();
				while(enumerator.MoveNext())
				{
					_keys[index] = enumerator.Current.Key;
					_values[index] = enumerator.Current.Value;
					index++;
				}
			}
		}
	}

}   // HoudiniEngineUnity