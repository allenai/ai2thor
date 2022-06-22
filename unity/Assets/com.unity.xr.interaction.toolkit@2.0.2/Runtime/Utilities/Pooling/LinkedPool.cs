using System;

namespace UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling
{
    // Class based on UnityEngine.Pool.LinkedPool<T>, which was added to Unity 2021.1.
    // When the minimum Editor dependency is bumped, replace use of this internal class.
    /// <summary>
    /// A linked list version of an object pool.
    /// </summary>
    /// <typeparam name="T">The type of class to pool.</typeparam>
    /// <remarks>
    /// Object Pooling is a way to optimize your projects and lower the burden that is placed
    /// on the CPU when having to rapidly create and destroy new objects. It is a good practice
    /// and design pattern to keep in mind to help relieve the processing power of the CPU to
    /// handle more important tasks and not become inundated by repetitive create and destroy calls.
    /// The LinkedPool uses a linked list to hold a collection of object instances for reuse.
    /// Note this is not thread-safe.
    /// </remarks>
    class LinkedPool<T> : IDisposable where T : class
    {
        internal class LinkedPoolItem
        {
            internal LinkedPoolItem poolNext;
            internal T value;
        }

        readonly Func<T> m_CreateFunc;
        readonly Action<T> m_ActionOnGet;
        readonly Action<T> m_ActionOnRelease;
        readonly Action<T> m_ActionOnDestroy;
        readonly int m_Limit; // Used to prevent catastrophic memory retention.
        LinkedPoolItem m_PoolFirst; // The pool of available T objects
        LinkedPoolItem m_NextAvailableListItem; // When Get is called we place the node here for reuse and to prevent GC
        readonly bool m_CollectionCheck;

        /// <summary>
        /// Creates a new LinkedPool instance.
        /// </summary>
        /// <param name="createFunc">Used to create a new instance when the pool is empty. In most cases this will just be `() = new T()`.</param>
        /// <param name="actionOnGet">Called when the instance is taken from the pool.</param>
        /// <param name="actionOnRelease">Called when the instance is returned to the pool. This can be used to clean up or disable the instance.</param>
        /// <param name="actionOnDestroy">Called when the element could not be returned to the pool due to the pool reaching the maximum size.</param>
        /// <param name="collectionCheck">Collection checks are performed when an instance is returned back to the pool. An exception will be thrown if the instance is already in the pool. Collection checks are only performed in the Editor.</param>
        /// <param name="maxSize">The maximum size of the pool. When the pool reaches the max size then any further instances returned to the pool will be destroyed and garbage-collected. This can be used to prevent the pool growing to a very large size.</param>
        public LinkedPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, bool collectionCheck = true, int maxSize = 10000)
        {
            if (createFunc == null)
                throw new ArgumentNullException(nameof(createFunc));

            if (maxSize <= 0)
                throw new ArgumentException("Max size must be greater than 0", nameof(maxSize));

            m_CreateFunc = createFunc;
            m_ActionOnGet = actionOnGet;
            m_ActionOnRelease = actionOnRelease;
            m_ActionOnDestroy = actionOnDestroy;
            m_Limit = maxSize;
            m_CollectionCheck = collectionCheck;
        }

        /// <summary>
        /// Number of objects that are currently available in the pool.
        /// </summary>
        public int countInactive { get; private set; }

        /// <summary>
        /// Get an instance from the pool. If the pool is empty then a new instance will be created.
        /// </summary>
        /// <returns>A pooled object or a new instance if the pool is empty.</returns>
        public T Get()
        {
            T item;
            if (m_PoolFirst == null)
            {
                item = m_CreateFunc();
            }
            else
            {
                var first = m_PoolFirst;
                item = first.value;
                m_PoolFirst = first.poolNext;

                // Add the empty node to our pool for reuse and to prevent GC
                first.poolNext = m_NextAvailableListItem;
                m_NextAvailableListItem = first;
                m_NextAvailableListItem.value = null;
                --countInactive;
            }

            m_ActionOnGet?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Returns a PooledObject that will automatically return the instance to the pool when it is disposed.
        /// </summary>
        /// <param name="v">Out parameter that will contain a reference to an instance from the pool.</param>
        /// <returns>A PooledObject that will return the instance back to the pool when its Dispose method is called.</returns>
        public PooledObject<T> Get(out T v) => new PooledObject<T>(v = Get(), this);

        /// <summary>
        /// Returns the instance back to the pool.
        /// </summary>
        /// <param name="item">The instance to return to the pool.</param>
        /// <remarks>
        /// If the pool has collection checks enabled and the instance is already held by the pool then an exception will be thrown.
        /// </remarks>
        public void Release(T item)
        {
#if UNITY_EDITOR // keep heavy checks in editor
            if (m_CollectionCheck)
            {
                var listItem = m_PoolFirst;
                while (listItem != null)
                {
                    if (ReferenceEquals(listItem.value, item))
                        throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
                    listItem = listItem.poolNext;
                }
            }
#endif

            m_ActionOnRelease?.Invoke(item);

            if (countInactive < m_Limit)
            {
                var poolItem = m_NextAvailableListItem;
                if (poolItem == null)
                {
                    poolItem = new LinkedPoolItem();
                }
                else
                {
                    m_NextAvailableListItem = poolItem.poolNext;
                }

                poolItem.value = item;
                poolItem.poolNext = m_PoolFirst;
                m_PoolFirst = poolItem;
                ++countInactive;
            }
            else
            {
                m_ActionOnDestroy?.Invoke(item);
            }
        }

        /// <summary>
        /// Removes all pooled items. If the pool contains a destroy callback then it will be called for each item that is in the pool.
        /// </summary>
        public void Clear()
        {
            if (m_ActionOnDestroy != null)
            {
                for (var itr = m_PoolFirst; itr != null; itr = itr.poolNext)
                {
                    m_ActionOnDestroy(itr.value);
                }
            }

            m_PoolFirst = null;
            m_NextAvailableListItem = null;
            countInactive = 0;
        }

        /// <summary>
        /// Removes all pooled items. If the pool contains a destroy callback then it will be called for each item that is in the pool.
        /// </summary>
        public void Dispose()
        {
            // Ensure we do a clear so the destroy action can be called.
            Clear();
        }
    }
}