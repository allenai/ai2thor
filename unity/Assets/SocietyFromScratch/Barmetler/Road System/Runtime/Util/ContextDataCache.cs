using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler
{
	public abstract class InValidatable
	{
		public void Invalidate(Stack<InValidatable> callStack = null)
		{
			callStack ??= new Stack<InValidatable>();
			if (callStack.Contains(this)) return;
			OnInvalidate();
			callStack.Push(this);
			foreach (var child in children)
				child.Invalidate(callStack);
			callStack.Pop();
		}

		public abstract void OnInvalidate();

		/// <summary>
		/// These objects will also be invalidated if this one is. Looping children are not a problem.
		/// </summary>
		public readonly List<InValidatable> children = new List<InValidatable>();
	}

	/// <summary>
	/// Caches data depending on some context. Good for preventing expensive computations, if the data it depends on has not been changed.
	/// </summary>
	public class ContextDataCache<DataType, ContextType> : InValidatable
	{
		Dictionary<int, DataType> data = new Dictionary<int, DataType>();

		public void SetData(DataType data, ContextType context)
		{
			this.data[context.GetHashCode()] = data;
		}

		public DataType GetData(ContextType context)
		{
			if (!IsValid(context)) throw new System.Exception("Cache is invalid");
			return data[context.GetHashCode()];
		}

		override public void OnInvalidate()
		{
			data.Clear();
		}

		public bool IsValid(ContextType context)
		{
			return data.ContainsKey(context.GetHashCode());
		}
	}
}