using OwlLogging;
using System;
using System.Collections.Generic;

namespace Shared
{
    public static class ResourcePool<T> where T : new()
    {
        private static List<T> _availablePool = new();
        // This is only used for safety checks so that object constructed outside can't be "returned" to the pool, or for metrics
        // It also guarantees that objects will never be GCed while the pool lives
        private static HashSet<T> _reservedPool = new();

        public static T Acquire()
        {
            if (_availablePool.Count == 0)
            {
                ExpandAvailablePool();
            }

            T acquiredObject = _availablePool[^1];
            _availablePool.RemoveAt(_availablePool.Count - 1);
            _reservedPool.Add(acquiredObject);
            return acquiredObject;
        }

        public static void Return(T returnedObject)
        {
            if (!_reservedPool.Contains(returnedObject))
            {
                OwlLogger.LogError("Tried to return object to pool that's not found in reservedPool!", GameComponent.Other);
                return;
            }

            _reservedPool.Remove(returnedObject);
            _availablePool.Add(returnedObject);
        }

        public static int ClearPool()
        {
            if (_reservedPool.Count > 0)
            {
                OwlLogger.LogError($"Can't clear pool for Type {nameof(T)} when some objects haven't been returned yet!", GameComponent.Other);
                return -1;
            }

            _availablePool.Clear();
            // Manipulate the pool's Capacity here to reduce its memory consumption?
            return 0;
        }

        private static void ExpandAvailablePool()
        {
            int increaseAmount = Math.Max(_availablePool.Capacity, 1);
            _availablePool.Capacity = _availablePool.Capacity + increaseAmount;
            for (int i = 0; i < increaseAmount; i++)
            {
                _availablePool.Add(new());
            }
        }
    }

    //******************************************

    public interface IAutoInitPoolObject
    {
        public abstract void Reset();
    }

    public static class AutoInitResourcePool<T> where T : IAutoInitPoolObject, new()
    {
        private static List<T> _availablePool = new();
        // This is only used for safety checks so that object constructed outside can't be "returned" to the pool, or for metrics
        // It also guarantees that objects will never be GCed while the pool lives
        private static HashSet<T> _reservedPool = new();

        public static T Acquire()
        {
            if (_availablePool.Count == 0)
            {
                ExpandAvailablePool();
            }

            T acquiredObject = _availablePool[^1];

            // If _availablePool is only enumberable, not indexable (like a HashSet)
            //T acquiredObject;
            //foreach(T obj in _availablePool)
            //{
            //	acquiredObject = obj;
            //	break;
            //}

            _availablePool.RemoveAt(_availablePool.Count - 1);
            _reservedPool.Add(acquiredObject);
            return acquiredObject;
        }

        public static void Return(T returnedObject)
        {
            if (!_reservedPool.Contains(returnedObject))
            {
                OwlLogger.LogError("Tried to return object to pool that's not found in reservedPool!", GameComponent.Other);
                return;
            }

            returnedObject.Reset();
            _reservedPool.Remove(returnedObject);
            _availablePool.Add(returnedObject);
        }

        public static int ClearPool()
        {
            if (_reservedPool.Count > 0)
            {
                OwlLogger.LogError($"Can't clear pool for Type {nameof(T)} when some objects haven't been returned yet!", GameComponent.Other);
                return -1;
            }

            _availablePool.Clear();
            // Manipulate the pool's Capacity here to reduce its memory consumption?
            return 0;
        }

        private static void ExpandAvailablePool()
        {
            int increaseAmount = Math.Max(_availablePool.Capacity, 1);
            _availablePool.Capacity = _availablePool.Capacity + increaseAmount;
            for (int i = 0; i < increaseAmount; i++)
            {
                T newObj = new();
                newObj.Reset();
                _availablePool.Add(newObj);
            }
        }
    }
}

