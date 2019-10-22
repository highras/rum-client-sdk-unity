using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;

namespace com.rum {

    public class RUMClient {

        private class PingLocker {

            public int Status = 0;
            public int Count = 0;
            public long Timestamp = 0;
        }

        private class TryConnLocker {

            public int Status = 0;
            public long Timestamp = 0;
        }

        private FPEvent _event = new FPEvent();

        public FPEvent GetEvent() {
            return this._event;
        }

        private static object Pids_Locker = new object();
        private static List<int> PIDS = new List<int>();

        private IDGenerator _midGenerator = new IDGenerator();

        private string _secret;
        private string _uid;
        private string _appv;

        private bool _debug = true;
        private string _endpoint;

        private int _pid = 0;
        private RUMEvent _rumEvent;
        private FPClient _baseClient;

        private long _session = 0;

        private int _sendCount = 0;
        private int _pingLatency = 0;
        private int _configVersion = 0;

        private long _initSession = 0;

        public RUMClient(int pid, string secret, string uid, string appv, bool debug) {
            this.Init(pid, secret, uid, appv, debug, false, false);
        }

        public RUMClient(int pid, string secret, string uid, string appv, bool debug, bool clearRumId, bool clearEvents) {
            this.Init(pid, secret, uid, appv, debug, clearRumId, clearEvents);
        }

        private void Init(int pid, string secret, string uid, string appv, bool debug, bool clearRumId, bool clearEvents) {
            Debug.Log("[RUM] rum_sdk@" + RUMConfig.VERSION + ", fpnn_sdk@" + FPConfig.VERSION);

            if (!RUMPlatform.HasInit()) {
                RUMRegistration.Register(new LocationService());
            }

            lock (Pids_Locker) {
                if (RUMClient.PIDS.Contains(pid)) {
                    if (debug) {
                        throw new Exception("The Same Project Id, Instance Limit!");
                    }

                    Debug.LogError("[RUM] The Same Project Id, Instance Limit!");
                    return;
                }

                RUMClient.PIDS.Add(pid);
            }

            this._pid = pid;
            this._secret = secret;
            this._uid = uid;
            this._appv = appv;
            this._debug = debug;
            this._rumEvent = new RUMEvent(this._pid, this._debug, OnSendQuest, OpenEvent);
            this._initSession = this._rumEvent.GenEventId();
            FPManager.Instance.AddSecond(OnSecondDelegate);
            ErrorRecorderHolder.setInstance(new RUMErrorRecorder(this._debug, WriteEvent));
            this.AddPlatformListener();
            this._rumEvent.Init(clearRumId, clearEvents, DumpEventCount);
        }

        private void OnSecondDelegate(EventData evd) {
            this.OnSecond(evd.GetTimestamp());
        }

        private object self_locker = new object();

        public void Destroy() {
            int pid = 0;

            lock (tryconn_locker) {
                tryconn_locker.Status = 0;
            }

            lock (ping_locker) {
                ping_locker.Status = 0;
            }

            this.RemovePlatformListener();
            FPManager.Instance.RemoveSecond(OnSecondDelegate);

            lock (self_locker) {
                pid = this._pid;
                this._event.FireEvent(new EventData("close"));
                this._event.RemoveListener();

                if (this._rumEvent != null) {
                    this._rumEvent.Destroy();
                }

                if (this._baseClient != null) {
                    this._baseClient.Close();
                    this._baseClient = null;
                }
            }

            lock (Pids_Locker) {
                if (RUMClient.PIDS.Contains(pid)) {
                    RUMClient.PIDS.Remove(pid);
                }
            }
        }

        public void Connect(string endpoint) {
            lock (self_locker) {
                this._endpoint = endpoint;

                if (this._baseClient != null) {
                    this.GetEvent().FireEvent(new EventData("error", new Exception("client has been init!")));
                    return;
                }

                this._baseClient = new FPClient(this._endpoint, RUMConfig.CONNCT_INTERVAL);
                this._baseClient.Client_Connect = BaseClient_Connect;
                this._baseClient.Client_Close = BaseClient_Close;
                this._baseClient.Client_Error = BaseClient_Error;
                this._baseClient.Connect();
            }
        }

        private void BaseClient_Connect(EventData evd) {
            if (this._debug) {
                Debug.Log("[RUM] connect on rum agent!");
            }

            this.GetEvent().FireEvent(new EventData("ready"));
            this.StartPing();
        }

        private void BaseClient_Close(EventData evd) {
            if (this._debug) {
                Debug.Log("[RUM] close from rum agent!");
            }

            this.StopPing();
            this._rumEvent.SetTimestamp(0);

            lock (self_locker) {
                if (this._baseClient != null) {
                    this._baseClient = null;
                }

                this._sendCount = 0;
                this._configVersion = 0;
            }

            lock (tryconn_locker) {
                tryconn_locker.Status = 1;
                tryconn_locker.Timestamp = FPManager.Instance.GetMilliTimestamp();
            }

            this.GetEvent().FireEvent(new EventData("close"));
        }

        private void BaseClient_Error(EventData evd) {
            ErrorRecorderHolder.recordError(evd.GetException());
        }

        public long GetSession() {
            lock (self_locker) {
                return this._session;
            }
        }

        public string GetRumId() {
            return this._rumEvent.GetRumId();
        }

        public void SetUid(string value) {
            lock (self_locker) {
                if (!string.IsNullOrEmpty(this._uid)) {
                    if (this._debug) {
                        Debug.LogWarning("[RUM] uid exist, uid: " + this._uid);
                    }

                    return;
                }

                this._uid = value;
            }

            if (!string.IsNullOrEmpty(value)) {
                IDictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("uid", value);
                this.AppendEvent("uid", dict);
            }
        }

        public void CustomEvent(string ev, IDictionary<string, object> attrs) {
            if (attrs == null) {
                return;
            }

            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("attrs", new Dictionary<string, object>(attrs));
            this.WriteEvent(ev, dict);
        }

        public void HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attrs) {
            if (string.IsNullOrEmpty(url)) {
                return;
            }

            IDictionary<string, object> dict = new Dictionary<string, object>() {
                { "url", url },
                { "method", method },
                { "status", status },
                { "reqsize", reqsize },
                { "respsize", respsize },
                { "latency", latency }
            };

            if (attrs != null) {
                dict.Add("attrs", new Dictionary<string, object>(attrs));
            }

            this.WriteEvent("http", dict);

            if (status <= 0 || status >= 300) {
                IDictionary<string, object> err_dict = new Dictionary<string, object>() {
                    { "url", url },
                    { "method", method },
                    { "status", status },
                    { "reqsize", reqsize },
                    { "respsize", respsize },
                    { "latency", latency }
                };

                if (attrs != null) {
                    err_dict.Add("attrs", new Dictionary<string, object>(attrs));
                }

                this.WriteEvent("httperr", err_dict);
                return;
            }

            if (latency > 1000) {
                IDictionary<string, object> lat_dict = new Dictionary<string, object>() {
                    { "url", url },
                    { "method", method },
                    { "status", status },
                    { "reqsize", reqsize },
                    { "respsize", respsize },
                    { "latency", latency }
                };

                if (attrs != null) {
                    lat_dict.Add("attrs", new Dictionary<string, object>(attrs));
                }

                this.WriteEvent("httplat", lat_dict);
                return;
            }
        }

        public void HookHttp(System.Net.HttpWebRequest req, System.Net.HttpWebResponse res, int latency) {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            IDictionary<string, object> attrs = new Dictionary<string, object>();

            if (req != null) {
                dict.Add("url", req.Address);
                dict.Add("method", req.Method);
                dict.Add("reqsize", req.ContentLength);

                if (req.ContentType != null) {
                    if (!string.IsNullOrEmpty(req.ContentType)) {
                        attrs.Add("Request-Content-Type", req.ContentType);
                    }
                }

                if (req.Timeout > 0) {
                    attrs.Add("Request-Timeout", req.Timeout);
                }

                if (req.TransferEncoding != null) {
                    if (!string.IsNullOrEmpty(req.TransferEncoding)) {
                        attrs.Add("Request-Transfer-Encoding", req.TransferEncoding);
                    }
                }

                if (req.UserAgent != null) {
                    if (!string.IsNullOrEmpty(req.UserAgent)) {
                        attrs.Add("Request-User-Agent", req.UserAgent);
                    }
                }
            }

            if (res != null) {
                dict.Add("status", res.StatusCode);
                dict.Add("respsize", res.ContentLength);
                dict.Add("latency", latency);

                if (res.ContentEncoding != null) {
                    if (!string.IsNullOrEmpty(res.ContentEncoding)) {
                        attrs.Add("Response-ContentEncoding", res.ContentEncoding);
                    }
                }

                if (res.ContentType != null) {
                    if (!string.IsNullOrEmpty(res.ContentType)) {
                        attrs.Add("Response-ContentType", res.ContentType);
                    }
                }

                if (res.StatusDescription != null) {
                    if (!string.IsNullOrEmpty(res.StatusDescription)) {
                        attrs.Add("Response-StatusDescription", res.StatusDescription);
                    }
                }
            }

            dict.Add("attrs", attrs);
            this.HttpEvent(dict);
        }

        public void HookHttp(UnityEngine.Networking.UnityWebRequest req, int latency) {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            IDictionary<string, object> attrs = new Dictionary<string, object>();

            if (req != null) {
                dict.Add("url", req.url);
                dict.Add("method", req.method);
                dict.Add("reqsize", req.uploadedBytes);
                dict.Add("respsize", req.downloadedBytes);
                dict.Add("status", req.responseCode);
                dict.Add("latency", latency);

                if (req.uploadHandler != null) {
                    if (!string.IsNullOrEmpty(req.uploadHandler.contentType)) {
                        attrs.Add("Request-Content-Type", req.uploadHandler.contentType);
                    }
                }

                if (!string.IsNullOrEmpty(req.error)) {
                    attrs.Add("error", req.error);
                }
            }

            dict.Add("attrs", attrs);
            this.HttpEvent(dict);
        }

        private void HttpEvent(IDictionary<string, object> dict) {
            string url = null;

            if (dict == null) {
                return;
            }

            if (dict.ContainsKey("url")) {
                url = Convert.ToString(dict["url"]);
            }

            string method = null;

            if (dict.ContainsKey("method")) {
                method = Convert.ToString(dict["method"]);
            }

            int status = 0;

            if (dict.ContainsKey("status")) {
                status = Convert.ToInt32(dict["status"]);
            }

            long reqsize = 0;

            if (dict.ContainsKey("reqsize")) {
                reqsize = Convert.ToInt64(dict["reqsize"]);
            }

            long respsize = 0;

            if (dict.ContainsKey("respsize")) {
                respsize = Convert.ToInt64(dict["respsize"]);
            }

            int latency = 0;

            if (dict.ContainsKey("latency")) {
                latency = Convert.ToInt32(dict["latency"]);
            }

            this.HttpEvent(url, method, status, reqsize, respsize, latency, (IDictionary<string, object>)dict["attrs"]);
        }

        private string DumpEventCount() {
            long event_write_count = 0;
            long event_send_count = 0;
            string dump = null;

            try {
                lock (self_locker) {
                    event_write_count = this._eventWriteCount;
                    event_send_count = this._eventSendCount;
                }

                dump = String.Format("event:[{0}:{1},{2}:{3}]"
                        , "write~", event_write_count
                        , "send~", event_send_count);
            } catch (Exception ex) {
                Debug.LogError(ex);
            }

            return dump;
        }

        private void DumpEvent() {
            FPManager.Instance.ExecTask(AsyncDumpEvent, null);
        }

        private void AsyncDumpEvent(object state) {
            IDictionary<string, object> dict = new Dictionary<string, object>() {
                { "init_dump", this._rumEvent.GetInitDump()},
                { "proc_dump", this._rumEvent.DumpEventCount()}
            };
            dict.Add("type", "unity_dump_info");

            if (this._debug) {
                Debug.Log("[RUM] dump info: " + Json.SerializeToString(dict));
            }

            this.WriteEvent("info", dict);
        }

        private void AppendEvent(string type, IDictionary<string, object> dict) {
            if (!string.IsNullOrEmpty(type)) {
                dict.Add("type", type);
                this.WriteEvent("append", dict);
            }
        }

        private void OnPlatformDelegate(EventData evd) {
            if (this._debug) {
                Debug.Log("[RUM] platform events: " + Json.SerializeToString(evd.GetPayload()));
            }

            this.WriteEvent(null, (IDictionary<string, object>)evd.GetPayload());
        }

        private void AddPlatformListener() {
            RUMPlatform.Instance.Event.AddListener(RUMPlatform.PLATFORM_EVENT, OnPlatformDelegate);
        }

        private void RemovePlatformListener() {
            RUMPlatform.Instance.Event.RemoveListener(RUMPlatform.PLATFORM_EVENT, OnPlatformDelegate);
        }

        private long _eventWriteCount;

        private void WriteEvent(string ev, IDictionary<string, object> dict) {
            if (!dict.ContainsKey("ev")) {
                dict.Add("ev", ev);
            }

            if (!dict.ContainsKey("eid")) {
                dict.Add("eid", this._rumEvent.GenEventId());
            }

            if (!dict.ContainsKey("pid")) {
                dict.Add("pid", this._pid);
            }

            lock (self_locker) {
                if (!dict.ContainsKey("sid")) {
                    if (this._session > 0) {
                        dict.Add("sid", this._session);
                    }
                }

                if (!dict.ContainsKey("uid")) {
                    dict.Add("uid", this._uid);
                }
            }

            if (!dict.ContainsKey("rid")) {
                string rumId = this._rumEvent.GetRumId();

                if (rumId != null) {
                    dict.Add("rid", rumId);
                }
            }

            if (!dict.ContainsKey("ts")) {
                long ts = this._rumEvent.GetTimestamp();

                if (ts > 0) {
                    dict.Add("ts", ts);
                }
            }

            // if (this._debug) {
            //     Debug.Log("[RUM] write event: " + Json.SerializeToString(dict));
            // }
            FPManager.Instance.ExecTask(AsyncWriteEvent, dict);

            lock (self_locker) {
                this._eventWriteCount++;
            }
        }

        private void AsyncWriteEvent(object state) {
            this._rumEvent.WriteEvent((IDictionary<string, object>)state);
        }

        private void OpenEvent() {
            lock (self_locker) {
                if (this._session > 0) {
                    return;
                }

                this._session = this._initSession;
                this._rumEvent.SetSession(this._session);
            }

            IDictionary<string, object> dict = new Dictionary<string, object>() {
                { "appv", this._appv },
                { "first", this._rumEvent.IsFirst() },
                { "v", RUMConfig.VERSION },
                { "sw", RUMPlatform.ScreenWidth },
                { "sh", RUMPlatform.ScreenHeight },
                { "manu", RUMPlatform.Manu },
                { "model", RUMPlatform.DeviceModel },
                { "os", RUMPlatform.OperatingSystem },
                { "osv", RUMPlatform.SystemVersion },
                { "nw", RUMPlatform.Network },
                { "carrier", RUMPlatform.Carrier },
                { "lang", RUMPlatform.SystemLanguage },
                { "from", RUMPlatform.From },
                { "eid", this._initSession }
            };
            this.WriteEvent("open", dict);
        }

        private void OnSecond(long timestamp) {
            this._rumEvent.OnSecond(timestamp);
            this.CheckPingCount();
            this.CheckDump(timestamp);
            this.CheckThreadPool(timestamp);
            this.TryConnect(timestamp);
        }

        private void CheckPingCount() {
            bool needClose = false;

            lock (ping_locker) {
                if (ping_locker.Status == 0) {
                    return;
                }

                if (ping_locker.Count >= 2) {
                    needClose = true;
                    ping_locker.Count = 0;
                }
            }

            if (needClose) {
                lock (self_locker) {
                    if (this._baseClient != null) {
                        this._baseClient.Close(new Exception("ping timeout"));
                    }
                }
            }
        }

        private long _dumpCheckTimestamp;

        private void CheckDump(long timestamp) {
            lock (self_locker) {
                if (this._dumpCheckTimestamp == 0) {
                    this._dumpCheckTimestamp = timestamp;
                }

                if (timestamp - this._dumpCheckTimestamp < RUMConfig.DUMP_INTERVAL) {
                    return;
                }

                this._dumpCheckTimestamp = timestamp;
            }

            this.DumpEvent();
        }

        private long _threadCheckTimestamp;

        private void CheckThreadPool(long timestamp) {
            int worker, io;
            System.Threading.ThreadPool.GetAvailableThreads(out worker, out io);

            if (Math.Min(worker, io) <= 1) {
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "available_io_threads", io },
                    { "available_worker_threads", worker }
                };
                dict.Add("type", "unity_available_threads");
                bool needWarn;

                lock (self_locker) {
                    needWarn = (timestamp - this._threadCheckTimestamp >= 30 * 1000);

                    if (needWarn) {
                        this._threadCheckTimestamp = timestamp;
                    }
                }

                if (this._debug) {
                    Debug.LogWarning("[RUM] available threads warn: " + Json.SerializeToString(dict));
                }

                if (needWarn) {
                    this.WriteEvent("warn", dict);
                }
            }
        }

        private TryConnLocker tryconn_locker = new TryConnLocker();

        private void TryConnect(long timestamp) {
            lock (tryconn_locker) {
                if (tryconn_locker.Status == 0) {
                    return;
                }

                if (timestamp - tryconn_locker.Timestamp < RUMConfig.CONNCT_INTERVAL) {
                    return;
                }

                tryconn_locker.Status = 0;
            }

            if (this._debug) {
                Debug.Log("[RUM] try connect...");
            }

            this.Connect(this._endpoint);
        }

        private void OnSendQuest() {
            this.SendPing();
            this.SendEvent();
        }

        private PingLocker ping_locker = new PingLocker();

        private void StartPing() {
            lock (ping_locker) {
                if (ping_locker.Status != 0) {
                    return;
                }

                ping_locker.Status = 1;
                ping_locker.Timestamp = FPManager.Instance.GetMilliTimestamp() - RUMConfig.PING_INTERVAL;
            }
        }

        private void StopPing() {
            lock (ping_locker) {
                ping_locker.Status = 0;
            }
        }

        private void SendPing() {
            long timestamp = 0;

            lock (ping_locker) {
                if (ping_locker.Status == 0) {
                    return;
                }

                timestamp = FPManager.Instance.GetMilliTimestamp();

                if (timestamp - ping_locker.Timestamp < RUMConfig.PING_INTERVAL) {
                    return;
                }

                ping_locker.Count++;
                ping_locker.Timestamp = timestamp;
            }

            long salt = this._midGenerator.Gen();
            IDictionary<string, object> payload = new Dictionary<string, object>() {
                { "pid", this._pid },
                { "sign", this.GenSign(salt) },
                { "salt", salt },
                { "ss", this._rumEvent.GetStorageSize() },
                { "rid", this._rumEvent.GetRumId() }
            };

            lock (self_locker) {
                payload.Add("uid", this._uid);
                payload.Add("sid", this._session);
                payload.Add("cv", this._configVersion);
                payload.Add("pt", this._pingLatency);
            }

            this.SendQuest("ping", payload, SendPing_OnCallback, RUMConfig.PING_INTERVAL);
        }

        private void SendPing_OnCallback(CallbackData cbd) {
            Exception ex = cbd.GetException();

            if (ex != null) {
                ErrorRecorderHolder.recordError(ex);
                return;
            }

            IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

            if (this._debug) {
                Debug.Log("[RUM] recv ping: " + Json.SerializeToString(dict));
            }

            if (dict == null) {
                return;
            }

            long timestamp = 0;

            lock (ping_locker) {
                ping_locker.Count--;
                timestamp = ping_locker.Timestamp;
            }

            lock (self_locker) {
                this._pingLatency = Convert.ToInt32(FPManager.Instance.GetMilliTimestamp() - timestamp);
            }

            if (dict.ContainsKey("ts")) {
                this._rumEvent.SetTimestamp(Convert.ToInt64(dict["ts"]));
            }

            if (dict.ContainsKey("bw")) {
                this._rumEvent.SetSizeLimit(Convert.ToInt32(dict["bw"]));
            }

            int cv = 0;

            if (dict.ContainsKey("cv")) {
                cv = Convert.ToInt32(dict["cv"]);
            }

            bool hasConfig = this._rumEvent.HasConfig();
            bool needLoad = !hasConfig;

            lock (self_locker) {
                if (hasConfig && this._configVersion != cv) {
                    this._configVersion = cv;
                    needLoad = true;
                }
            }

            if (needLoad) {
                this.LoadConfig();
            }
        }

        private void LoadConfig() {
            long salt = this._midGenerator.Gen();
            IDictionary<string, object> payload = new Dictionary<string, object>() {
                { "pid", this._pid },
                { "sign", this.GenSign(salt) },
                { "salt", salt },
                { "rid", this._rumEvent.GetRumId() },
                { "appv", this._appv },
                { "lang", RUMPlatform.SystemLanguage },
                { "manu", RUMPlatform.Manu },
                { "model", RUMPlatform.DeviceModel },
                { "os", RUMPlatform.OperatingSystem },
                { "osv", RUMPlatform.SystemVersion },
                { "nw", RUMPlatform.Network },
                { "carrier", RUMPlatform.Carrier },
                { "from", RUMPlatform.From }
            };

            lock (self_locker) {
                payload.Add("uid", this._uid);
            }

            this.SendQuest("getconfig", payload, LoadConfig_OnCallback, RUMConfig.SENT_TIMEOUT);
        }

        private void LoadConfig_OnCallback(CallbackData cbd) {
            Exception ex = cbd.GetException();

            if (ex != null) {
                lock (self_locker) {
                    this._configVersion = 0;
                }

                ErrorRecorderHolder.recordError(ex);
                return;
            }

            IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

            if (this._debug) {
                Debug.Log("[RUM] recv config: " + Json.SerializeToString(dict));
            }

            this._rumEvent.UpdateConfig((IDictionary<string, object>)dict["events"]);
            this.GetEvent().FireEvent(new EventData("config"));
        }

        private void SendEvent() {
            lock (ping_locker) {
                if (ping_locker.Status == 0) {
                    return;
                }
            }

            lock (self_locker) {
                if (this._sendCount >= RUMConfig.SENT_CONCURRENT) {
                    return;
                }
            }

            List<object> items = this._rumEvent.GetSentEvents();

            if (items.Count == 0) {
                return;
            }

            lock (self_locker) {
                this._sendCount++;
            }

            this.SendEvents(items);
        }

        private long _eventSendCount;

        private void SendEvents(List<object> items) {
            long salt = this._midGenerator.Gen();
            IDictionary<string, object> payload = new Dictionary<string, object>() {
                { "pid", this._pid },
                { "sign", this.GenSign(salt) },
                { "salt", salt },
                { "events", items }
            };
            RUMClient self = this;
            this.SendQuest("adds", payload, (cbd) => {
                lock (self_locker) {
                    if (self._sendCount > 0) {
                        self._sendCount--;
                    }

                    self._eventSendCount += items.Count;
                }

                self._rumEvent.RemoveFromCache(items);
                Exception ex = cbd.GetException();

                if (ex != null) {
                    self._rumEvent.WriteEvents(items);
                    ErrorRecorderHolder.recordError(ex);
                }
            }, RUMConfig.SENT_TIMEOUT);
        }

        private string GenSign(long salt) {
            lock (self_locker) {
                StringBuilder sb = new StringBuilder(70);
                sb.Append(Convert.ToString(this._pid));
                sb.Append(":");
                sb.Append(this._secret);
                sb.Append(":");
                sb.Append(Convert.ToString(salt));
                return this.CalcMd5(sb.ToString(), true);
            }
        }

        private void SendQuest(string method, IDictionary<string, object> payload, CallbackDelegate callback, int timeout) {
            byte[] bytes = new byte[0];

            try {
                using (MemoryStream outputStream = new MemoryStream()) {
                    MsgPack.Serialize(payload, outputStream);
                    outputStream.Seek(0, SeekOrigin.Begin);
                    bytes = outputStream.ToArray();
                }
            } catch (Exception ex) {
                if (callback != null) {
                    callback(new CallbackData(ex));
                }

                return;
            }

            FPData data = new FPData();
            data.SetFlag(0x1);
            data.SetMtype(0x1);
            data.SetMethod(method);
            data.SetPayload(bytes);

            lock (self_locker) {
                if (this._baseClient != null) {
                    this._baseClient.SendQuest(data, this.QuestCallback(callback), timeout);
                }
            }
        }

        private CallbackDelegate QuestCallback(CallbackDelegate callback) {
            RUMClient self = this;
            return (cbd) => {
                if (callback == null) {
                    return;
                }

                self.CheckFPCallback(cbd);
                callback(cbd);
            };
        }

        private void CheckFPCallback(CallbackData cbd) {
            bool isAnswerException = false;
            FPData data = cbd.GetData();
            IDictionary<string, object> payload = null;

            if (data != null) {
                if (data.GetFlag() == 0) {
                    try {
                        payload = Json.Deserialize<IDictionary<string, object>>(data.JsonPayload());
                    } catch (Exception ex) {
                        ErrorRecorderHolder.recordError(ex);
                    }
                }

                if (data.GetFlag() == 1) {
                    try {
                        using (MemoryStream inputStream = new MemoryStream(data.MsgpackPayload())) {
                            payload = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
                        }
                    } catch (Exception ex) {
                        ErrorRecorderHolder.recordError(ex);
                    }
                }

                lock (self_locker) {
                    if (this._baseClient != null) {
                        if (this._baseClient.GetPackage().IsAnswer(data)) {
                            isAnswerException = data.GetSS() != 0;
                        }
                    }
                }
            }

            cbd.CheckException(isAnswerException, payload);
        }

        private string CalcMd5(string str, bool upper) {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(str);
            return CalcMd5(inputBytes, upper);
        }

        private string CalcMd5(byte[] bytes, bool upper) {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(bytes);
            string f = "x2";

            if (upper) {
                f = "X2";
            }

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString(f));
            }

            return sb.ToString();
        }

        private class RUMErrorRecorder: ErrorRecorder {

            private bool _debug;
            private Action<string, IDictionary<string, object>> _writeEvent;

            public RUMErrorRecorder(bool debug, Action<string, IDictionary<string, object>> writeEvent) {
                this._debug = debug;
                this._writeEvent = writeEvent;
            }

            public override void recordError(Exception ex) {
                if (this._debug) {
                    Debug.LogError(ex);
                }

                this.WriteDebug("rum_exception", ex);
            }

            private void WriteDebug(string type, Exception ex) {
                this.OnException("debug", type, ex.Message, ex.StackTrace);
            }

            private void WriteException(string type, Exception ex) {
                this.OnException("error", type, ex.Message, ex.StackTrace);
            }

            private void OnException(string ev, string type, string message, string stack) {
                IDictionary<string, object> dict = new Dictionary<string, object>() {
                    { "type", type },
                    { "message", message },
                    { "stack", stack }
                };

                if (this._writeEvent != null) {
                    this._writeEvent(ev, dict);
                }
            }
        }
    }

    public class IDGenerator {

        private long count = 0;
        private StringBuilder sb = new StringBuilder(20);
        private object lock_obj = new object();

        public long Gen() {
            lock (lock_obj) {
                if (++count > 999) {
                    count = 1;
                }

                sb.Length = 0;
                sb.Append(FPManager.Instance.GetMilliTimestamp());

                if (count < 100) {
                    sb.Append("0");
                }

                if (count < 10) {
                    sb.Append("0");
                }

                sb.Append(count);
                return Convert.ToInt64(sb.ToString());
            }
        }
    }

    public static class RUMRegistration {

        static public void Register(LocationService location) {
            FPManager.Instance.Init();
            RUMPlatform.Instance.Init(location);
        }
    }
}