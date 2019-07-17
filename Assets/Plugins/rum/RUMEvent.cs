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

        private int _storageSize;
        private int _sizeLimit;

        private string _rumIdKey = "rum_rid_";
        private string _rumEventKey = "rum_event_";
        private string _fileIndexKey = "rum_index_";

        private System.Object locker = new System.Object();
        private System.Object storage_locker = new System.Object();

        private IDictionary<string, object> _storage = new Dictionary<string, object>();

        private Action _sendQuest;

        public RUMEvent(int pid, bool debug, Action sendQuest) {

            this._rumIdKey += pid;
            this._rumEventKey += pid;
            this._fileIndexKey += pid;

            this._debug = debug;
            this._sendQuest = sendQuest;
            this._sizeLimit = RUMConfig.SENT_SIZE_LIMIT;
        }

        public void InitStorage() {

            lock(storage_locker) {

                this._storage = this.StorageLoad(); 

                if (!this._storage.ContainsKey(this._rumIdKey)) {

                    this._storage.Add(this._rumIdKey, new Dictionary<string, object>());
                }

                if (!this._storage.ContainsKey(this._rumEventKey)) {

                    this._storage.Add(this._rumEventKey, new Dictionary<string, object>());
                }

                if (!this._storage.ContainsKey(this._fileIndexKey)) {

                    this._storage.Add(this._fileIndexKey, new Dictionary<string, object>());
                }
            }

            this.StartWriteThread();
            this.StartCheckThread();
        }

        public void UpdateConfig(IDictionary<string, object> value) {

            this._config = value;
            this._hasConf = true;

            lock (storage_locker) {

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                if (!this.IsNullOrEmpty(event_cache)) {

                    this.AddEvents(event_cache.Values);
                    event_cache.Clear();
                }
            }
        }

        private List<object> _eventCache = new List<object>();

        public void WriteEvent(IDictionary<string, object> dict) {

            lock(this._eventCache) {

                this._eventCache.Add(dict);

                if (this._eventCache.Count >= 1000) {

                    this._eventCache.Clear();
                }
            }
        }

        private bool _writeAble;
        private System.Threading.ManualResetEvent _writeEvent = new System.Threading.ManualResetEvent(false);

        private void StartWriteThread() {

            if (this._writeAble) {

                return;
            }

            RUMEvent self = this;
            this._writeAble = true;

            ThreadPool.Instance.Execute((state) => {

                while (self._writeAble) {

                    try {

                        List<object> list;

                        lock (self._eventCache) {

                            list = self._eventCache;
                            self._eventCache = new List<object>();
                        }

                        self.WriteEvents(list);

                        if (self._sendQuest != null) {

                            self._sendQuest();
                        }

                        self._writeEvent.WaitOne(1000);
                    } catch (System.Threading.ThreadAbortException tex) {
                    } catch (Exception e) {

                        ErrorRecorderHolder.recordError(e);
                    }
                }
            });
        }

        private void StopWriteThread() {

            lock(this._eventCache) {

                this._writeEvent.Set();
            }

            this._writeAble = false;
        }

        public void WriteEvents(ICollection<object> items) {

            lock (storage_locker) {

                this.AddEvents(items);
            } 
        }

        private void AddEvents(ICollection<object> items) {

            foreach (IDictionary<string, object> item in items) {

                this.AddEvent(item);
            }
        }

        private void AddEvent(IDictionary<string, object> dict) {

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
            } else {

                if (this._debug) {

                    Debug.Log("[RUM] disable event & will be discard! " + key);
                } 
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

            this.StopWriteThread();
            this.StopCheckThread();

            this._lastSecondTime = 0;

            this._rumId = null;

            this._isFirst = false;

            this._config = null;
            this._hasConf = false;

            this._delayCount = 0;
            this._sendQuest = null;
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
        
        public void ClearRumId() {

            lock (storage_locker) {

                ((IDictionary<string, object>)this._storage[this._rumIdKey]).Clear();
            }

            if (this._debug) {

                Debug.Log("[RUM] storage clear! rid_key: " + this._rumIdKey);
            }
        }

        public void ClearEvents() {

            lock (storage_locker) {

                ((IDictionary<string, object>)this._storage[this._rumEventKey]).Clear();
            }

            if (this._debug) {

                Debug.Log("[RUM] storage clear! storage_key: " + this._rumEventKey);
            }
        }

        public void RemoveFromCache(ICollection<object> items) {

            lock (storage_locker) {

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                foreach (IDictionary<string, object> item in items) {

                    string key = Convert.ToString(item["eid"]); 

                    if (event_cache.ContainsKey(key)) {

                        event_cache.Remove(key);
                    }
                }
            }
        }

        private IDictionary<string, object> GetEventMap(string key) {

            IDictionary<string, object> items = (IDictionary<string, object>)this._storage[this._rumEventKey];

            if (!items.ContainsKey(key)) {

                items.Add(key, new Dictionary<string, object>());
            }

            return (IDictionary<string, object>)items[key];
        }

        public List<object> GetSentEvents() {

            int size = 0;
            List<object> items = new List<object>();

            this.ShiftEvents(EVENT_MAP_1, this._sizeLimit, true, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_2, this._sizeLimit, true, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_3, this._sizeLimit, true, ref size, ref items);

            if (size >= this._sizeLimit) {

                return items;
            }

            return items;
        }

        private void ShiftEvents(string key, int sizeLimit, bool catchAble, ref int size, ref List<object> items) {

            lock (storage_locker) {

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

                    while (event_list.Count > 0 && size < sizeLimit) {

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

                            if (k == "status") {

                                continue;
                            }

                            if (this.IsNullOrEmpty(item[k])) {

                                item.Remove(k);
                            }
                        }

                        items.Add(item);

                        if (catchAble) {

                            string cache_key = Convert.ToString(item["eid"]);

                            if (event_cache.ContainsKey(cache_key)) {

                                event_cache[cache_key] = item;
                            } else {

                                event_cache.Add(cache_key, item);
                            }
                        }

                        byte[] bytes = new byte[0];

                        try {

                            using (MemoryStream outputStream = new MemoryStream()) {

                                MsgPack.Serialize(item, outputStream);
                                outputStream.Seek(0, SeekOrigin.Begin);

                                bytes = outputStream.ToArray();
                            }
                        } catch (Exception ex) {

                            RUMPlatform.Instance.WriteDebug("shift_events_serialize_item", ex);
                        }

                        size += bytes.Length;
                    }

                    if (event_list.Count == 0) {

                        event_map.Remove(evk);
                    }

                    if (size >= sizeLimit) {

                        break;
                    }
                }
            }
        }

        private int _saveFailCount;

        private void StorageSave() {

            byte[] storage_bytes = new byte[0];

            lock(storage_locker) {

                try {

                    using (MemoryStream outputStream = new MemoryStream()) {

                        MsgPack.Serialize(this._storage, outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        storage_bytes = outputStream.ToArray();
                    }
                } catch (Exception ex) {

                    RUMPlatform.Instance.WriteDebug("storage_save_serialize_storage", ex);
                    return;
                }
            }

            if (storage_bytes.Length <= 0) {

                return;
            }

            if (storage_bytes.Length > 2 * RUMConfig.STORAGE_SIZE_MAX) {

                this.ClearEvents();
                return;
            }

            RUMFile.Result res = RUMFile.Instance.WriteStorage(storage_bytes);

            if (!res.success) {

                if (this._saveFailCount < 3) {

                    this._saveFailCount++;
                    RUMPlatform.Instance.WriteDebug("storage_save_write_storage", (Exception)res.content);
                } 
            }

            if (this._debug) {

                Debug.Log("[RUM] storage save! " + res.success);
            } 
        }

        private IDictionary<string, object> StorageLoad() {

            IDictionary<string, object> storage = null;

            RUMFile.Result res = RUMFile.Instance.ReadStorage();

            if (res.success) {

                try {

                    using (MemoryStream inputStream = new MemoryStream((byte[])res.content)) {

                        storage = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
                    }
                } catch(Exception ex) {

                    Debug.LogError("storage_load_deserialize_content: " + ex.Message);
                }
            } 

            if (storage == null) {

                storage = new Dictionary<string, object>();
            }

            return storage;
        }

        private string BuildRumId() {

            string rum_id = this.UUID(0, 16, 'c');

            lock(storage_locker) {

                IDictionary<string, object> item = (IDictionary<string, object>)this._storage[this._rumIdKey];

                if (item.ContainsKey("rid")) {

                    rum_id = Convert.ToString(item["rid"]);
                } else {

                    this._isFirst = true;
                    item.Add("rid", rum_id);
                }

                return this._rumId = rum_id;
            }
        }

        private string UUID(int len, int radix, char fourteen) {

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
                uuid_chars[14] = fourteen;

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

            if (obj is byte) {

                return Convert.ToByte(obj) == 0;
            }

            if (obj is sbyte) {

                return Convert.ToSByte(obj) == 0;
            }

            if (obj is short) {

                return Convert.ToInt16(obj) == 0;
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

        private bool _checkAble;
        private System.Threading.ManualResetEvent _checkEvent = new System.Threading.ManualResetEvent(false);

        private void StartCheckThread() {

            if (this._checkAble) {

                return;
            }

            this._checkAble = true;

            RUMEvent self = this;

            ThreadPool.Instance.Execute((state) => {

                while (self._checkAble) {

                    try {

                        self.CheckStorageSize();
                        self._checkEvent.WaitOne(RUMConfig.LOCAL_STORAGE_DELAY);
                    } catch (System.Threading.ThreadAbortException tex) {
                    } catch (Exception e) {

                        ErrorRecorderHolder.recordError(e);
                    }
                }
            });
        }

        private void StopCheckThread() {

            lock(storage_locker) {

                this._checkEvent.Set();            
            }
            
            this._checkAble = false;
        }

        private void CheckStorageSize() {

            byte[] storage_bytes = new byte[0];

            lock(storage_locker) {

                try {

                    using (MemoryStream outputStream = new MemoryStream()) {

                        MsgPack.Serialize(this._storage, outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        storage_bytes = outputStream.ToArray();
                    }
                } catch (Exception ex) {

                    RUMPlatform.Instance.WriteDebug("check_storage_size_serialize_storage", ex);
                }
            }

            this._storageSize = storage_bytes.Length;

            if (this._storageSize >= RUMConfig.STORAGE_SIZE_MAX) {

                List<object> list = this.GetFileEvents();

                if (!this.IsNullOrEmpty(list)) {

                    this.SlipStorage(list);
                }
            }

            if (this._storageSize < RUMConfig.STORAGE_SIZE_MIN) {

                this.ReStorage();
            }

            this.StorageSave();
        }

        private void SlipStorage(List<object> list) {

            int index = 1;

            lock(storage_locker) {

                IDictionary<string, object> item = (IDictionary<string, object>)this._storage[this._fileIndexKey];

                if (item.ContainsKey("index")) {

                    index = Convert.ToInt32(item["index"]);
                } else {

                    item.Add("index", index);
                }

                byte[] bytes = new byte[0];

                try {

                    using (MemoryStream outputStream = new MemoryStream()) {

                        MsgPack.Serialize(list, outputStream);
                        outputStream.Seek(0, SeekOrigin.Begin);

                        bytes = outputStream.ToArray();
                    }
                } catch (Exception ex) {

                    RUMPlatform.Instance.WriteDebug("check_storage_size_serialize_list", ex);
                }

                RUMFile.Result res = RUMFile.Instance.WriteRumLog(index, bytes);

                if (res.success) {

                    item["index"] = (index + 1) % RUMConfig.LOCAL_FILE_COUNT;
                } else {

                    RUMPlatform.Instance.WriteDebug("check_storage_size_write_rum_log", (Exception)res.content);
                }

                if (this._debug) {

                    Debug.Log("[RUM] write to file: " + res.success + ",  index: " + index);
                }
            }
        }

        private void ReStorage() {

            RUMFile.Result res = RUMFile.Instance.ReadRumLog();

            if (res.success) {

                List<object> items = null;

                try {

                    using (MemoryStream inputStream = new MemoryStream((byte[])res.content)) {

                        items = MsgPack.Deserialize<List<object>>(inputStream);
                    }
                } catch(Exception ex) {

                    RUMPlatform.Instance.WriteDebug("check_storage_size_deserialize_content", ex);
                }

                if (!this.IsNullOrEmpty(items)) {

                    this.WriteEvents(items);
                }
            } 

            if (this._debug) {

                Debug.Log("[RUM] load form file: " + res.success);
            }
        }

        private List<object> GetFileEvents() {

            int size = 0;
            int sizeLimit = RUMConfig.LOCAL_FILE_SIZE;

            List<object> items = new List<object>();

            this.ShiftEvents(EVENT_MAP_1, sizeLimit, false, ref size, ref items);

            if (size >= sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_2, sizeLimit, false, ref size, ref items);

            if (size >= sizeLimit) {

                return items;
            }

            this.ShiftEvents(EVENT_MAP_3, sizeLimit, false, ref size, ref items);

            if (size >= sizeLimit) {

                return items;
            }

            lock (storage_locker) {

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                if (!this.IsNullOrEmpty(event_cache)) {

                    List<string> keys = new List<string>(event_cache.Keys);

                    foreach (string k in keys) {

                        IDictionary<string, object> item = (IDictionary<string, object>)event_cache[k];

                        items.Add(item);

                        byte[] bytes = new byte[0];

                        try {

                            using (MemoryStream outputStream = new MemoryStream()) {

                                MsgPack.Serialize(item, outputStream);
                                outputStream.Seek(0, SeekOrigin.Begin);

                                bytes = outputStream.ToArray();
                            }
                        } catch (Exception ex) {

                            RUMPlatform.Instance.WriteDebug("get_file_events_serialize_item", ex);
                        }

                        size += bytes.Length;
                        event_cache.Remove(k);

                        if (size >= sizeLimit) {

                            break;
                        }
                    }
                }
            }

            return items;
        }

        private void StartSecond() {

            if (this._lastSecondTime != 0 ) {

                return;
            }

            this._lastSecondTime = ThreadPool.Instance.GetMilliTimestamp();
        }

        public void OnSecond(long timestamp) {

            lock(locker) {

                if (this._lastSecondTime > 0 && timestamp - this._lastSecondTime >= 1000) {

                    this._lastSecondTime += 1000;

                    if (this._delayCount > 0) {

                        this._delayCount--;
                    } else {

                        this._timestamp++;
                    }
                }
            }
        }

        private string SelectKey(string innerKey) {

            if (!this._hasConf) {

                return EVENT_MAP_1;
            } 

            if (this._config.ContainsKey("1") && ((List<object>)this._config["1"]).Contains(innerKey)) {

                return EVENT_MAP_1;
            }

            if (this._config.ContainsKey("2") && ((List<object>)this._config["2"]).Contains(innerKey)) {

                return EVENT_MAP_2;
            }

            if (this._config.ContainsKey("3") && ((List<object>)this._config["3"]).Contains(innerKey)) {

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