using System;
using System.Collections.Generic;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public class RUMDuplicate {

        private static RUMDuplicate instance;
        private static object lock_obj = new object();

        public static RUMDuplicate Instance {
            get {
                if (instance == null) {
                    lock (lock_obj) {
                        if (instance == null) {
                            instance = new RUMDuplicate();
                        }
                    }
                }

                return instance;
            }
        }

        private int _pid = 0;
        private object self_locker = new object();
        private IDictionary<string, int> _duplicateMap = new Dictionary<string, int>();

        public RUMDuplicate() {}

        public void Init(int pid) {
            if (pid <= 0) {
                Debug.LogWarning("[RUM] The 'pid' Is Zero Or Negative!");
                return;
            }

            this._pid = pid;
            FPManager.Instance.AddSecond(OnSecondDelegate);
        }

        private void OnSecondDelegate(EventData evd) {
            this.OnSecond();
        }

        public bool Check(string key, int ttl) {
            if (this._pid <= 0) {
                Debug.LogWarning("[RUM] The 'pid' Is Zero Or Negative, Init First!");
                return false;
            }

            if (string.IsNullOrEmpty(key)) {
                Debug.LogWarning("[RUM] The 'key' Is Null Or Empty!");
                return false;
            }

            if (ttl <= 0) {
                Debug.LogWarning("[RUM] The 'ttl' Zero Or Negative!");
                return true;
            }

            int timestamp = FPManager.Instance.GetTimestamp();
            string real_key = String.Format("{0}_{1}", this._pid, key);

            lock (self_locker) {
                if (this._duplicateMap.ContainsKey(real_key)) {
                    int expire = this._duplicateMap[real_key];

                    if (expire > timestamp) {
                        return false;
                    }

                    this._duplicateMap.Remove(real_key);
                }

                this._duplicateMap.Add(real_key, ttl + timestamp);
                return true;
            }
        }

        private void OnSecond() {
            int timestamp = FPManager.Instance.GetTimestamp();

            lock (self_locker) {
                List<string> keys = new List<string>(this._duplicateMap.Keys);

                foreach (string key in keys) {
                    int expire = this._duplicateMap[key];

                    if (expire > timestamp) {
                        continue;
                    }

                    this._duplicateMap.Remove(key);
                }
            }
        }
    }
}