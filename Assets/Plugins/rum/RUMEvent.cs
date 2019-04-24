using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public class RUMEvent {

        private const string EVENT_CACHE = "event_cache";
        private const string EVENT_MAP_0 = "event_map_0";
        private const string EVENT_MAP_1 = "event_map_1";
        private const string EVENT_MAP_2 = "event_map_2";
        private const string EVENT_MAP_3 = "event_map_3";

        private string _rumId;
        private bool _isFirst;

        private IDictionary<string, object> _config;
        private bool _hasConf;
        private bool _debug;

        private long _timestamp; 
        private long _delayCount;
        private long _lastSecondTime;

        private RUMPlatform _platform;

        private string _rum_id_storage;
        private string _rum_events_storage;

        private int _storageSize;
        private int _sizeLimit;

        public RUMEvent(int pid, RUMPlatform platform, bool debug) {

            this._debug = debug;
            this._platform = platform;
            this._rum_id_storage = "rum_rid_" + pid;
            this._rum_events_storage = "rum_events_" + pid;

            this._sizeLimit = RUMConfig.SENT_SIZE_LIMIT;
            this._platform.InitPrefs(this._rum_id_storage, this._rum_events_storage);
        }

        public void UpdateConfig(IDictionary<string, object> value) {

            this._config = value;
            this._hasConf = true;

            IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP_0);

            foreach (KeyValuePair<string, object> kvp in event_map) {

                string storageKey = this.SelectKey(kvp.Key);

                if (!string.IsNullOrEmpty(storageKey)) {

                    IDictionary<string, object> event_storage = this.GetEventMap(storageKey);

                    if (event_storage.ContainsKey(kvp.Key)) {

                        event_storage[kvp.Key] = this.MergeDictionary((IDictionary<string, object>)kvp.Value, (IDictionary<string, object>)event_storage[kvp.Key]);
                    } else {

                        event_storage[kvp.Key] = kvp.Value;
                    }
                } else {

                    if (this._debug) {

                        Debug.Log("[RUM] disable event & will be discard! " + Json.SerializeToString(kvp.Value));
                    } 
                }

                event_map.Remove(kvp.Key);
            }

            this.SetEventMap(EVENT_MAP_0, event_map);
            IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

            if (!this.IsNullOrEmpty(event_cache)) {

                foreach (IDictionary<string, object> item in event_cache.Values) {

                    this.WriteEvent(item);
                }
            }

            this.SetEventMap(EVENT_CACHE, new Dictionary<string, object>());
        }

        public void WriteEvent(IDictionary<string, object> dict) {

            string key = Convert.ToString(dict["ev"]);
            string storageKey = this.SelectKey(key);

            if (!string.IsNullOrEmpty(storageKey)) {

                IDictionary<string, object> event_storage = this.GetEventMap(storageKey);

                if (!event_storage.ContainsKey(key)) {

                    event_storage[key] = new List<object>();
                }

                List<object> event_list = (List<object>)event_storage[key];

                if (event_list.Count >= RUMConfig.EVENT_QUEUE_LIMIT) {

                    if (this._debug) {

                        Debug.Log("[RUM] event(normal) queue limit & will be shift! " + Json.SerializeToString(dict));
                    }

                    event_list.RemoveAt(0);
                }

                event_list.Add(dict);
                this.SetEventMap(storageKey, event_storage);
            } else {

                if (this._debug) {

                    Debug.Log("[RUM] disable event & will be discard! " + key);
                } 
            }
        }

        public void WriteEvents(List<object> items) {

            foreach (IDictionary<string, object> item in items) {

                this.WriteEvent(item);
            }
        }

        public string GetRumId() {

            if (string.IsNullOrEmpty(this._rumId)) {

                return this.BuildRumId();
            }

            return this._rumId;
        }

        public long GetTimestamp() {

            return this._timestamp;
        }

        public void SetTimestamp(long value) {

            this._lastSecondTime = 0;

            if (value < this._timestamp) {

                this._delayCount = this._timestamp - value;
            } else {

                this._timestamp = value;
            }

            this.StartSecond();
        }

        public bool IsFirst() {

            if (string.IsNullOrEmpty(this._rumId)) {

                this.BuildRumId();
            }

            if (this._isFirst) {

                this._isFirst = false;
                return true;
            }

            return false;
        }

        public void Destroy() {

            this._lastSecondTime = 0;

            this._rumId = null;

            this._isFirst = false;

            this._config = null;
            this._hasConf = false;

            this._delayCount = 0;
        }

        public int GetStorageSize() {

            return this._storageSize;
        }

        public void SetSizeLimit(int value) {

            if (value > 0) {

                this._sizeLimit = value;
            }
        }

        public bool HasConfig() {

            return this._hasConf;
        }

        public void ClearStorage() {

            this._platform.SetItem(this._rum_id_storage, new Dictionary<string, object>());
            this._platform.SetItem(this._rum_events_storage, new Dictionary<string, object>());
        }

        public void RemoveFromCache(List<object> items) {

            IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

            foreach (object item in items) {

                IDictionary<string, object> dict = (IDictionary<string, object>)item;
                event_cache.Remove(Convert.ToString(dict["eid"]));
            }

            this.SetEventMap(EVENT_CACHE, event_cache);
        }

        private IDictionary<string, object> GetEventMap(string key) {

            IDictionary<string, object> items = this._platform.GetItem(this._rum_events_storage);

            if (!items.ContainsKey(key)) {

                items.Add(key, new Dictionary<string, object>());
            }

            return (IDictionary<string, object>)items[key];
        }

        private void SetEventMap(string key, IDictionary<string, object> value) {

            IDictionary<string, object> items = this._platform.GetItem(this._rum_events_storage);

            if (!items.ContainsKey(key)) {

                items.Add(key, value);
            } else {

                items[key] = value;
            }
        }

        public List<object> GetSentEvents() {

            int size = 0;
            List<object> items = new List<object>();

            this.ShiftEvents(EVENT_MAP_1, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_2, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_3, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            return items;
        }

        private void ShiftEvents(string key, ref int size, ref List<object> items) {

            IDictionary<string, object> event_map = this.GetEventMap(key);

            if (this.IsNullOrEmpty(event_map)) {

                return;
            }

            IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

            if (string.IsNullOrEmpty(this._rumId)) {

                this.BuildRumId();
            }

            List<string> event_keys = new List<string>(event_map.Keys);

            foreach (string evk in event_keys) {

                List<object> event_list = (List<object>)event_map[evk];

                while (event_list.Count > 0 && size < this._sizeLimit) {

                    IDictionary<string, object> item = (IDictionary<string, object>)event_list[0];
                    event_list.RemoveAt(0);

                    if (!item.ContainsKey("ts")) {

                        item.Add("ts", this._timestamp);
                    }

                    if (!item.ContainsKey("rid")) {

                        item.Add("rid", this._rumId);
                    }

                    List<string> keys = new List<string>(item.Keys);

                    foreach (string k in keys) {

                        if (this.IsNullOrEmpty(item[k])) {

                            item.Remove(k); 
                        }
                    }

                    items.Add(item);
                    event_cache[Convert.ToString(item["eid"])] = item;

                    size += System.Text.Encoding.Default.GetByteCount(Json.SerializeToString(item));
                }

                if (event_list.Count == 0) {

                    event_map.Remove(evk);
                }

                if (size >= this._sizeLimit) {

                    break;
                }
            }

            this.SetEventMap(key, event_map);
        }

        private string BuildRumId() {

            string rum_id = this.UUID(0, 16);
            IDictionary<string, object> item = this._platform.GetItem(this._rum_id_storage);

            if (item.ContainsKey("rid")) {

                rum_id = Convert.ToString(item["rid"]);
            } else {

                this._isFirst = true;

                IDictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("rid", rum_id);

                this._platform.SetItem(this._rum_id_storage, dict);
            }

            return this._rumId = rum_id;
        }

        private string UUID(int len, int radix) {

            char[] chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
            char[] uuid_chars = new char[36];

            if (radix == 0) {

                radix = chars.Length;
            }

            System.Random rdm = new System.Random();

            if (len > 0) {

                // Compact form
                for (int i = 0; i < len; i++) {

                    uuid_chars[i] = chars[ 0 | Convert.ToInt32(rdm.NextDouble() * radix) ];
                }
            } else {

                // rfc4122, version 4 form
                int r;

                // Fill in random data.  At i==19 set the high bits of clock sequence as
                // per rfc4122, sec. 4.1.5
                for (int j = 0; j < 36; j++) {

                    r = 0 | Convert.ToInt32(rdm.NextDouble() * 16);
                    uuid_chars[j] = chars[ (j == 19) ? (r & 0x3) | 0x8 : r ];
                }

                // rfc4122 requires these characters
                uuid_chars[8] = uuid_chars[13] = uuid_chars[18] = uuid_chars[23] = '-';
                uuid_chars[14] = '4';

                // add timestamp(ms) at prefix
                char[] ms_chars = Convert.ToString(ThreadPool.Instance.GetMilliTimestamp()).ToCharArray();

                for (int k = 0; k < ms_chars.Length; k++) {

                    uuid_chars[k] = ms_chars[k];
                }
            }

            return new string(uuid_chars);
        }

        private bool IsNullOrEmpty(object obj) {

            if (obj == null) {

                return true;
            }

            if (obj is bool) {

                return false; 
            }

            if (obj is int) {

                return Convert.ToInt32(obj) == 0;
            }

            if (obj is long) {

                return Convert.ToInt64(obj) == 0;
            }

            if (obj is string) {

                return string.IsNullOrEmpty((string)obj);
            }

            if (obj is List<object>) {

                return ((List<object>)obj).Count == 0;
            }

            if (obj is Hashtable) {

                return ((Hashtable)obj).Count == 0;
            }

            if (obj is IDictionary<string, object>) {

                return ((IDictionary<string, object>)obj).Count == 0;
            }

            return false;
        }

        private void CheckStorageSize() {

            IDictionary<string, object> items = this._platform.GetItem(this._rum_events_storage);

            this._storageSize = System.Text.Encoding.Default.GetByteCount(Json.SerializeToString(items));

            if (this._storageSize > this._sizeLimit) {

                this._platform.SetItem(this._rum_events_storage, new Dictionary<string, object>());
            } 
        }

        private void StartSecond() {

            if (this._lastSecondTime != 0 ) {

                return;
            }

            this._lastSecondTime = ThreadPool.Instance.GetMilliTimestamp();
        }

        public void OnSecond(long timestamp) {

            if (this._lastSecondTime == 0) {

                return;
            }

            if (timestamp - this._lastSecondTime < 1000) {

                return;
            }

            this._lastSecondTime += 1000;

            if (this._delayCount > 0) {

                this._delayCount--;
            } else {

                this._timestamp++;
            }

            this.CheckStorageSize();
        }

        private string SelectKey(string innerKey) {

            if (!this._hasConf) {

                return EVENT_MAP_1;
            } 

            if (this._config.ContainsKey("1") && ((List<string>)this._config["1"]).Contains(innerKey)) {

                return EVENT_MAP_1;
            }

            if (this._config.ContainsKey("2") && ((List<string>)this._config["2"]).Contains(innerKey)) {

                return EVENT_MAP_2;
            }

            if (this._config.ContainsKey("3") && ((List<string>)this._config["3"]).Contains(innerKey)) {

                return EVENT_MAP_3;
            }

            return null;
        }

        private IDictionary<string, object> MergeDictionary(IDictionary<string, object> first, IDictionary<string, object> second) {

            if (first == null) {

                first = new Dictionary<string, object>();
            }

            if (second == null) {

                return first;
            }

            foreach (string key in second.Keys) {

                if (!first.ContainsKey(key)) {

                    first.Add(key,second[key]);
                }
            }

            return first;
        }
    }
}