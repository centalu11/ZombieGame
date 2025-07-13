using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Core
{
    /// <summary>
    /// Simple service locator for dependency injection
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Register a service implementation
        /// </summary>
        /// <typeparam name="T">The interface type</typeparam>
        /// <param name="implementation">The concrete implementation</param>
        public static void Register<T>(T implementation)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Service of type {type.Name} is already registered. Overwriting...");
            }
            _services[type] = implementation;
        }

        /// <summary>
        /// Get a service implementation
        /// </summary>
        /// <typeparam name="T">The interface type</typeparam>
        /// <returns>The registered implementation</returns>
        public static T Get<T>()
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            Debug.LogError($"Service of type {type.Name} is not registered!");
            return default(T);
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">The interface type</typeparam>
        /// <returns>True if the service is registered</returns>
        public static bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        /// <typeparam name="T">The interface type</typeparam>
        public static void Unregister<T>()
        {
            _services.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all registered services
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }
} 