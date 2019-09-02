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

    public static class RUMRegistration {

        static public void Register(LocationService location) {

            FPManager.Instance.Init();
            RUMPlatform.Instance.Init(location);
        }
    }

    public class RUMClient {

        private static class MidGenerator {

            static private long Count = 0;
            static private StringBuilder sb = new StringBuilder(20);
            static object lock_obj = new object();

            static public long Gen() {

                lock (lock_obj) {

                    if (++Count > 999) {

                        Count = 1;
                    }

                    long c = Count;

                    //.Net >= 4.0  sb.Clear();
                    sb.Length = 0;
                    sb.Append(FPManager.Instance.GetMilliTimestamp());

                    if (c < 100) {

                        sb.Append("0");
                    }

                    if (c < 10) {

                        sb.Append("0");
                    }

                    sb.Append(c);

                    return Convert.ToInt64(sb.ToString());
                }
            }
        }

        private class PingLocker {

            public int Status = 0;
        }

        private class TryConnLocker {

            public int Status = 0;
        }

        private FPEvent _event = new FPEvent();

        public FPEvent GetEvent() {

            return this._event;
        }

        private static object Pids_Locker = new object();
        private static List<int> PIDS = new List<int>();

        private string _secret;
        private string _uid;
        private string _appv;

        private bool _debug = true;
        private string _endpoint;

        private int _pid = 0;
        private RUMEvent _rumEvent;
        private FPClient _baseClient;

        private long _pingEid = 0;
        private long _session = 0;
        private long _lastPingTime = 0;
        private long _lastConnectTime = 0;

        private int _pingCount = 0;
        private int _sendCount = 0;
        private int _pingLatency = 0;
        private int _writeCount = 0;
        private int _configVersion = 0;

        private EventDelegate _eventDelegate;

        public RUMClient(int pid, string secret, string uid, string appv, bool debug) {

            Debug.Log("[RUM] rum_sdk@" + RUMConfig.VERSION + ", fpnn_sdk@" + FPConfig.VERSION);

            if (!RUMPlatform.HasInit()) {

                RUMPlatform.Instance.Init(null); 
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
            this._rumEvent = new RUMEvent(this._pid, this._debug, this.OnSendQuest, this.OpenEvent);

            RUMClient self = this;

            this._eventDelegate = (evd) => {

                self.OnSecond(evd.GetTimestamp());
            };

            FPManager.Instance.AddSecond(this._eventDelegate);
            ErrorRecorderHolder.setInstance(new RUMErrorRecorder(this._debug, WriteEvent));

            this.AddPlatformListener();
            this._rumEvent.Init();
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

            lock (self_locker) {

                pid = this._pid;

                if (this._eventDelegate != null) {

                    FPManager.Instance.RemoveSecond(this._eventDelegate);
                    this._eventDelegate = null;
                }

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

        public void Connect(string endpoint, bool clearRumId, bool clearEvents) {

            lock (self_locker) {

                this._endpoint = endpoint;

                if (this._baseClient != null) {

                    this.GetEvent().FireEvent(new EventData("error", new Exception("client has been init!")));
                    return;
                }

                if (clearRumId) {

                    this._rumEvent.ClearRumId();
                }

                if (clearEvents) {

                    this._rumEvent.ClearEvents();
                }

                RUMClient self = this;

                this._baseClient = new FPClient(this._endpoint, RUMConfig.CONNCT_INTERVAL);

                this._baseClient.Client_Connect = (evd) => {

                    if (self._debug) {

                        Debug.Log("[RUM] connect on rum agent!");
                    }
                    
                    self.GetEvent().FireEvent(new EventData("ready"));
                    self.StartPing();
                };

                this._baseClient.Client_Close = (evd) => {

                    if (self._debug) {

                        Debug.Log("[RUM] close from rum agent!");
                    }

                    self.StopPing();
                    self._rumEvent.SetTimestamp(0);

                    lock (self_locker) {

                        if (self._baseClient != null) {

                            self._baseClient = null;
                        }

                        self._sendCount = 0;
                        self._configVersion = 0;
                    }

                    lock (tryconn_locker) {

                        tryconn_locker.Status = 1;
                        self._lastConnectTime = FPManager.Instance.GetMilliTimestamp();
                    }

                    self.GetEvent().FireEvent(new EventData("close"));
                };

                this._baseClient.Client_Error = (evd) => {

                    ErrorRecorderHolder.recordError(evd.GetException());
                };

                this._baseClient.Connect();
            }
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

                        Debug.Log("[RUM] uid exist, uid: " + this._uid);
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

        private void AppendEvent(string type, IDictionary<string, object> dict) {

            if (!string.IsNullOrEmpty(type)) {

                dict.Add("type", type);
                this.WriteEvent("append", dict);
            }
        }

        private EventDelegate _fpsDelegate;
        private EventDelegate _infoDelegate;
        private EventDelegate _appFgDelegate;
        private EventDelegate _appBgDelegate;
        private EventDelegate _exceptionDelegate;
        private EventDelegate _geoDelegate;
        private EventDelegate _netwrokDelegate;
        private EventDelegate _memoryDelegate;

        private void AddPlatformListener() {

            RUMClient self = this;

            this._exceptionDelegate = (evd) => {

                self.WriteEvent(null, (IDictionary<string, object>)evd.GetPayload());
            };
            RUMPlatform.Instance.Event.AddListener("system_exception", this._exceptionDelegate);

            this._appFgDelegate = (evd) => {

                self.WriteEvent("fg", (IDictionary<string, object>)evd.GetPayload());
            };
            RUMPlatform.Instance.Event.AddListener("app_fg", this._appFgDelegate);

            this._appBgDelegate = (evd) => {

                self.WriteEvent("bg", (IDictionary<string, object>)evd.GetPayload());
            };
            RUMPlatform.Instance.Event.AddListener("app_bg", this._appBgDelegate);

            this._infoDelegate = (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>() {

                    { "type", "unity_system_info" },
                    { "system_info", evd.GetPayload() }
                };

                self.WriteEvent("info", dict);
            };
            RUMPlatform.Instance.Event.AddListener("system_info", this._infoDelegate);

            this._netwrokDelegate = (evd) => {

                self.WriteEvent("nwswitch", (IDictionary<string, object>)evd.GetPayload());
            };
            RUMPlatform.Instance.Event.AddListener("netwrok_switch", this._netwrokDelegate);

            this._fpsDelegate = (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>() {

                    { "type", "unity_fps_info" },
                    { "fps_info", evd.GetPayload() }
                };

                self.WriteEvent("info", dict);
            };
            RUMPlatform.Instance.Event.AddListener("fps_update", this._fpsDelegate);

            this._geoDelegate = (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>() {

                    { "type", "unity_geo_info" },
                    { "geo_info", evd.GetPayload() }
                };

                self.WriteEvent("info", dict);
            };
            RUMPlatform.Instance.Event.AddListener("geo_update", this._geoDelegate);

            this._memoryDelegate = (evd) => {

                self.WriteEvent("warn", (IDictionary<string, object>)evd.GetPayload());
            };
            RUMPlatform.Instance.Event.AddListener("memory_low", this._memoryDelegate);
        }

        private void RemovePlatformListener() {

            RUMPlatform.Instance.Event.RemoveListener("system_exception", this._exceptionDelegate);
            RUMPlatform.Instance.Event.RemoveListener("app_fg", this._appFgDelegate);
            RUMPlatform.Instance.Event.RemoveListener("app_bg", this._appBgDelegate);
            RUMPlatform.Instance.Event.RemoveListener("system_info", this._infoDelegate);
            RUMPlatform.Instance.Event.RemoveListener("netwrok_switch", this._netwrokDelegate);
            RUMPlatform.Instance.Event.RemoveListener("fps_update", this._fpsDelegate);
            RUMPlatform.Instance.Event.RemoveListener("geo_update", this._geoDelegate);
            RUMPlatform.Instance.Event.RemoveListener("memory_low", this._memoryDelegate);
        }

        private void WriteEvent(string ev, IDictionary<string, object> dict) {

            if (!dict.ContainsKey("ev")) {

                dict.Add("ev", ev);
            } 

            if (!dict.ContainsKey("eid")) {

                dict.Add("eid", MidGenerator.Gen());

                lock (ping_locker) {

                    this._writeCount++;
                }
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

            this._rumEvent.WriteEvent(dict);

            // if (this._debug) {

            //     Debug.Log("[RUM] write event: " + Json.SerializeToString(dict));
            // }
        }

        private void OpenEvent() {

            lock (self_locker) {

                if (this._session > 0) {

                    return;
                }

                this._session = MidGenerator.Gen();
                this._rumEvent.SetSession(this._session);
            }

            IDictionary<string, object> dict = new Dictionary<string, object>() {

                { "v", RUMConfig.VERSION },
                { "appv", this._appv },
                { "first", this._rumEvent.IsFirst() },
                { "sw", RUMPlatform.ScreenWidth },
                { "sh", RUMPlatform.ScreenHeight },
                { "manu", RUMPlatform.Manu },
                { "model", RUMPlatform.DeviceModel },
                { "os", RUMPlatform.OperatingSystem },
                { "osv", RUMPlatform.SystemVersion },
                { "nw", RUMPlatform.Network },
                { "carrier", RUMPlatform.Carrier },
                { "lang", RUMPlatform.SystemLanguage },
                { "from", RUMPlatform.From }
            };

            this.WriteEvent("open", dict);
        }

        private void OnSecond(long timestamp) {

            this._rumEvent.OnSecond(timestamp);
            
            this.CheckPingCount();
            this.TryConnect(timestamp);
        }

        private void CheckPingCount() {

            bool needClose = false;

            lock (ping_locker) {

                if (ping_locker.Status == 0) {

                    return;
                }

                if (this._pingCount >= 2) {

                    needClose = true;
                    this._pingCount = 0;
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

        private void OnSendQuest() {

            this.SendPing();
            this.SendEvent();
        }

        private TryConnLocker tryconn_locker = new TryConnLocker();

        private void TryConnect(long timestamp) {

            lock (tryconn_locker) {

                if (tryconn_locker.Status == 0) {

                    return;
                }

                if (timestamp - this._lastConnectTime < RUMConfig.CONNCT_INTERVAL) {

                    return;
                }

                tryconn_locker.Status = 0;
            }

            if (this._debug) {

                Debug.Log("[RUM] try connect...");
            }

            this.Connect(this._endpoint, false, false);
        }

        private PingLocker ping_locker = new PingLocker();

        private void StartPing() {

            lock (ping_locker) {

                if (ping_locker.Status != 0) {

                    return;
                }

                ping_locker.Status = 1;
                this._lastPingTime = FPManager.Instance.GetMilliTimestamp() - RUMConfig.PING_INTERVAL;
            }
        }

        private void StopPing() {

            lock (ping_locker) {

                ping_locker.Status = 0;
            }
        }

        private void SendPing() {

            lock (ping_locker) {

                if (ping_locker.Status == 0) {

                    return;
                }

                if (FPManager.Instance.GetMilliTimestamp() - this._lastPingTime < RUMConfig.PING_INTERVAL) {

                    return;
                }

                this._pingCount++;
                this._lastPingTime = FPManager.Instance.GetMilliTimestamp();
            }

            long lastEid = 0;
            int lastCount = 0;

            long salt = MidGenerator.Gen();

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
            }

            lock (ping_locker) {

                payload.Add("pt", this._pingLatency);

                lastEid = this._pingEid;
                lastCount = this._writeCount;

                this._writeCount = 0;
                this._pingEid = MidGenerator.Gen();

                payload.Add("wc", lastCount);
                payload.Add("feid", lastEid);
                payload.Add("teid", this._pingEid);
            }

            RUMClient self = this;
            long pingTime = FPManager.Instance.GetMilliTimestamp();

            this.SendQuest("ping", payload, (cbd) => {

                lock (ping_locker) {

                    self._pingLatency = Convert.ToInt32(FPManager.Instance.GetMilliTimestamp() - pingTime);
                }

                Exception ex = cbd.GetException();

                if (ex != null) {

                    ErrorRecorderHolder.recordError(ex);
                    return;
                }

                lock (ping_locker) {

                    self._pingCount--;
                }

                IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

                if (self._debug) {

                    Debug.Log("[RUM] recv ping: " + Json.SerializeToString(dict));
                }

                self._rumEvent.SetTimestamp(Convert.ToInt64(dict["ts"]));
                self._rumEvent.SetSizeLimit(Convert.ToInt32(dict["bw"]));

                int cv = Convert.ToInt32(dict["cv"]);

                bool needLoad = false;
                bool hasConfig = self._rumEvent.HasConfig();

                lock (self_locker) {

                    if (self._configVersion != cv || (cv == 0 && !hasConfig)) {

                        needLoad = true;
                        self._configVersion = cv;
                    }
                }

                if (needLoad) {

                    self.LoadConfig();
                }

            }, RUMConfig.PING_INTERVAL);
        }

        private void LoadConfig() {

            long salt = MidGenerator.Gen();

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

            RUMClient self = this;
            this.SendQuest("getconfig", payload, (cbd) => {

                Exception ex = cbd.GetException();

                if (ex != null) {

                    lock (self_locker) {

                        self._configVersion = 0;
                    }

                    ErrorRecorderHolder.recordError(ex);
                    return;
                }

                IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

                if (self._debug) {

                    Debug.Log("[RUM] recv config: " + Json.SerializeToString(dict));
                }

                self._rumEvent.UpdateConfig((IDictionary<string, object>)dict["events"]);
                self.GetEvent().FireEvent(new EventData("config"));
            }, RUMConfig.SENT_TIMEOUT);
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

        private void SendEvents(List<object> items) {

            long salt = MidGenerator.Gen();

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
                }

                int count = items.Count;
                self._rumEvent.RemoveFromCache(items);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    self._rumEvent.WriteEvents(items);
                    ErrorRecorderHolder.recordError(ex);
                    return;
                }

                if (self._debug) {

                    Debug.Log("[RUM] sent count: " + count);
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

                ErrorRecorderHolder.recordError(ex);
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
                    }catch(Exception ex) {

                        ErrorRecorderHolder.recordError(ex);
                    }
                }

                if (data.GetFlag() == 1) {

                    try {

                        using (MemoryStream inputStream = new MemoryStream(data.MsgpackPayload())) {

                            payload = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
                        }
                    } catch(Exception ex) {

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

        private class RUMErrorRecorder:ErrorRecorder {

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
}