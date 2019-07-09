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
            static private System.Object Lock = new System.Object();

            static public long Gen() {

                lock(Lock) {

                    long c = 0;

                    if (++Count >= 999) {

                        Count = 0;
                    }

                    c = Count;

                    sb.Clear();
                    sb.Append(ThreadPool.Instance.GetMilliTimestamp());

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

        private FPEvent _event = new FPEvent();

        public FPEvent GetEvent() {

            return this._event;
        }

        private string _token;
        private string _uid;
        private string _appv;

        private int _pid = 0;
        private bool _debug = true;

        private RUMEvent _rumEvent;
        private FPClient _baseClient;

        private long _pingEid = 0;
        private long _session = 0;
        private long _lastPingTime = 0;
        private long _lastSendTime = 0;
        private long _lastConnectTime = 0;

        private int _sendCount = 0;
        private int _pingLatency = 0;
        private int _writeCount = 0;
        private int _configVersion = 0;

        private EventDelegate _eventDelegate;

        public RUMClient(int pid, string token, string uid, string appv, bool debug) {

            Debug.Log("Hello RUM!   rum@" + RUMConfig.VERSION + ", fpnn@" + FPConfig.VERSION);

            this._pid = pid;
            this._token = token;
            this._uid = uid;
            this._appv = appv;
            this._debug = debug;
            this._rumEvent = new RUMEvent(this._pid, this._debug, this.OnSendQuest);

            RUMClient self = this;

            this._eventDelegate = (evd) => {

                self.OnSecond(evd.GetTimestamp());
            };

            ThreadPool.Instance.Event.AddListener("second", this._eventDelegate);

            RUMFile.Instance.Init(this._pid);
            RUMPlatform.Instance.InitPrefs(WriteEvent);

            this._rumEvent.InitStorage();
            this.AddPlatformListener();
        }

        public void Destroy() {

            if (this._eventDelegate != null) {

                ThreadPool.Instance.Event.RemoveListener("second", this._eventDelegate);
                this._eventDelegate = null;
            }

            this._session = 0;
            this._pingEid = 0;
            this._writeCount = 0;

            this._pid = 0;
            this._token = null;
            this._uid = null;
            this._appv = null;

            this._lastPingTime = 0;
            this._lastSendTime = 0;
            this._lastConnectTime = 0;

            this._sendCount = 0;
            this._pingLatency = 0;
            this._configVersion = 0;

            if (this._baseClient != null) {

                this._baseClient.Destroy();
                this._baseClient = null;
            }

            if (this._rumEvent != null) {

                this._rumEvent.Destroy();
            }

            this._event.RemoveListener();
            RUMPlatform.Instance.GetEvent().RemoveListener();
        }

        public void Connect(string endpoint, bool clearRumId, bool clearEvents) {

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

            if (this._debug) {

                Debug.Log("[RUM] init: " + endpoint + " version: " + RUMConfig.VERSION);
            }

            this.OpenEvent();

            RUMClient self = this;
            ThreadPool.Instance.StartTimerThread();

            this._baseClient = new FPClient(endpoint, false, RUMConfig.CONNCT_INTERVAL);

            this._baseClient.GetEvent().AddListener("connect", (evd) => {

                if (self._debug) {

                    Debug.Log("[RUM] connect on rum agent!");
                }

                self.GetEvent().FireEvent(new EventData("ready"));

                self.StartPing();
                self.StartSend();
            });

            this._baseClient.GetEvent().AddListener("close", (evd) => {

                if (self._debug) {

                    Debug.Log("[RUM] close from rum agent!");
                }

                self.StopPing();
                self.StopSend();

                self._sendCount = 0;
                self._configVersion = 0;
                self._lastConnectTime = ThreadPool.Instance.GetMilliTimestamp();
                self.GetEvent().FireEvent(new EventData("close"));
            });

            this._baseClient.GetEvent().AddListener("error", (evd) => {

                RUMPlatform.Instance.WriteDebug("base_client", evd.GetException());
            });

            this._baseClient.Connect();
        }

        public long GetSession() {

            return this._session;
        }

        public string GetRumId() {

            return this._rumEvent.GetRumId();
        }

        public void SetUid(string value) {

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

        public void CustomEvent(string ev, IDictionary<string, object> attrs) {

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("attrs", attrs);
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
            dict.Add("attrs", attrs);

            this.WriteEvent("http", dict);

            if (status <= 0 || status >= 300) {

                IDictionary<string, object> err_dict = new Dictionary<string, object>();

                err_dict.Add("url", url);
                err_dict.Add("method", method);
                err_dict.Add("status", status);
                err_dict.Add("reqsize", reqsize);
                err_dict.Add("respsize", respsize);
                err_dict.Add("latency", latency);
                err_dict.Add("attrs", attrs);

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
                lat_dict.Add("attrs", attrs);

                this.WriteEvent("httplat", lat_dict);
                return;
            }
        }

        private void HttpEvent(IDictionary<string, object> dict) {

            string url = null;

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

            RUMPlatform.Instance.GetEvent().AddListener("app_bg", (evd) => {

                self.WriteEvent("bg", new Dictionary<string, object>());
            });

            RUMPlatform.Instance.GetEvent().AddListener("app_fg", (evd) => {

                self.WriteEvent("fg", new Dictionary<string, object>());
            });

            RUMPlatform.Instance.GetEvent().AddListener("system_info", (evd) => {

                IDictionary<string, object> dict = (IDictionary<string, object>)evd.GetPayload();
                dict.Add("type", "system_info");

                self.WriteEvent("info", dict);
            });

            RUMPlatform.Instance.GetEvent().AddListener("network_change", (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("nw", RUMPlatform.Instance.GetNetwork());

                self.WriteEvent("nwswitch", dict);
            });

            RUMPlatform.Instance.GetEvent().AddListener("memory_warning", (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>();
                
                dict.Add("type", "low_memory");
                dict.Add("system_memory", RUMPlatform.Instance.GetMemorySize());

                self.WriteEvent("warn", dict);
            });

            RUMPlatform.Instance.GetEvent().AddListener("http_hook", (evd) => {

                if (evd.GetPayload() != null) {

                    this.HttpEvent((IDictionary<string, object>)evd.GetPayload());
                }
            });
        }

        private void WriteEvent(string ev, IDictionary<string, object> dict) {

            dict.Add("ev", ev);

            if (!dict.ContainsKey("eid")) {

                dict.Add("eid", MidGenerator.Gen());
                this._writeCount++;
            }

            if (!dict.ContainsKey("pid")) {

                dict.Add("pid", this._pid);
            }

            if (!dict.ContainsKey("sid")) {

                dict.Add("sid", this._session);
            }

            if (!dict.ContainsKey("uid")) {

                dict.Add("uid", this._uid);
            }

            if (!dict.ContainsKey("rid")) {

                dict.Add("rid", this._rumEvent.GetRumId());
            }

            if (!dict.ContainsKey("ts")) {

                dict.Add("ts", this._rumEvent.GetTimestamp());
            }

            if (this._debug) {

                Debug.Log("[RUM] write event: " + Json.SerializeToString(dict));
            }

            this._rumEvent.WriteEvent(dict);
        }

        private void OpenEvent() {

            if (this._session > 0) {

                return;
            }

            this._session = MidGenerator.Gen();

            IDictionary<string, object> dict = new Dictionary<string, object>();

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
            dict.Add("appv", this._appv);
            dict.Add("v", RUMConfig.VERSION);
            dict.Add("first", this._rumEvent.IsFirst());

            this.WriteEvent("open", dict);
        }

        private void OnSecond(long timestamp) {

            int AvailableWorkerThreads, aiot;
            System.Threading.ThreadPool.GetAvailableThreads(out AvailableWorkerThreads, out aiot);

            if (this._debug) {

                Debug.Log("[ThreadPool] available worker threads: " + AvailableWorkerThreads);
            } else if (AvailableWorkerThreads <= 1) {

                RUMPlatform.Instance.WriteDebug("low_available_worker_threads", new Exception("available count: " + AvailableWorkerThreads));
            }

            this._rumEvent.OnSecond(timestamp);
            this.TryConnect(timestamp);
        }

        private void OnSendQuest() {

            this.SendPing();
            this.SendEvent();
        }

        private void TryConnect(long timestamp) {

            if (this._lastConnectTime == 0) {

                return;
            }

            if (timestamp - this._lastConnectTime < RUMConfig.CONNCT_INTERVAL) {

                return;
            }

            if (this._debug) {

                Debug.Log("[RUM] try connect...");
            }

            this._lastConnectTime = 0;

            if (this._baseClient != null) {

                this._baseClient.Connect();
            }
        }

        private void StartPing() {

            if (this._lastPingTime != 0 ) {

                return;
            }

            this._lastPingTime = ThreadPool.Instance.GetMilliTimestamp() - RUMConfig.PING_INTERVAL;
        }

        private void StopPing() {

            this._lastPingTime = 0;
        }

        private void SendPing() {

            if (this._lastPingTime == 0) {

                return;
            }

            if (ThreadPool.Instance.GetMilliTimestamp() - this._lastPingTime < RUMConfig.PING_INTERVAL) {

                return;
            }

            this._lastPingTime += RUMConfig.PING_INTERVAL;

            if (this._debug) {

                Debug.Log("[RUM] ping...");
            }

            long lastEid = this._pingEid;
            int lastCount = this._writeCount;

            this._writeCount = 0;
            this._pingEid = MidGenerator.Gen();

            long salt = MidGenerator.Gen();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("uid", this._uid);
            payload.Add("rid", this._rumEvent.GetRumId());
            payload.Add("sid", this._session);
            payload.Add("cv", this._configVersion);
            payload.Add("pt", this._pingLatency);
            payload.Add("ss", this._rumEvent.GetStorageSize());
            payload.Add("wc", lastCount);
            payload.Add("feid", lastEid);
            payload.Add("teid", this._pingEid);

            byte[] bytes = new byte[0];

            try {

                using (MemoryStream outputStream = new MemoryStream()) {

                    MsgPack.Serialize(payload, outputStream);
                    outputStream.Seek(0, SeekOrigin.Begin);

                    bytes = outputStream.ToArray();
                }
            } catch (Exception ex) {

                RUMPlatform.Instance.WriteDebug("send_ping_serialize_payload", ex);
            }

            FPData data = new FPData();
            data.SetFlag(0x1);
            data.SetMtype(0x1);
            data.SetMethod("ping");
            data.SetPayload(bytes);

            long pingTime = ThreadPool.Instance.GetMilliTimestamp();
            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                self._pingLatency = Convert.ToInt32(ThreadPool.Instance.GetMilliTimestamp() - pingTime);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    RUMPlatform.Instance.WriteDebug("send_ping_send_quest", ex);
                    return;
                }

                IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

                if (self._debug) {

                    Debug.Log("[RUM] ping: " + Json.SerializeToString(dict));
                }

                self._rumEvent.SetTimestamp(Convert.ToInt64(dict["ts"]));
                self._rumEvent.SetSizeLimit(Convert.ToInt32(dict["bw"]));

                int cv = Convert.ToInt32(dict["cv"]);

                if (self._configVersion != cv || (cv == 0 && !self._rumEvent.HasConfig())) {

                    self._configVersion = cv;
                    self.LoadConfig();
                }

            }, RUMConfig.PING_INTERVAL);
        }

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
            payload.Add("rid", this._rumEvent.GetRumId());

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

                RUMPlatform.Instance.WriteDebug("load_config_serialize_payload", ex);
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

                    self._configVersion = 0;

                    RUMPlatform.Instance.WriteDebug("load_config_send_quest", ex);
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

        private void StartSend() {

            if (this._lastSendTime != 0 ) {

                return;
            }

            this._lastSendTime = ThreadPool.Instance.GetMilliTimestamp();
        }

        private void StopSend() {

            this._lastSendTime = 0;
        }

        private void SendEvent() {

            if (this._lastSendTime == 0) {

                return;
            }

            if (ThreadPool.Instance.GetMilliTimestamp() - this._lastSendTime < RUMConfig.SENT_INTERVAL) {

                return;
            }

            this._lastSendTime += RUMConfig.SENT_INTERVAL;

            if (this._sendCount >= 3) {

                return;
            }

            List<object> items = this._rumEvent.GetSentEvents();

            if (items.Count == 0) {

                return;
            }

            this._sendCount++;

            if (this._debug) {

                Debug.Log("[RUM] will be sent! " + Json.SerializeToString(items));
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

                RUMPlatform.Instance.WriteDebug("send_events_serialize_payload", ex);
            }

            FPData data = new FPData();
            data.SetFlag(0x1);
            data.SetMtype(0x1);
            data.SetMethod("adds");
            data.SetPayload(bytes);

            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                if (self._sendCount > 0) {

                    self._sendCount--;
                } 

                self._rumEvent.RemoveFromCache(items);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    self._rumEvent.WriteEvents(items);
                    RUMPlatform.Instance.WriteDebug("send_events_send_quest", ex);
                    return;
                }
            }, RUMConfig.SENT_TIMEOUT);
        }

        private string GenSign(long salt) {

            StringBuilder sb = new StringBuilder(70);

            sb.Append(Convert.ToString(this._pid));
            sb.Append(":");
            sb.Append(this._token);
            sb.Append(":");
            sb.Append(Convert.ToString(salt));

            return this.CalcMd5(sb.ToString(), true);
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

                        RUMPlatform.Instance.WriteDebug("check_fpcallback_deserialize_json_payload", ex);
                    }
                }

                if (data.GetFlag() == 1) {

                    try {

                        using (MemoryStream inputStream = new MemoryStream(data.MsgpackPayload())) {

                            payload = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
                        }
                    } catch(Exception ex) {

                        RUMPlatform.Instance.WriteDebug("check_fpcallback_deserialize_msgpack_payload", ex);
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