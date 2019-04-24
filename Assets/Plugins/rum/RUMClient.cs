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
            static private System.Object Lock = new System.Object();

            static public long Gen() {

                long c = 0;

                lock(Lock) {

                    if (++Count >= 999) {

                        Count = 0;
                    }

                    c = Count;

                    StringBuilder sb = new StringBuilder(20);

                    sb.Append(Convert.ToString(ThreadPool.Instance.GetMilliTimestamp()));

                    if (c < 100) {

                        sb.Append("0");
                    }

                    if (c < 10) {

                        sb.Append("0");
                    }

                    sb.Append(Convert.ToString(c));
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
        private RUMPlatform _platform;

        private long _pingEid = 0;
        private long _session = 0;
        private long _lastPingTime = 0;
        private long _lastSendTime = 0;

        private int _pingLatency = 0;
        private int _writeCount = 0;
        private int _configVersion = 0;

        public RUMClient(int pid, string token, string uid, string appv, bool debug) {

            this._pid = pid;
            this._token = token;
            this._uid = uid;
            this._appv = appv;
            this._debug = debug;
            
            this._platform = RUMPlatform.Instance;
            this._rumEvent = new RUMEvent(this._pid, this._platform, this._debug);
        }

        public void Destroy() {

            this._session = 0;
            this._pingEid = 0;
            this._writeCount = 0;   

            if (this._rumEvent != null) {

                this._rumEvent.Destroy();
            }

            this._event.RemoveListener();

            if (this._baseClient != null) {

                this._baseClient.Destroy();
                this._baseClient = null;
            }
        }

        public void Connect(string endpoint, bool clearStorage) {

            if (this._baseClient != null) {

                this.GetEvent().FireEvent(new EventData("error", new Exception("client has been init!")));
                return;
            }

            if (clearStorage) {

                this._rumEvent.ClearStorage();
            }

            if (this._debug) {

                Debug.Log("[RUM] init: " + endpoint);
            }

            this.OpenEvent();
            this.AddPlatformListener();

            RUMClient self = this;
            ThreadPool.Instance.StartTimerThread();

            this._baseClient = new FPClient(endpoint, true, RUMConfig.SENT_TIMEOUT);

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

                self.GetEvent().FireEvent(new EventData("close"));
            });

            this._baseClient.GetEvent().AddListener("error", (evd) => {

                if (self._debug) {

                    Debug.Log("[RUM] error: " + evd.GetException().Message);
                }

                self.GetEvent().FireEvent(new EventData("error", evd.GetException()));
            });

            this._baseClient.GetEvent().AddListener("second", (evd) => {

                self.OnSecond(evd.GetTimestamp());
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

        public void AppendEvent(string type, IDictionary<string, object> dict) {

            if (!string.IsNullOrEmpty(type)) {

                dict.Add("type", type);
                this.WriteEvent("append", dict);
            }
        }

        private void AddPlatformListener() {

            RUMClient self = this;

            this._platform.GetEvent().AddListener("app_bg", (evd) => {

                self.WriteEvent("bg", new Dictionary<string, object>());
            });

            this._platform.GetEvent().AddListener("app_fg", (evd) => {

                self.WriteEvent("fg", new Dictionary<string, object>());
            });

            this._platform.GetEvent().AddListener("network_change", (evd) => {

                IDictionary<string, object> dict = new Dictionary<string, object>();
                dict.Add("nw", self._platform.GetNetwork());

                self.WriteEvent("nw", dict);
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

            IDictionary<string, object> cp_dict = new Dictionary<string, object>(dict);

            if (this._debug) {

                Debug.Log("[RUM] write event: " + Json.SerializeToString(cp_dict));
            }

            this._rumEvent.WriteEvent(cp_dict);
        }

        private void OpenEvent() {

            if (this._session > 0) {

                return;
            }

            this._session = MidGenerator.Gen();

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("sw", this._platform.ScreenWidth());
            dict.Add("sh", this._platform.ScreenHeight());
            dict.Add("manu", this._platform.GetManu());
            dict.Add("model", this._platform.GetModel());
            dict.Add("os", this._platform.GetOS());
            dict.Add("osv", this._platform.GetOSV());
            dict.Add("nw", this._platform.GetNetwork());
            dict.Add("carrier", this._platform.GetCarrier());
            dict.Add("lang", this._platform.GetLang());
            dict.Add("from", this._platform.GetFrom());
            dict.Add("appv", this._appv);
            dict.Add("v", RUMConfig.VERSION);
            dict.Add("first", this._rumEvent.IsFirst());

            this.WriteEvent("open", dict);
        }

        private void OnSecond(long timestamp) {

            this._rumEvent.OnSecond(timestamp);
            this.SendPing(timestamp);
            this.SendEvent(timestamp);
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

        public void SendPing(long timestamp) {

            if (this._lastPingTime == 0) {

                return;
            }

            if (timestamp - this._lastPingTime < RUMConfig.PING_INTERVAL) {

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

            long salt = this.GenSalt();

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

            MemoryStream outputStream = new MemoryStream();

            MsgPack.Serialize(payload, outputStream);
            outputStream.Position = 0; 

            byte[] bytes = outputStream.ToArray();

            FPData data = new FPData();
            data.SetFlag(0x1);
            data.SetMtype(0x1);
            data.SetMethod("ping");
            data.SetPayload(bytes);

            long pingTime = timestamp;
            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                self._pingLatency = Convert.ToInt32(ThreadPool.Instance.GetMilliTimestamp() - pingTime);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    self.GetEvent().FireEvent(new EventData("error", ex));
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
                    this.LoadConfig();
                }

            }, RUMConfig.PING_INTERVAL);
        }

        private void LoadConfig() {

            if (this._debug) {

                Debug.Log("[RUM] load config...");
            }

            long salt = this.GenSalt();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("uid", this._uid);
            payload.Add("rid", this._rumEvent.GetRumId());

            payload.Add("lang", this._platform.GetLang());
            payload.Add("manu", this._platform.GetManu());
            payload.Add("model", this._platform.GetModel());
            payload.Add("os", this._platform.GetOS());
            payload.Add("osv", this._platform.GetOSV());
            payload.Add("nw", this._platform.GetNetwork());
            payload.Add("carrier", this._platform.GetCarrier());
            payload.Add("from", this._platform.GetFrom());
            payload.Add("appv", this._appv);

            MemoryStream outputStream = new MemoryStream();

            MsgPack.Serialize(payload, outputStream);
            outputStream.Position = 0; 

            byte[] bytes = outputStream.ToArray();

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
                    self.GetEvent().FireEvent(new EventData("error", ex));
                    return;
                }

                IDictionary<string, object> dict = (IDictionary<string, object>)cbd.GetPayload();

                if (self._debug) {

                    Debug.Log("[RUM] load config: " + Json.SerializeToString(dict));
                }

                self._rumEvent.UpdateConfig((IDictionary<string, object>)dict["events"]);
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

        private void SendEvent(long timestamp) {

            if (this._lastSendTime == 0) {

                return;
            }

            if (timestamp - this._lastSendTime < RUMConfig.SENT_INTERVAL) {

                return;
            }

            this._lastSendTime += RUMConfig.SENT_INTERVAL;

            List<object> items = this._rumEvent.GetSentEvents();

            if (items.Count == 0) {

                return;
            }

            if (this._debug) {

                Debug.Log("[RUM] will be sent! " + Json.SerializeToString(items));
            }

            this.SendEvents(items);
        }

        private void SendEvents(List<object> items) {

            long salt = this.GenSalt();

            IDictionary<string, object> payload = new Dictionary<string, object>();

            payload.Add("pid", this._pid);
            payload.Add("sign", this.GenSign(salt));
            payload.Add("salt", salt);
            payload.Add("events", items);

            MemoryStream outputStream = new MemoryStream();

            MsgPack.Serialize(payload, outputStream);
            outputStream.Position = 0; 

            byte[] bytes = outputStream.ToArray();

            FPData data = new FPData();
            data.SetFlag(0x1);
            data.SetMtype(0x1);
            data.SetMethod("adds");
            data.SetPayload(bytes);

            RUMClient self = this;

            this.SendQuest(data, (cbd) => {

                self._rumEvent.RemoveFromCache(items);

                Exception ex = cbd.GetException();

                if (ex != null) {

                    self.GetEvent().FireEvent(new EventData("error", ex));
                    self._rumEvent.WriteEvents(items);
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

        private long GenSalt() {

            return ThreadPool.Instance.GetMilliTimestamp();
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

                    payload = Json.Deserialize<IDictionary<string, object>>(data.JsonPayload());
                }

                if (data.GetFlag() == 1) {

                    MemoryStream inputStream = new MemoryStream(data.MsgpackPayload());
                    payload = MsgPack.Deserialize<IDictionary<string, object>>(inputStream);
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