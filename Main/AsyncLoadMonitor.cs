using System.Collections.Generic;
using UnityEngine;

namespace TNHFramework
{
    public static class AsyncLoadMonitor
    {
        public static List<AnvilCallback<AssetBundle>> CallbackList = [];

        public static float GetProgress()
        {
            if (CallbackList.Count == 0)
                return 1;

            float totalStatus = 0;

            for (int i = CallbackList.Count - 1; i >= 0; i--)
            {
                if (CallbackList[i].IsCompleted)
                {
                    CallbackList.RemoveAt(i);
                }
                else
                {
                    totalStatus += CallbackList[i].Progress;
                }
            }

            return totalStatus / CallbackList.Count;
        }
    }
}
