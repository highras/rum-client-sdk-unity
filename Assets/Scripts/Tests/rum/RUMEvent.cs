using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public delegate string DumpDelegate();

    public class RUMEvent {

        private class WriteLocker {

            public int Status = 0;
        }

        private class CheckLocker {

            public int Status = 0;
        }

        private class ConfigLocker {

            public int Status = 0;
            public int Count = 0;
        }

        private const string EVENT_MAP = "event_map";
        private const string EVENT_CACHE = "event_cache";

        private const string HIGH_LEVEL = "high";
        private const string MEDIUM_LEVEL = "medium";
        private const string LOW_LEVEL = "low";

        private IDictionary<string, string> PRIORITY_MAP = new Dictionary<string, string>() {
            { "1", HIGH_LEVEL },
            { "2", MEDIUM_LEVEL },
            { "3", LOW_LEVEL }
        };

        private int _writeIndex;

        private string _rumId;
        private bool _isFirst;

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
        private object storage_locker = new object();
        private WriteLocker write_locker = new WriteLocker();
        private CheckLocker check_locker = new CheckLocker();

        private IDGenerator _eidGenerator = new IDGenerator();

        private IDictionary<string, string> _config;
        private IDictionary<string, object> _storage;

        private Action _sendQuest;
        private Action _openEvent;
        private DumpDelegate _clientDump;

        private bool _clearRumId;
        private bool _clearEvents;
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

        public void Init(bool clearRumId, bool clearEvents, DumpDelegate clientDump) {
            this._clearRumId = clearRumId;
            this._clearEvents = clearEvents;
            this._clientDump = clientDump;
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

                if (this._clearEvents) {
                    ((IDictionary<string, object>)this._storage[this._rumEventKey]).Clear();

                    if (this._debug) {
                        Debug.LogWarning("[RUM] storage clear! storage_key: " + this._rumEventKey);
                    }
                }

                if (this._clearRumId) {
                    ((IDictionary<string, object>)this._storage[this._rumIdKey]).Clear();

                    if (this._debug) {
                        Debug.LogWarning("[RUM] storage clear! rid_key: " + this._rumIdKey);
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

            this._initDump = this.DumpEventCount();

            if (this._debug) {
                Debug.Log("[RUM] DUMP: " + this._initDump);
            }

            List<object> items = this.ReshapeStorage();
            this.BuildRumId();

            if (!this.IsNullOrEmpty(items)) {
                this.WriteEvents(items);
            }

            if (this._openEvent != null) {
                this._openEvent();
            }
        }

        private List<object> ReshapeStorage() {
            List<object> event_items = new List<object>();

            lock (storage_locker) {
                IDictionary<string, object> rum_event = (IDictionary<string, object>)this._storage[this._rumEventKey];

                if (this.IsNullOrEmpty(rum_event)) {
                    return event_items;
                }

                if (rum_event.ContainsKey("event_map_1")) {
                    rum_event.Clear();
                    return event_items;
                }

                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                if (this.IsNullOrEmpty(event_cache)) {
                    return event_items;
                }

                try {
                    foreach (List<object> items in event_cache.Values) {
                        event_items.AddRange(items);
                    }
                } catch (Exception ex) {
                    if (this._debug) {
                        Debug.LogWarning(ex);
                    }
                }

                event_cache.Clear();
            }

            return event_items;
        }

        public long GenEventId() {
            return this._eidGenerator.Gen();
        }

        private string _initDump;

        public string GetInitDump() {
            return this._initDump;
        }

        private object self_locker = new object();

        public void SetSession(long value) {
            lock (self_locker) {
                this._session = value;
            }
        }

        private ConfigLocker config_locker = new ConfigLocker();

        public void UpdateConfig(IDictionary<string, object> value) {
            if (this.IsNullOrEmpty(value)) {
                return;
            }

            lock (config_locker) {
                if (this._config == null) {
                    this._config = new Dictionary<string, string>();
                }

                this._config.Clear();

                foreach (string key in value.Keys) {
                    List<object> list = null;

                    try {
                        list = (List<object>)value[key];
                    } catch (Exception ex) {
                        ErrorRecorderHolder.recordError(ex);
                    }

                    if (this.IsNullOrEmpty(list)) {
                        return;
                    }

                    foreach (object obj in list) {
                        if (PRIORITY_MAP.ContainsKey(key)) {
                            this._config.Add((string)obj, PRIORITY_MAP[key]);
                        }
                    }
                }

                config_locker.Status = 1;
            }
        }

        private long _cacheWriteCount;

        private List<object> _eventCache = new List<object>();

        public void WriteEvent(IDictionary<string, object> dict) {
            if (this.IsNullOrEmpty(dict)) {
                ErrorRecorderHolder.recordError(new Exception("Null or Empty IDictionary<string, object>!"));
                return;
            }

            if (!dict.ContainsKey("ev")) {
                ErrorRecorderHolder.recordError(new Exception("Not contains key 'ev'!"));
                return;
            }

            bool isOpen = ("open" == Convert.ToString(dict["ev"]));

            lock (self_locker) {
                if (this._destroyed) {
                    return;
                }
            }

            lock (write_locker) {
                if (this._eventCache.Count < 5000) {
                    if (isOpen) {
                        this._eventCache.Insert(0, dict);
                    } else {
                        this._eventCache.Add(dict);
                    }
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

                try {
                    this._writeThread = new Thread(new ThreadStart(WriteThread));

                    if (this._writeThread.Name == null) {
                        this._writeThread.Name = "RUM-WRITE";
                    }

                    this._writeThread.Start();
                    this._writeEvent.Reset();
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }
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
                if (write_locker.Status == 1) {
                    write_locker.Status = 2;

                    try {
                        this._writeEvent.Set();
                    } catch (Exception ex) {
                        ErrorRecorderHolder.recordError(ex);
                    }

                    RUMEvent self = this;
                    FPManager.Instance.DelayTask(100, (state) => {
                        List<object> list;

                        lock (write_locker) {
                            write_locker.Status = 0;
                            list = self._eventCache;
                            self._eventCache = new List<object>();
                        }

                        if (!self.IsNullOrEmpty(list)) {
                            self.WriteEvents(list);

                            lock (self_locker) {
                                self._cacheWriteCount += list.Count;
                            }

                            self.SaveStorage();
                        }
                    }, null);
                }
            }
        }

        private void WriteStorage(ICollection<object> list) {
            if (!this.IsNullOrEmpty(list)) {
                this.WriteEvents(list);

                lock (self_locker) {
                    this._cacheWriteCount += list.Count;
                }
            }

            if (this._sendQuest != null) {
                this._sendQuest();
            }

            this.SaveStorage();
        }

        private void WriteEvents(ICollection<object> items) {
            if (this.IsNullOrEmpty(items)) {
                return;
            }

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

        private long _cacheDisableCount;

        private void AddEvent(IDictionary<string, object> dict) {
            string key = null;

            if (dict.ContainsKey("ev")) {
                key = Convert.ToString(dict["ev"]);
            }

            string storageKey = this.SelectKey(key);

            if (string.IsNullOrEmpty(storageKey)) {
                lock (self_locker) {
                    this._cacheDisableCount++;
                }

                if (this._debug) {
                    Debug.LogWarning(String.Format("Event Disable! ev: {0}", key));
                }
                return;
            }

            this.TrimEmptyKey(dict);

            lock (storage_locker) {
                IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP);

                if (event_map == null) {
                    return;
                }

                List<object> event_list = new List<object>();

                if (event_map.ContainsKey(storageKey)) {
                    event_list = (List<object>)event_map[storageKey];
                } else {
                    event_map.Add(storageKey, event_list);
                }

                if (event_list.Count < RUMConfig.EVENT_QUEUE_LIMIT) {
                    if ("open" == key) {
                        event_list.Insert(0, dict);
                    } else {
                        event_list.Add(dict);
                    }
                }

                if (event_list.Count == (RUMConfig.EVENT_QUEUE_LIMIT - 2)) {
                    ErrorRecorderHolder.recordError(new Exception(String.Format("Event Queue Limit! storage key: {0}", storageKey)));
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
                if (value <= 0) {
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

            lock (second_locker) {
                this._delayCount = 0;
                this._timestamp = 0;
            }
        }

        public string DumpEventCount() {
            long cache_write_count = 0;
            long cache_disable_count = 0;
            int cache_current_count = 0;
            long file_read_count = 0;
            long file_write_count = 0;
            int storage_high_count = 0;
            int storage_medium_count = 0;
            int storage_low_count = 0;
            int storage_cache_count = 0;
            string dump = Convert.ToString(FPManager.Instance.GetMilliTimestamp());

            try {
                if (this._clientDump != null) {
                    dump = this._clientDump();
                }

                lock (self_locker) {
                    cache_write_count = this._cacheWriteCount;
                    cache_disable_count = this._cacheDisableCount;
                    file_read_count = this._fileReadCount;
                    file_write_count = this._fileWriteCount;
                }

                lock (write_locker) {
                    cache_current_count = this._eventCache.Count;
                }

                dump = String.Format("{0},cache:[{1}:{2},{3}:{4},{5}:{6}],file:[{7}:{8},{9}:{10}]", dump
                        , "write~", cache_write_count
                        , "disable~", cache_disable_count
                        , "current", cache_current_count
                        , "read~", file_read_count
                        , "write~", file_write_count);

                lock (storage_locker) {
                    storage_high_count = this.GetStorageCount(HIGH_LEVEL);
                    storage_medium_count = this.GetStorageCount(MEDIUM_LEVEL);
                    storage_low_count = this.GetStorageCount(LOW_LEVEL);
                    IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                    if (!this.IsNullOrEmpty(event_cache)) {
                        foreach (ICollection<object> items in event_cache.Values) {
                            if (items != null) {
                                storage_cache_count += items.Count;
                            }
                        }
                    }
                }

                dump = String.Format("{0},storage:[{1}:{2},{3}:{4},({5}:{6},{7}:{8},{9}:{10})]", dump
                        , "cache", storage_cache_count
                        , "map", storage_high_count + storage_medium_count + storage_low_count
                        , "high", storage_high_count
                        , "medium", storage_medium_count
                        , "low", storage_low_count);
            } catch (Exception ex) {
                if (this._debug) {
                    Debug.LogWarning(ex);
                }
            }

            return String.Format("{0},{1}", FPManager.Instance.GetMilliTimestamp(), dump);
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

        private void AddToCache(List<object> items) {
            if (this.IsNullOrEmpty(items)) {
                return;
            }

            IDictionary<string, object> item = (IDictionary<string, object>) items[0];

            if (this.IsNullOrEmpty(item)) {
                return;
            }

            if (!item.ContainsKey("eid")) {
                ErrorRecorderHolder.recordError(new Exception("Fail To Add! not contains key: 'eid'"));
                return;
            }

            string cache_key = Convert.ToString(item["eid"]);

            if (string.IsNullOrEmpty(cache_key)) {
                ErrorRecorderHolder.recordError(new Exception("Fail To Add! cache key is null or empty"));
                return;
            }

            IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

            if (event_cache == null) {
                return;
            }

            if (event_cache.ContainsKey(cache_key)) {
                ErrorRecorderHolder.recordError(new Exception(String.Format("Fail To Add! cache has been contains key: {0}", cache_key)));
                return;
            }

            event_cache.Add(cache_key, items);
        }

        private void UnshiftToStorage(List<object> items) {
            lock (storage_locker) {
                IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP);

                if (this.IsNullOrEmpty(event_map)) {
                    return;
                }

                if (event_map.ContainsKey(HIGH_LEVEL)) {
                    List<object> event_list = (List<object>)event_map[HIGH_LEVEL];
                    event_list.InsertRange(0, items);
                }
            }
        }

        public void RemoveFromCache(List<object> items, bool unshift)  {
            if (this.IsNullOrEmpty(items)) {
                return;
            }

            if (unshift) {
                this.UnshiftToStorage(items);
            }

            IDictionary<string, object> item = (IDictionary<string, object>) items[0];

            if (this.IsNullOrEmpty(item)) {
                return;
            }

            if (!item.ContainsKey("eid")) {
                ErrorRecorderHolder.recordError(new Exception("Fail To Remove! not contains key: 'eid'"));
                return;
            }

            string cache_key = Convert.ToString(item["eid"]);

            if (string.IsNullOrEmpty(cache_key)) {
                ErrorRecorderHolder.recordError(new Exception("Fail To Remove! cache key is null or empty"));
                return;
            }

            lock (storage_locker) {
                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                if (event_cache == null) {
                    return;
                }

                if (event_cache.ContainsKey(cache_key)) {
                    event_cache.Remove(cache_key);
                }
            }
        }

        private IDictionary<string, object> GetEventMap(string key) {
            if (this.IsNullOrEmpty(this._storage)) {
                return null;
            }

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

            return this.ShiftEventsFromStorage(sizeLimit, true);
        }

        private List<object> GetFileEvents() {
            return this.PopEventsFromStorage(RUMConfig.LOCAL_FILE_SIZE, false);
        }

        private List<object> ShiftEventsFromStorage(int size, bool catchAble) {
            List<object> items = new List<object>();
            int countLimit = this.GetCountLimit(size);

            if (countLimit <= 0) {
                return items;
            }

            lock (storage_locker) {
                for (int index = 1; index <= PRIORITY_MAP.Count; index++) {
                    if (items.Count >= countLimit) {
                        break;
                    }

                    this.ShiftEventRange(PRIORITY_MAP[Convert.ToString(index)], countLimit - items.Count, ref items);
                }

                if (catchAble) {
                    this.AddToCache(items);
                }
            }

            return items;
        }

        private void ShiftEventRange(string key, int countLimit, ref List<object> items) {
            IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP);

            if (this.IsNullOrEmpty(event_map)) {
                return;
            }

            List<object> event_list = new List<object>();

            if (event_map.ContainsKey(key)) {
                event_list = (List<object>) event_map[key];
            }

            if (this.IsNullOrEmpty(event_list)) {
                return;
            }

            int index = 0;
            int count = Math.Min(countLimit, event_list.Count);
            List<object> range = event_list.GetRange(index, count);
            items.AddRange(range);
            event_list.RemoveRange(index, count);
        }

        private List<object> PopEventsFromStorage(int size, bool catchAble) {
            List<object> items = new List<object>();
            int countLimit = this.GetCountLimit(size);

            if (countLimit <= 0) {
                return items;
            }

            lock (storage_locker) {
                for (int index = PRIORITY_MAP.Count; index > 0; index--) {
                    if (items.Count >= countLimit) {
                        break;
                    }

                    this.PopEventRange(PRIORITY_MAP[Convert.ToString(index)], countLimit - items.Count, ref items);
                }

                if (catchAble) {
                    this.AddToCache(items);
                }
            }

            return items;
        }

        private void PopEventRange(string key, int countLimit, ref List<object> items) {
            IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP);

            if (this.IsNullOrEmpty(event_map)) {
                return;
            }

            List<object> event_list = new List<object>();

            if (event_map.ContainsKey(key)) {
                event_list = (List<object>) event_map[key];
            }

            if (this.IsNullOrEmpty(event_list)) {
                return;
            }

            int count = Math.Min(countLimit, event_list.Count);
            int index = event_list.Count - count;
            List<object> range = event_list.GetRange(index, count);
            items.InsertRange(0, range);
            event_list.RemoveRange(index, count);
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

            return (int) Math.Ceiling(size / (storageSize / storegeCount * 1.0f));
        }

        private void TrimEmptyKey(IDictionary<string, object> item) {
            List<string> keys = new List<string>(item.Keys);

            if (this.IsNullOrEmpty(keys)) {
                return;
            }

            foreach (string k in keys) {
                if (k == "status") {
                    continue;
                }

                if (this.IsNullOrEmpty(item[k])) {
                    item.Remove(k);
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
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                    storage = null;
                }
            }

            if (needClear && storage.ContainsKey(this._rumEventKey)) {
                ((IDictionary<string, object>)storage[this._rumEventKey]).Clear();

                if (this._debug) {
                    Debug.LogWarning("[RUM] storage size limit, clear events!");
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
                return new byte[0];
            }

            if (storage_bytes.Length > 2 * RUMConfig.STORAGE_SIZE_MAX) {
                if (this._debug) {
                    Debug.LogWarning("[RUM] storage size limit, will be clear!");
                }

                lock (storage_locker) {
                    ((IDictionary<string, object>)this._storage[this._rumEventKey]).Clear();
                }

                ErrorRecorderHolder.recordError(new Exception("Storage Size Limit!"));
                this.UpdateStorageSize(160);
                return new byte[0];
            }

            this.UpdateStorageSize(storage_bytes.Length);
            return storage_bytes;
        }

        private void UpdateStorageSize(int size) {
            int count = 0;

            lock (storage_locker) {
                count += this.GetStorageCount(HIGH_LEVEL);
                count += this.GetStorageCount(MEDIUM_LEVEL);
                count += this.GetStorageCount(LOW_LEVEL);
                IDictionary<string, object> event_cache = this.GetEventMap(EVENT_CACHE);

                if (!this.IsNullOrEmpty(event_cache)) {
                    foreach (ICollection<object> items in event_cache.Values) {
                        if (items != null) {
                            count += items.Count;
                        }
                    }
                }
            }

            lock (self_locker) {
                this._storageCount = count;
                this._storageSize = size;
            }
        }

        private int GetStorageCount(string key) {
            int count = 0;
            IDictionary<string, object> event_map = this.GetEventMap(EVENT_MAP);

            if (this.IsNullOrEmpty(event_map)) {
                return count;
            }

            ICollection<object> event_list = new List<object>();

            if (event_map.ContainsKey(key)) {
                event_list = (ICollection<object>) event_map[key];
            }

            if (this.IsNullOrEmpty(event_list)) {
                return count;
            }

            return event_list.Count;
        }

        private string BuildRumId() {
            bool first = false;
            string rum_id = this.UUID(0, 16, 'C');

            lock (storage_locker) {
                if (this.IsNullOrEmpty(this._storage)) {
                    return null;
                }

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

                try {
                    this._checkThread = new Thread(new ThreadStart(CheckThread));

                    if (this._checkThread.Name == null) {
                        this._checkThread.Name = "RUM-CHECK";
                    }

                    this._checkThread.Start();
                    this._checkEvent.Reset();
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }
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
                if (check_locker.Status == 1) {
                    check_locker.Status = 2;

                    try {
                        this._checkEvent.Set();
                    } catch (Exception ex) {
                        ErrorRecorderHolder.recordError(ex);
                    }

                    RUMEvent self = this;
                    FPManager.Instance.DelayTask(100, (state) => {
                        lock (check_locker) {
                            check_locker.Status = 0;
                        }

                        if (self._debug) {
                            Debug.Log("[RUM] DUMP: " + self.DumpEventCount());
                        }
                    }, null);
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
                needSave = this.SlipStorage();
            }

            if (size < RUMConfig.STORAGE_SIZE_MIN) {
                needSave = this.ReStorage();
            }

            if (needSave) {
                this.SaveStorage();
            }
        }

        private long _fileWriteCount;

        private bool SlipStorage() {
            List<object> list = this.GetFileEvents();

            if (this.IsNullOrEmpty(list)) {
                return false;
            }

            int count = list.Count;
            byte[] bytes = new byte[0];

            try {
                using (MemoryStream outputStream = new MemoryStream()) {
                    MsgPack.Serialize(list, outputStream);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    bytes = outputStream.ToArray();
                }
            } catch (Exception ex) {
                ErrorRecorderHolder.recordError(ex);
                this.WriteEvents(list);
                return false;
            }

            int index = 0;

            lock (self_locker) {
                index = this._writeIndex;
                this._fileWriteCount += count;
            }

            RUMFile.Result res = this._rumFile.SaveRumLog(index, bytes);

            if (res.success) {
                if (this._debug) {
                    Debug.Log("[RUM] write to file, index: " + index + ", count: " + count);
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

        private long _fileReadCount;

        private bool ReStorage() {
            RUMFile.Result res = this._rumFile.LoadRumLog();

            if (res.success) {
                List<object> items = null;

                try {
                    using (MemoryStream inputStream = new MemoryStream((byte[])res.content)) {
                        items = MsgPack.Deserialize<List<object>>(inputStream);
                    }
                } catch (Exception ex) {
                    ErrorRecorderHolder.recordError(ex);
                }

                int count = 0;

                if (!this.IsNullOrEmpty(items)) {
                    count = items.Count;
                    this.WriteEvents(items);
                }

                lock (self_locker) {
                    this._fileReadCount += count;
                }

                if (this._debug) {
                    Debug.Log("[RUM] success load from file! count: " + count);
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
            if (string.IsNullOrEmpty(innerKey)) {
                return null;
            }

            lock (config_locker) {
                if (config_locker.Status == 0) {
                    return HIGH_LEVEL;
                }

                if (this._config.ContainsKey(innerKey)) {
                    return this._config[innerKey];
                }
            }

            return null;
        }
    }
}