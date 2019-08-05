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

        private static class MidGenerator {

            static private long Count = 0;
            static private StringBuilder sb = new StringBuilder(20);
            static object lock_obj = new object();

            static public long Gen() {

                lock (lock_obj) {

                    long c = 0;

                    if (++Count >= 999) {

                        Count = 0;
                    }

                    c = Count;

                    sb.Clear();
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

        private class ConfigLocker {

            public int Status = 0;
        }

        private class SendLocker {

            public int Status = 0;
        }

        private class TryConnLocker {

            public int Status = 0;
        }

        private FPEvent _event = new FPEvent();

        public FPEvent GetEvent() {

            return this._event;
        }

        private string _token;
        private string _uid;
        private string _appv;

        private int _pid = 0;
        private bool _debug = true;
        private string _endpoint;

        private RUMEvent _rumEvent;
        private FPClient _baseClient;

        private long _pingEid = 0;
        private long _session = 0;
        private long _lastPingTime = 0;
        private long _lastConnectTime = 0;

        private String _rumId;

        private int _pingCount = 0;
        private int _sendCount = 0;
        private int _pingLatency = 0;
        private int _writeCount = 0;
        private int _configVersion = 0;

        private EventDelegate _eventDelegate;

        public RUMClient(int pid, string token, string uid, string appv, bool debug) {

            Debug.Log("Hello RUM! rum@" + RUMConfig.VERSION + ", fpnn@" + FPConfig.VERSION);

            this._pid = pid;
            this._token = token;
            this._uid = uid;
            this._appv = appv;
            this._debug = debug;
            this._rumEvent = new RUMEvent(this._pid, this._debug, this.OnSendQuest, this.OpenEvent);

            RUMClient self = this;

            this._eventDelegate = (evd) => {

                self.OnSecond(evd.GetTimestamp());
            };

            FPManager.Instance.AddSecond(this._eventDelegate);
            RUMPlatform.Instance.InitPrefs(WriteEvent);

            this._rumEvent.Init();
            this.AddPlatformListener();
        }

        private object self_locker = new object();

        public void Destroy() {

            lock (self_locker) {

                if (this._eventDelegate != null) {

                    FPManager.Instance.RemoveSecond(this._eventDelegate);
                    this._eventDelegate = null;
                }

                this._session = 0;

                this._pid = 0;
                this._token = null;
                this._uid = null;
                this._appv = null;

                lock (ping_locker) {

                    ping_locker.Status = 0;

                    this._pingEid = 0;
                    this._writeCount = 0;

                    this._pingCount = 0;
                    this._lastPingTime = 0;

                    this._pingLatency = 0;
                }

                lock (tryconn_locker) {

                    tryconn_locker.Status = 0;
                    this._lastConnectTime = 0;
                }

                lock (send_locker) {

                    this._sendCount = 0;
                }

                lock (config_locker) {

                    this._configVersion = 0;
                }

                if (this._baseClient != null) {

                    this._baseClient.Destroy();
                    this._baseClient = null;
                }

                if (this._rumEvent != null) {

                    this._rumEvent.Destroy();
                }

                this._event.RemoveListener();
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

                    lock (self_locker) {

                        if (self._baseClient != null) {

                            self._baseClient.Destroy();
                            self._baseClient = null;
                        }
                    }

                    lock (send_locker) {

                        self._sendCount = 0;
                    }

                    lock (config_locker) {

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

            return this._session;
        }

        public string GetRumId() {

            return this._rumId;
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

                if (!string.IsNullOrEmpty(this._uid)) {

                    IDictionary<string, object> dict = new Dictionary<string, object>();

                    dict.Add("uid", this._uid);

                    this.AppendEvent("uid", dict);
                }
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

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("url", url);
            dict.Add("method", method);
            dict.Add("status", status);
            dict.Add("reqsize", reqsize);
            dict.Add("respsize", respsize);
            dict.Add("latency", latency);

            if (attrs != null) {

                dict.Add("attrs", new Dictionary<string, object>(attrs));
            }

            this.WriteEvent("http", dict);

            if (status <= 0 || status >= 300) {

                IDictionary<string, object> err_dict = new Dictionary<string, object>();

                err_dict.Add("url", url);
                err_dict.Add("method", method);
                err_dict.Add("status", status);
                err_dict.Add("reqsize", reqsize);
                err_dict.Add("respsize", respsize);
                err_dict.Add("latency", latency);

                if (attrs != null) {

                    err_dict.Add("attrs", new Dictionary<string, object>(attrs));
                }

                this.WriteEvent("httperr", err_dict);
                return;
            }

            if (latency > 1000) {

                IDictionary<string, object> lat_dict = new Dictionary<string, object>();

                lat_dict.Add("url", url);
                lat_dict.Add("method", method);
                lat_dict.Add("status", status);
                lat_dict.Add("reqsize", reqsize);
                lat_dict.Add("respsize", respsize);
                lat_dict.Add("latency", latency);

                if (attrs != null) {

                    lat_dict.Add("attrs", new Dictionary<string, object>(attrs));
                }

                this.WriteEvent("httplat", lat_dict);
                return;
            }
        }

        // HttpWebRequest
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

        // UnityWebRequest
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

        private void AddPlatformListener() {

            RUMClient self = this;

            if (RUMPlatform.HasInstance()) {

                RUMPlatform.Instance.AppFg_Action = () => {

                    self.WriteEvent("fg", new Dictionary<string, object>());
                };

                RUMPlatform.Instance.AppBg_Action = () => {

                    self.WriteEvent("bg", new Dictionary<string, object>());
                };

                RUMPlatform.Instance.SystemInfo_Action = (dict) => {

                    dict.Add("type", "system_info");
                    self.WriteEvent("info", dict);
                };

                RUMPlatform.Instance.NetworkChange_Action = (nw) => {

                    IDictionary<string, object> dict = new Dictionary<string, object>();

                    dict.Add("nw", nw);
                    self.WriteEvent("nwswitch", dict);
                };

                RUMPlatform.Instance.LowMemory_Action = (mem) => {

                    IDictionary<string, object> dict = new Dictionary<string, object>();
                    
                    dict.Add("type", "low_memory");
                    dict.Add("system_memory", mem);

                    self.WriteEvent("warn", dict);
                };
            }
        }

        private void WriteEvent(string ev, IDictionary<string, object> dict) {

            dict.Add("ev", ev);

            if (!dict.ContainsKey("eid")) {

                dict.Add("eid", MidGenerator.Gen());

                lock (ping_locker) {

                    this._writeCount++;
                }
            }

            if (!dict.ContainsKey("pid")) {

                dict.Add("pid", this._pid);
            }

            if (!dict.ContainsKey("sid")) {

                if (this._session > 0) {

                    dict.Add("sid", this._session);
                }
            }

            if (!dict.ContainsKey("uid")) {

                dict.Add("uid", this._uid);
            }

            if (!dict.ContainsKey("rid")) {

                if (this._rumId != null) {

                    dict.Add("rid", this._rumId);
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

            this._rumEvent.WriteEvent(dict);
        }

        private void OpenEvent() {

            if (this._session > 0) {

                return;
            }

            this._session = MidGenerator.Gen();

            IDictionary<string, object> dict = new Dictionary<string, object>();

            if (RUMPlatform.HasInstance()) {

                dict.Add("sw", RUMPlatform.Instance.ScreenWidth());
                dict.Add("sh", RUMPlatform.Instance.ScreenHeight());
                dict.Add("manu", RUMPlatform.Instance.GetManu());
                dict.Add("model", RUMPlatform.Instance.GetModel());
                dict.Add("os", RUMPlatform.Instance.GetOS());
                dict.Add("osv", RUMPlatform.Instance.GetOSV());
                dict.Add("nw", RUMPlatform.Instance.GetNetwork());
                dict.Add("carrier", RUMPlatform.Instance.GetCarrier());
                dict.Add("lang", RUMPlatform.Instance.GetLang());
                dict.Add("from", RUMPlatform.Instance.GetFrom());
            }
            
            dict.Add("appv", this._appv);
            dict.Add("v", RUMConfig.VERSION);
            dict.Add("first", this._rumEvent.IsFirst());

            this._rumId = this._rumEvent.GetRumId();
            this._rumEvent.SetSession(this._session);

            this.WriteEvent("open", dict);
        }

        private void OnSecond(long timestamp) {

            this._rumEvent.OnSecond(timestamp);
            
            this.CheckPingCount();
            this.TryConnect(timestamp);
        }

        private void CheckPingCount() {

            lock (ping_locker) {

                if (ping_locker.Status == 0) {

                    return;
                }

                if (this._pingCount >= 2) {

                    this._pingCount = 0;

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

            if (this._debug) {

                Debug.Log("[RUM] ping...");
            }

            long lastEid = 0;
            int lastCount = 0;

            long salt = MidGenerator.Gen();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("uid", this._uid);
            payload.Add("rid", this._rumId);
            payload.Add("sid", this._session);
            // payload.Add("ss", this._rumEvent.GetStorageSize());
            payload.Add("ss", 1);

            lock (config_locker) {

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
            data.SetMethod("ping");
            data.SetPayload(bytes);

            long pingTime = FPManager.Instance.GetMilliTimestamp();
            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

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

                    Debug.Log("[RUM] ping: " + Json.SerializeToString(dict));
                }

                bool hasConfig = self._rumEvent.HasConfig();
                self._rumEvent.SetTimestamp(Convert.ToInt64(dict["ts"]));
                self._rumEvent.SetSizeLimit(Convert.ToInt32(dict["bw"]));

                int cv = Convert.ToInt32(dict["cv"]);

                bool needLoad = false;

                lock (config_locker) {

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

        private ConfigLocker config_locker = new ConfigLocker();

        private void LoadConfig() {

            if (this._debug) {

                Debug.Log("[RUM] load config...");
            }

            long salt = MidGenerator.Gen();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("uid", this._uid);
            payload.Add("rid", this._rumId);

            payload.Add("lang", RUMPlatform.Instance.GetLang());
            payload.Add("manu", RUMPlatform.Instance.GetManu());
            payload.Add("model", RUMPlatform.Instance.GetModel());
            payload.Add("os", RUMPlatform.Instance.GetOS());
            payload.Add("osv", RUMPlatform.Instance.GetOSV());
            payload.Add("nw", RUMPlatform.Instance.GetNetwork());
            payload.Add("carrier", RUMPlatform.Instance.GetCarrier());
            payload.Add("from", RUMPlatform.Instance.GetFrom());
            payload.Add("appv", this._appv);

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
            data.SetMethod("getconfig");
            data.SetPayload(bytes);

            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                Exception ex = cbd.GetException();

                if (ex != null) {

                    lock (config_locker) {

                        self._configVersion = 0;
                    }

                    ErrorRecorderHolder.recordError(ex);
                    return;
                }

                IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

                if (self._debug) {

                    Debug.Log("[RUM] load config: " + Json.SerializeToString(dict));
                }

                self._rumEvent.UpdateConfig((IDictionary<string, object>)dict["events"]);
                self.GetEvent().FireEvent(new EventData("config"));
            }, RUMConfig.SENT_TIMEOUT);
        }

        private SendLocker send_locker = new SendLocker();

        private void SendEvent() {

            lock (ping_locker) {

                if (ping_locker.Status == 0) {

                    return;
                }
            }

            lock (send_locker) {

                if (this._sendCount >= 3) {

                    return;
                }
            }

            List<object> items = this._rumEvent.GetSentEvents();

            if (items.Count == 0) {

                return;
            }

            lock (send_locker) {

                this._sendCount++;
            }

            if (this._debug) {

                Debug.Log("[RUM] will be sent! " + items.Count);
            }

            this.SendEvents(items);
        }

        private void SendEvents(List<object> items) {

            long salt = MidGenerator.Gen();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("events", items);

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
            data.SetMethod("adds");
            data.SetPayload(bytes);

            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                lock (send_locker) {

                    if (self._sendCount > 0) {

                        self._sendCount--;
                    }
                }

                self._rumEvent.RemoveFromCache(items);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    self._rumEvent.WriteEvents(items);
                    ErrorRecorderHolder.recordError(ex);
                    return;
                }
            }, RUMConfig.SENT_TIMEOUT);
        }

        private string GenSign(long salt) {

            lock (self_locker) {

                StringBuilder sb = new StringBuilder(70);

                sb.Append(Convert.ToString(this._pid));
                sb.Append(":");
                sb.Append(this._token);
                sb.Append(":");
                sb.Append(Convert.ToString(salt));

                return this.CalcMd5(sb.ToString(), true);
            }
        }

        private void SendQuest(FPData data, CallbackDelegate callback, int timeout) {

            if (this._baseClient != null) {

                this._baseClient.SendQuest(data, this.QuestCallback(callback), timeout);
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

                if (this._baseClient.GetPackage().IsAnswer(data)) {

                    isAnswerException = data.GetSS() != 0;
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
    }
}