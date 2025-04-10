﻿using System.Collections.Generic;
using UnityEngine;

namespace TNHFramework.Utilities
{  
    public static class Extensions
    {

        public static T GetRandom<T>(this List<T> list)
        {
            if (list.Count < 1)
            {
                TNHFrameworkLogger.LogWarning($"GetRandom() was called on an empty list of type [{typeof(T)}]!");
                return default;
            }
            
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static bool ContainsNull<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Calculates needed space for an object
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        public static Bounds GetMaxBounds(this GameObject g)
        {
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }
    }

}
