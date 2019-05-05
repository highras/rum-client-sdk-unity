using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;
using UnityEngine.Networking;

namespace com.rum {

    public class RUMPlatform:MonoBehaviour {

        private static RUMPlatform instance = null;

        public static RUMPlatform Instance {

            get {

                if (instance == null) {    

                    instance = GameObject.FindObjectOfType<RUMPlatform>();

                    if (instance == null) {

                        GameObject go = new GameObject("RUMPlatform");
                        instance = go.AddComponent<RUMPlatform>();
                    }
                }

                return instance;
            }
        }

        private FPEvent _event = new FPEvent();
        public IDictionary<string, object> StoragePrefs;

        private bool _isPause;
        private bool _isFocus;

        private bool _isSaveing;

        void Awake() {    

            if (instance == null) {

                instance = this;
            } else {

                Destroy(gameObject);
            } 
        }

        void OnEnable() {

            this._isPause = false;
            this._isFocus = false;

            Application.logMessageReceived += OnLogCallback;
            Application.logMessageReceivedThreaded += OnLogCallbackThreaded;

            this._nw = "NONE";

            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {

                this._nw = "3G/4G";
            }

            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork) {

                this._nw = "WIFI";
            }

            this._lang = Application.systemLanguage.ToString();
            this._model = SystemInfo.deviceModel;
            this._os = SystemInfo.operatingSystem;
            this._sh = Screen.height;
            this._sw = Screen.width;

            Invoke("OnTimer", RUMConfig.LOCAL_STORAGE_DELAY / 1000);
        }

        void Start() {}

        void OnDisable() {

            this.SavePrefs();

            Application.logMessageReceived -= OnLogCallback;
            Application.logMessageReceivedThreaded -= OnLogCallbackThreaded;

            CancelInvoke();
        }

        void OnApplicationPause() {
 
            if (!this._isPause) {
             
                this._event.FireEvent(new EventData("app_bg"));
                
                this.SavePrefs();
            } else {

                this._isFocus = true;
            }

            this._isPause = true;
        }

        void OnApplicationFocus() {

            if (this._isFocus) {
             
                this._isPause = false;
                this._isFocus = false;
            }
             
            if (this._isPause) {

                this._event.FireEvent(new EventData("app_fg"));
                this._isFocus = true;
            }
        }

        private void OnTimer() {

            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {

                if (this._nw != "3G/4G") {

                    this._nw = "3G/4G";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork) {

                if (this._nw != "WIFI") {

                    this._nw = "WIFI";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            if (Application.internetReachability == NetworkReachability.NotReachable){

                if (this._nw != "NONE") {

                    this._nw = "NONE";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            this.SavePrefs();
            Invoke("OnTimer", RUMConfig.LOCAL_STORAGE_DELAY / 1000);
        }

        private void SavePrefs() {

            if (this._isSaveing) {

                return;
            }

            this._isSaveing = true;

            if (StoragePrefs != null) {

                lock(StoragePrefs) {

                    foreach (KeyValuePair<string, object> kvp in StoragePrefs) {

                        PlayerPrefs.SetString(kvp.Key, Json.SerializeToString(kvp.Value));
                    }
                }

                PlayerPrefs.Save();
            }

            this._isSaveing = false;
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {

            if (type == LogType.Assert) {

                this.WriteException("crash", "main_assert", logString, stackTrace);
                this.SavePrefs();
            }

            if (type == LogType.Exception) {

                this.WriteException("error", "main_exception", logString, stackTrace);
                this.SavePrefs();
            }
        }

        private void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {

            if (type == LogType.Exception) {

                this.WriteException("error", "threaded_exception", logString, stackTrace);
            }
        }

        public void WriteException(string ev, string type, string message, string stack) {

            if (this._writeEvent != null) {

                IDictionary<string, object> dict = new Dictionary<string, object>();

                dict.Add("ev", ev);
                dict.Add("type", type);
                dict.Add("message", message);
                dict.Add("stack", stack);

                this._writeEvent(ev, dict);
            }

            Debug.LogError("message: " + message + " stack: " + stack);
        }

        private Action<string, IDictionary<string, object>> _writeEvent;

        public string RumIdKey = "rum_rid_";
        public string RumEventKey = "rum_event_";
        public string FileIndexKey = "rum_index_";

        public void InitPrefs(int pid, Action<string, IDictionary<string, object>> writeEvent) {

            RumIdKey += pid;
            RumEventKey += pid;
            FileIndexKey += pid;

            this._writeEvent = writeEvent;

            StoragePrefs = new Dictionary<string, object>();
            IDictionary<string, object> rumid_value = new Dictionary<string, object>();

            if (PlayerPrefs.HasKey(RumIdKey)) {

                try {

                    rumid_value = Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(RumIdKey));
                } catch(Exception ex) {

                    this.WriteException("error", "main_exception", ex.Message, ex.StackTrace);
                }
            } 

            StoragePrefs.Add(RumIdKey, rumid_value);
            IDictionary<string, object> events_value = new Dictionary<string, object>();

            if (PlayerPrefs.HasKey(RumEventKey)) {

                try {

                    events_value = Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(RumEventKey));
                } catch(Exception ex) {

                    this.WriteException("error", "main_exception", ex.Message, ex.StackTrace);
                }
            } 

            StoragePrefs.Add(RumEventKey, events_value);
            IDictionary<string, object> fileindex_value = new Dictionary<string, object>();

            if (PlayerPrefs.HasKey(FileIndexKey)) {

                try {

                    fileindex_value = Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(FileIndexKey));
                } catch(Exception ex) {

                    this.WriteException("error", "main_exception", ex.Message, ex.StackTrace);
                }
            } 

            StoragePrefs.Add(FileIndexKey, fileindex_value);
            ErrorRecorderHolder.setInstance(new RUMErrorRecorder(writeEvent));
        }

        public FPEvent GetEvent() {

            return this._event;
        }

        private string _lang;

        public string GetLang() {

            return this._lang;
        }

        public string GetManu() {

            return null;
        }

        private string _model;

        public string GetModel() {

            return this._model;
        }

        private string _os;

        public string GetOS() {

            return this._os;
        }

        public string GetOSV() {

            return null;
        }

        private string _nw;

        public string GetNetwork() {

            return this._nw;
        }

        public bool IsMobile() {

            return true;
        }

        private int _sh;

        public int ScreenHeight() {

            return this._sh;
        }

        private int _sw;

        public int ScreenWidth() {

            return this._sw;
        }

        public string GetCarrier() {

            return null;
        }

        public string GetFrom() {

            return null;
        }

        public void AddSelfListener() {}

        // HttpWebRequest
        public void HookHttp(HttpWebRequest req, HttpWebResponse res, int latency) {

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

                if (req.Timeout != null) {

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
            }

            dict.Add("attrs", attrs);
            this._event.FireEvent(new EventData("http_hook", dict));
        }

        // UnityWebRequest
        public void HookHttp(UnityWebRequest req, int latency) {

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
            this._event.FireEvent(new EventData("http_hook", dict));
        }

        private class RUMErrorRecorder:ErrorRecorder {

            private Action<string, IDictionary<string, object>> _writeEvent;

            public RUMErrorRecorder(Action<string, IDictionary<string, object>> writeEvent):base() {

                this._writeEvent = writeEvent;
            }

            public override void recordError(Exception e) {
            
                if (this._writeEvent != null) {

                    IDictionary<string, object> dict = new Dictionary<string, object>();

                    dict.Add("type", "rum_threaded_exception");
                    dict.Add("message", e.Message);
                    dict.Add("stack", e.StackTrace);

                    this._writeEvent("error", dict);
                }

                Debug.LogError("message: " + e.Message + " stack: " + e.StackTrace);
            }
        }
    }
}