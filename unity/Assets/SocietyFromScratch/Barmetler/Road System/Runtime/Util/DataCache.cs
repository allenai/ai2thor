using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler
{
	public class DataCache<T> : InValidatable
	{
		T data;
		bool valid = false;

		public void SetData(T data)
		{
			this.data = data;
			valid = true;
		}

		public T GetData()
		{
			if (!IsValid()) throw new System.Exception("Cache is invalid");
			return data;
		}

		override public void OnInvalidate()
		{
			valid = false;
		}

		public bool IsValid()
		{
			return valid;
		}
	}
}