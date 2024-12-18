using UnityEngine;
using System.Collections.Generic;
using System;

namespace Unet.LitUdp.Chat
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly object Lock = new object();
        private readonly Queue<Action> executionQueue = new Queue<Action>();
        private static bool _initialized;

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_initialized) return _instance;
                
                lock (Lock)
                {
                    if (_initialized) return _instance;
                    
                    // 創建一個新的 GameObject 來持有這個組件
                    var go = new GameObject("UnityMainThreadDispatcher");
                    _instance = go.AddComponent<UnityMainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                    _initialized = true;
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                _initialized = true;
                DontDestroyOnLoad(gameObject);
            }
        }

        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogError("無法入隊 null action");
                return;
            }

            lock (executionQueue)
            {
                executionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            if (executionQueue.Count == 0) return;

            // 每幀最多執行所有當前隊列中的操作
            int operationsCount;
            lock (executionQueue)
            {
                operationsCount = executionQueue.Count;
            }

            while (operationsCount > 0)
            {
                Action action = null;
                lock (executionQueue)
                {
                    if (executionQueue.Count > 0)
                    {
                        action = executionQueue.Dequeue();
                    }
                }

                if (action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"執行隊列操作時發生錯誤: {ex.Message}");
                    }
                }

                operationsCount--;
            }
        }
    }
} 