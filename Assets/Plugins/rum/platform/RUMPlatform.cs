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
                        
                        DontDestroyOnLoad(go);
                        instance = go.AddComponent<RUMPlatform>();
                    }
                }

                return instance;
            }
        }

        private FPEvent _event = new FPEvent();

        private bool _isPause;
        private bool _isFocus;

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

            Application.lowMemory += OnLowMemory;
            Application.logMessageReceived += OnLogCallback;
            Application.logMessageReceivedThreaded += OnLogCallbackThreaded;

            this._nw = "NONE";
            NetworkReachability internetReachability = Application.internetReachability;

            if (internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {

                this._nw = "3G/4G";
            }

            if (internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork) {

                this._nw = "WIFI";
            }

            this._lang = Application.systemLanguage.ToString();
            this._model = SystemInfo.deviceModel;
            this._os = SystemInfo.operatingSystem;
            this._sh = Screen.height;
            this._sw = Screen.width;
            this._isMobile = Application.isMobilePlatform;
            this._memorySize = SystemInfo.systemMemorySize;
            this._unityVersion = Application.unityVersion;
            this._installMode = Application.installMode;
            
            this._deviceToken = null;
            #if UNITY_IPHONE
            this._deviceToken = UnityEngine.iOS.NotificationServices.deviceToken;
            #endif

            Invoke("OnTimer", 5f);
            Invoke("OnInfo", 20f);
        }

        void Start() {}

        void OnDisable() {

            // this.SavePrefs();

            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceived -= OnLogCallback;
            Application.logMessageReceivedThreaded -= OnLogCallbackThreaded;

            CancelInvoke();
        }

        void OnApplicationPause() {
 
            if (!this._isPause) {
             
                this._event.FireEvent(new EventData("app_bg"));
                
                // this.SavePrefs();
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

        void OnLowMemory() {

            this._event.FireEvent(new EventData("memory_warning"));
        }

        private void OnInfo() {

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("system_memory", this.GetMemorySize());
            dict.Add("unity_version", this.GetUnityVersion());
            dict.Add("install_mode", this.GetInstallMode());
            dict.Add("device_token", this.GetDeviceToken());

            this._event.FireEvent(new EventData("system_info", dict));
        }

        private void OnTimer() {

            NetworkReachability internetReachability = Application.internetReachability;

            if (internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {

                if (this._nw != "3G/4G") {

                    this._nw = "3G/4G";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            if (internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork) {

                if (this._nw != "WIFI") {

                    this._nw = "WIFI";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            if (internetReachability == NetworkReachability.NotReachable){

                if (this._nw != "NONE") {

                    this._nw = "NONE";
                    this._event.FireEvent(new EventData("network_change"));
                }
            }

            Invoke("OnTimer", 5f);
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {

            if (type == LogType.Assert) {

                this.WriteException("crash", "main_assert", logString, stackTrace);
                // this.SavePrefs();
            }

            if (type == LogType.Exception) {

                this.WriteException("error", "main_exception", logString, stackTrace);
                // this.SavePrefs();
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

                dict.Add("type", type);

                if (!string.IsNullOrEmpty(message)) {

                    dict.Add("message", message);
                }

                if (!string.IsNullOrEmpty(stack)) {

                    dict.Add("stack", stack);
                }

                this._writeEvent(ev, dict);
            }

            Debug.LogError("message: " + message + ", " + stack);
        }

        private Action<string, IDictionary<string, object>> _writeEvent;

        public void InitPrefs(Action<string, IDictionary<string, object>> writeEvent) {

            this._writeEvent = writeEvent;
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

        private bool _isMobile;

        public bool IsMobile() {

            return this._isMobile;
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

        private int _memorySize;

        public int GetMemorySize() {

            return this._memorySize;
        }

        private string _unityVersion;

        public string GetUnityVersion(){

            return this._unityVersion;
        }

        private ApplicationInstallMode _installMode;

        public string GetInstallMode() {

            if (this._installMode == ApplicationInstallMode.Store) {

                return "Store";
            }

            if (this._installMode == ApplicationInstallMode.DeveloperBuild) {

                return "DeveloperBuild";
            }

            if (this._installMode == ApplicationInstallMode.Adhoc) {

                return "Adhoc";
            }

            if (this._installMode == ApplicationInstallMode.Enterprise) {

                return "Enterprise";
            }

            if (this._installMode == ApplicationInstallMode.Editor) {

                return "Editor";
            }

            return "Unknown";
        }

        private byte[] _deviceToken;

        public byte[] GetDeviceToken() {

            return this._deviceToken; 
        }

        public void AddSelfListener() {

        }

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

                    if (!string.IsNullOrEmpty(e.Message)) {

                        dict.Add("message", e.Message);
                    }

                    if (!string.IsNullOrEmpty(e.StackTrace)) {

                        dict.Add("stack", e.StackTrace);
                    }

                    this._writeEvent("error", dict);
                }

                Debug.LogError("message: " + e.Message + ", " + e.StackTrace);
            }
        }
    }
}