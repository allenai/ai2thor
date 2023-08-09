using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace Barmetler
{
	/// <summary>
	/// Updates a value when you call the Update function, but asynchronously.
	/// <para>This will make sure that the update was executed after the last time it was called, but only as often as necessary.</para>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class AsyncUpdater<T>
	{
		private T data;
		readonly System.Func<T> updater;

		readonly MonoBehaviour mb;
		readonly object dispatcherLock = new object();
		readonly object dataLock = new object();
		private bool coroutineRunning = false;
		private bool updateQueued = false;
		readonly float interval = 0;
		readonly Stopwatch sw = new Stopwatch();

		public AsyncUpdater(MonoBehaviour mb, System.Func<T> updater, T initialData, float interval = 0)
		{
			this.mb = mb;
			this.updater = updater;
			this.interval = interval;
			data = initialData;
		}

		public AsyncUpdater(MonoBehaviour mb, System.Func<T> updater)
		{
			this.mb = mb;
			this.updater = updater;
		}

		/// <summary>
		/// Will make sure that the updater is called at some point in the future.
		/// </summary>
		public void Update()
		{
			updateQueued = true;
			MaybeDispatchCoroutine();
		}

		/// <summary>
		/// Get current Data.
		/// </summary>
		public T GetData()
		{
			T d;
			lock (dataLock)
				d = data;
			return d;
		}

		private void MaybeDispatchCoroutine()
		{
			lock (dispatcherLock)
			{
				if (!coroutineRunning && updateQueued)
				{
					updateQueued = false;
					coroutineRunning = true;
					mb.StartCoroutine(CallUpdater());
				}
			}
		}

		private IEnumerator CallUpdater()
		{
			sw.Restart();
			var newData = updater();
			sw.Stop();
			float secondsToWait = (float)(interval - sw.ElapsedMilliseconds / 1e6);
			if (secondsToWait > 0)
				yield return new WaitForSeconds(secondsToWait);

			lock (dataLock)
				data = newData;

			coroutineRunning = false;
			MaybeDispatchCoroutine();
			yield return null;
		}
	}
}
