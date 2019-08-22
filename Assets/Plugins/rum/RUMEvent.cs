using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public class RUMEvent {

        private class WriteLocker {

            public int Status = 0;
        }

        private class CheckLocker {

            public int Status = 0;
        }

        private class ConfigLocker {

            public int Status = 0;
        }

        private const string EVENT_CACHE = "event_cache";
        private const string EVENT_MAP_0 = "event_map_0";
        private const string EVENT_MAP_1 = "event_map_1";
        private const string EVENT_MAP_2 = "event_map_2";
        private const string EVENT_MAP_3 = "event_map_3";

        private IDictionary<string, string> _eventMap = new Dictionary<string, string>() {

            { "1", EVENT_MAP_1 }, 
            { "2", EVENT_MAP_2 }, 
            { "3", EVENT_MAP_3 }
        };

        private int _writeIndex;

        private string _rumId;
        private bool _isFirst;

        private IDictionary<string, string> _config;
        private bool _debug;

        private long _session;
        private long _timestamp; 
        private long _delayCount;

        private int _storageCount;
        private int _storageSize;
        private int _sizeLimit;

        private RUMFile _rumFile;

        private string _rumIdKey = "rum_rid_";
        private string _rumEventKey = "rum_event_";
        private string _fileIndexKey = "rum_index_";

        private object second_locker = new object();

        private WriteLocker write_locker = new WriteLocker();
        private CheckLocker check_locker = new CheckLocker();

        private IDictionary<string, object> _storage;
        private object storage_locker = new object();

        private Action _sendQuest;
        private Action _openEvent;

        private bool _destroyed;

        public RUMEvent(int pid, bool debug, Action sendQuest, Action openEvent) {

            this._rumIdKey += pid;
            this._rumEventKey += pid;
            this._fileIndexKey += pid;

            this._debug = debug;
            this._sendQuest = sendQuest;
            this._openEvent = openEvent;
            this._sizeLimit = RUMConfig.SENT_SIZE_LIMIT;
            this._rumFile = new RUMFile(pid, debug);
        }

        public void Init() {

            this.StartWriteThread();
        }

        private void InitStorage() {

            int index = 0;

            lock (storage_locker) {

                if (this._storage == null) {

                    this._storage = this.LoadStorage(); 

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

                IDictionary<string, object> item = (IDictionary<string, object>)this._storage[this._fileIndexKey];

                if (item.ContainsKey("index")) {

                    index = Convert.ToInt32(item["index"]);
                } else {

                    item.Add("index", index);
                }
            }

            lock (self_locker) {

                this._writeIndex = index;
            }

            if (this._openEvent != null) {

                this._openEvent();
            }
        }

        private object self_locker = new object();

        public void SetSession(long value) {

            lock (self_locker) {

                this._session = value;
            } 
        }

        private ConfigLocker config_locker = new ConfigLocker();

        public void UpdateConfig(IDictionary<string, object> value) {

            lock (config_locker) {

                if (this._config == null) {

                    this._config = new Dictionary<string, string>();
                }

                this._config.Clear();

                foreach (string key in value.Keys) {

                    List<object> list = (List<object>) value[key];

                    foreach (object obj in list) {

                        if (this._eventMap.ContainsKey(key)) {

                            this._config.Add((string)obj, this._eventMap[key]);
                        }
                    }
                }

                config_locker.Status = 1;
            }

            ICollection<object> items = null;

            lock (storage_locker) {

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                items = event_cache.Values;
                event_cache = new Dictionary<string, object>();
            }

            if (!this.IsNullOrEmpty(items)) {

                this.WriteEvents(items);
            }
        }

        private List<object> _eventCache = new List<object>();

        public void WriteEvent(IDictionary<string, object> dict) {

            lock (write_locker) {

                if (this._eventCache.Count < 5000) {

                    this._eventCache.Add(dict);
                }

                if (this._eventCache.Count == 4998) {

                    ErrorRecorderHolder.recordError(new Exception("Cache Events Limit!"));
                }
            }

            this.StartWriteThread();
        }

        private Thread _writeThread = null;
        private ManualResetEvent _writeEvent = new ManualResetEvent(false);

        private void StartWriteThread() {

            lock (self_locker) {

                if (this._destroyed) {

                    return;
                }
            }

            lock (write_locker) {

                if (write_locker.Status != 0) {

                    return;
                }

                write_locker.Status = 1;
                this._writeEvent.Reset();

                this._writeThread = new Thread(new ThreadStart(WriteThread));

                if (this._writeThread.Name == null) {

                    this._writeThread.Name = "rum_write_thread";
                }

                this._writeThread.Start();
            }
        }

        private void WriteThread() {

            try {

                this.InitStorage();

                while (true) {

                    List<object> list;

                    lock (write_locker) {

                        if (write_locker.Status == 0) {

                            return;
                        }

                        list = this._eventCache;
                        this._eventCache = new List<object>();
                    }

                    this.WriteStorage(list);
                    this.StartCheckThread();
                    this._writeEvent.WaitOne(RUMConfig.SENT_INTERVAL);
                }
            } catch (ThreadAbortException tex) {
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            } finally {

                this.StopWriteThread();
            }
        }

        private void StopWriteThread() {

            lock (write_locker) {

                if (write_locker.Status != 0) {

                    write_locker.Status = 0;
                    this._writeEvent.Set();
                }
            }
        }

        private void WriteStorage(ICollection<object> list) {

            if (!this.IsNullOrEmpty(list)) {

                this.WriteEvents(list);
            }

            if (this._sendQuest != null) {

                this._sendQuest();
            }

            this.SaveStorage();
        }

        public void WriteEvents(ICollection<object> items) {

            foreach (IDictionary<string, object> item in items) {

                lock (self_locker) {

                    if (!item.ContainsKey("rid")) {

                        item.Add("rid", this._rumId);
                    }

                    if (!item.ContainsKey("sid")) {

                        item.Add("sid", this._session);
                    }
                }

                this.AddEvent(item);
            }
        }

        private void AddEvent(IDictionary<string, object> dict) {

            string key = Convert.ToString(dict["ev"]);
            string storageKey = this.SelectKey(key);

            if (!string.IsNullOrEmpty(storageKey)) {

                lock (storage_locker) {

                    IDictionary<string, object> event_storage = this.GetEventMap(storageKey);

                    if (!event_storage.ContainsKey(key)) {

                        event_storage[key] = new List<object>();
                    }

                    List<object> event_list = (List<object>)event_storage[key];

                    if (event_list.Count >= RUMConfig.EVENT_QUEUE_LIMIT) {

                        event_list.RemoveAt(0);
                        
                        if (this._debug) {

                            Debug.Log("[RUM] event(normal) queue limit & will be shift! " + Json.SerializeToString(dict));
                        }
                    }

                    event_list.Add(dict);
                }
            } else {

                if (this._debug) {

                    Debug.Log("[RUM] disable event & will be discard! " + key);
                } 
            }
        }

        public string GetRumId() {

            lock (self_locker) {

                return this._rumId;
            }
        }

        public long GetTimestamp() {

            lock (second_locker) {

                return this._timestamp;
            }
        }

        public void SetTimestamp(long value) {

            lock (second_locker) {

                if (value == 0) {

                    this._delayCount = 0;
                    this._timestamp = 0;
                    return;
                }

                if (value < this._timestamp) {

                    this._delayCount = this._timestamp - value;
                } else {

                    this._delayCount = 0;
                    this._timestamp = value;
                }
            }
        }

        public bool IsFirst() {

            bool build = false;

            lock (self_locker) {
            
                build = string.IsNullOrEmpty(this._rumId);
            }

            if (build) {

                this.BuildRumId();
            }

            lock (self_locker) {

                if (this._isFirst) {

                    this._isFirst = false;
                    return true;
                }

                return false;
            }
        }

        public void Destroy() {

            lock (self_locker) {

                if (this._destroyed) {

                    return;
                }

                this._destroyed = true;
            }

            this.StopWriteThread();
            this.StopCheckThread();

            lock (config_locker) {

                config_locker.Status = 0;
                this._config = null;
            }

            lock (second_locker) {

                this._delayCount = 0;
                this._timestamp = 0;
            }

            this._sendQuest = null;
            this._openEvent = null;
        }

        public int GetStorageSize() {

            lock (self_locker) {

                return this._storageSize;
            }
        }

        public void SetSizeLimit(int value) {

            lock (self_locker) {

                if (value > 0) {

                    this._sizeLimit = value;
                }
            }
        }

        public bool HasConfig() {

            lock (config_locker) {

                return config_locker.Status != 0;
            }
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

            int sizeLimit = 0;

            lock (self_locker) {

                sizeLimit = this._sizeLimit;
            } 

            return this.GetEventsFromStorage(sizeLimit, true);
        }

        private List<object> GetFileEvents() {

            return this.GetEventsFromStorage(RUMConfig.LOCAL_FILE_SIZE, false);
        }

        private List<object> GetEventsFromStorage(int size, bool catchAble) {

            List<object> items = new List<object>();
            int countLimit = this.GetCountLimit(size);
            
            if (countLimit <= 0) {

                return items;
            }

            lock (storage_locker) {

                foreach (string map_key in this._eventMap.Values) {

                    this.ShiftEvents(map_key, countLimit, ref items);

                    if (items.Count >= countLimit) {

                        break;
                    }
                }

                if (catchAble) {

                    this.AddToCache(items);
                }
            }

            return items;
        }

        private int GetCountLimit(int size) {

            int storegeCount = 0;
            int storageSize = 0;

            lock (self_locker) {

                storageSize = this._storageSize;
                storegeCount = this._storageCount;
            }

            if (size < 1 || storageSize < 1 || storegeCount < 1) {

                return 0;
            }

            return (int) Math.Ceiling(size / (storageSize / storegeCount * 1f));
        }

        private void ShiftEvents(string key, int countLimit, ref List<object> items) {

            IDictionary<string, object> event_map = this.GetEventMap(key);

            if (this.IsNullOrEmpty(event_map)) {

                return;
            }

            List<string> event_keys = new List<string>(event_map.Keys);

            foreach (string evk in event_keys) {

                List<object> event_list = (List<object>)event_map[evk];

                while (event_list.Count > 0 && items.Count < countLimit) {

                    IDictionary<string, object> item = (IDictionary<string, object>)event_list[0];

                    this.TrimEmptyKey(item);
                    
                    items.Add(item);
                    event_list.RemoveAt(0);
                }

                if (event_list.Count == 0) {

                    event_map.Remove(evk);
                }

                if (items.Count >= countLimit) {

                    break;
                }
            }
        }

        private void TrimEmptyKey(IDictionary<string, object> item) {

            List<string> keys = new List<string>(item.Keys);

            foreach (string k in keys) {

                if (k == "status") {

                    continue;
                }

                if (this.IsNullOrEmpty(item[k])) {

                    item.Remove(k);
                }
            }
        }

        private void AddToCache(ICollection<object> items) {

            IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

            foreach (IDictionary<string, object> item in items) {

                string cache_key = Convert.ToString(item["eid"]); 

                if (event_cache.ContainsKey(cache_key)) {

                    event_cache[cache_key] = item;
                } else {

                    event_cache.Add(cache_key, item);
                }
            }
        }

        private IDictionary<string, object> LoadStorage() {

            bool needClear = false;
            IDictionary<string, object> storage = null;

            RUMFile.Result res = this._rumFile.LoadStorage();

            if (res.success) {

                try {

                    byte[] storage_bytes = (byte[])res.content;
                    using (MemoryStream inputStream = new MemoryStream(storage_bytes)) {

                        storage = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
                    }

                    needClear = storage_bytes.Length > 2 * RUMConfig.STORAGE_SIZE_MAX;
                } catch(Exception ex) {

                    ErrorRecorderHolder.recordError(ex);
                }
            } 

            if (needClear && storage.ContainsKey(this._rumEventKey)) {

                ((IDictionary<string, object>)storage[this._rumEventKey]).Clear();

                if (this._debug) {

                    Debug.LogError("[RUM] storage size limit, clear events!");
                }
            }

            if (storage == null) {

                storage = new Dictionary<string, object>();
            }

            return storage;
        }

        private void SaveStorage() {

            byte[] storage_bytes = this.CheckStorageBytes();

            if (storage_bytes.Length < 1) {

                return;
            }

            RUMFile.Result res = this._rumFile.SaveStorage(storage_bytes);

            if (!res.success) {

                if (this._debug) {

                    Debug.Log("[RUM] fail to save storage!");
                }

                ErrorRecorderHolder.recordError((Exception)res.content);
            }
        }

        private byte[] CheckStorageBytes() {

            byte[] storage_bytes = new byte[0];

            try {

                using (MemoryStream outputStream = new MemoryStream()) {

                    lock (storage_locker) {

                        MsgPack.Serialize(this._storage, outputStream);
                    }

                    outputStream.Seek(0, SeekOrigin.Begin);
                    storage_bytes = outputStream.ToArray();
                }
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            if (storage_bytes.Length > 2 * RUMConfig.STORAGE_SIZE_MAX) {

                if (this._debug) {

                    Debug.LogError("[RUM] storage size limit, will be clear!");
                }

                lock (storage_locker) {

                    ((IDictionary<string, object>)this._storage[this._rumEventKey]).Clear();
                }

                this.UpdateStorageSize(160);
                return new byte[0];
            }

            this.UpdateStorageSize(storage_bytes.Length);
            return storage_bytes;
        }

        private void UpdateStorageSize(int size) {

            int count = 0;

            lock (storage_locker) {
                
                count += this.GetStorageCount(EVENT_MAP_1);
                count += this.GetStorageCount(EVENT_MAP_2);
                count += this.GetStorageCount(EVENT_MAP_3);

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);
                count += event_cache.Values.Count;
            }

            lock (self_locker) {

                this._storageCount = count;
                this._storageSize = size;
            }
        }

        private int GetStorageCount(string key) {

            int count = 0;

            IDictionary<string, object> event_map = this.GetEventMap(key);

            if (!this.IsNullOrEmpty(event_map)) {

                List<object> event_list = new List<object>(event_map.Values);

                foreach (object obj in event_list) {

                    count += ((List<object>) obj).Count;
                }
            }

            return count;
        }

        private string BuildRumId() {

            bool first = false;
            string rum_id = this.UUID(0, 16, 'c');

            lock (storage_locker) {

                IDictionary<string, object> item = (IDictionary<string, object>)this._storage[this._rumIdKey];

                if (item.ContainsKey("rid")) {

                    rum_id = Convert.ToString(item["rid"]);
                } else {

                    first = true;
                    item.Add("rid", rum_id);
                }
            }

            lock (self_locker) {

                this._isFirst = first;
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
                char[] ms_chars = Convert.ToString(FPManager.Instance.GetMilliTimestamp()).ToCharArray();

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

        private Thread _checkThread = null;
        private ManualResetEvent _checkEvent = new ManualResetEvent(false);

        private void StartCheckThread() {

            lock (self_locker) {

                if (this._destroyed) {

                    return;
                }
            }

            lock (check_locker) {

                if (check_locker.Status != 0) {

                    return;
                }

                check_locker.Status = 1;
                this._checkEvent.Reset();

                this._checkThread = new Thread(new ThreadStart(CheckThread));

                if (this._checkThread.Name == null) {

                    this._checkThread.Name = "rum_check_thread";
                }

                this._checkThread.Start();
            }
        }

        private void CheckThread() {

            try {

                while (true) {

                    lock (check_locker) {

                        if (check_locker.Status == 0) {

                            return;
                        }
                    }

                    this.CheckStorage();
                    this._checkEvent.WaitOne(RUMConfig.LOCAL_STORAGE_DELAY);
                }
            } catch (ThreadAbortException tex) {
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            } finally {

                this.StopCheckThread();
            }
        }

        private void StopCheckThread() {

            lock (check_locker) {

                if (check_locker.Status != 0) {

                    check_locker.Status = 0;
                    this._checkEvent.Set();
                }
            }
        }

        private void CheckStorage() {

            int size;
            bool needSave = false;

            lock (self_locker) { 

                size = this._storageSize;
            }

            if (size >= RUMConfig.STORAGE_SIZE_MAX) {

                List<object> list = this.GetFileEvents();

                if (!this.IsNullOrEmpty(list)) {

                    needSave = this.SlipStorage(list);
                }
            }

            if (size < RUMConfig.STORAGE_SIZE_MIN) {

                needSave = this.ReStorage();
            }

            if (needSave) {

                this.SaveStorage();
            }
        }

        private bool SlipStorage(List<object> list) {

            byte[] bytes = new byte[0];

            try {

                using (MemoryStream outputStream = new MemoryStream()) {

                    MsgPack.Serialize(list, outputStream);
                    outputStream.Seek(0, SeekOrigin.Begin);

                    bytes = outputStream.ToArray();
                }
            } catch (Exception ex) {

                ErrorRecorderHolder.recordError(ex);
            }

            int index = 0;

            lock (self_locker) {

                index = this._writeIndex;
            }

            RUMFile.Result res = this._rumFile.SaveRumLog(index, bytes);

            if (res.success) {

                if (this._debug) {

                    Debug.Log("[RUM] write to file, index: " + index);
                }

                index = (index + 1) % RUMConfig.LOCAL_FILE_COUNT;

                lock (storage_locker) {

                    ((IDictionary<string, object>)(this._storage[this._fileIndexKey]))["index"] = index;
                }

                lock (self_locker) {

                    this._writeIndex = index;
                }
            } else {

                ErrorRecorderHolder.recordError((Exception)res.content);
                this.WriteEvents(list);
            }

            return res.success;
        }

        private bool ReStorage() {

            RUMFile.Result res = this._rumFile.LoadRumLog();

            if (res.success) {

                List<object> items = null;

                try {

                    using (MemoryStream inputStream = new MemoryStream((byte[])res.content)) {

                        items = MsgPack.Deserialize<List<object>>(inputStream);
                    }
                } catch(Exception ex) {

                    ErrorRecorderHolder.recordError(ex);
                }

                if (!this.IsNullOrEmpty(items)) {

                    this.WriteEvents(items);
                }

                if (this._debug) {

                    Debug.Log("[RUM] success load from file!");
                }
            } 

            return res.success;
        }

        public void OnSecond(long timestamp) {

            lock (second_locker) {

                if (this._timestamp <= 0) {

                    return;
                }

                if (this._delayCount > 0) {

                    this._delayCount--;
                } else {

                    this._timestamp++;
                }
            }
        }

        private string SelectKey(string innerKey) {

            lock (config_locker) {

                if (config_locker.Status == 0) {

                    return EVENT_MAP_1;
                }

                if (this._config.ContainsKey(innerKey)) {

                    return this._config[innerKey];
                }
            }

            return null;
        }
    }
}