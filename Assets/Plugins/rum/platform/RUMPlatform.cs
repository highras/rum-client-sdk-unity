using System;
using System.IO;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;

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

            if (StoragePrefs != null) {

                lock(StoragePrefs) {

                    foreach (KeyValuePair<string, object> kvp in StoragePrefs) {

                        PlayerPrefs.SetString(kvp.Key, Json.SerializeToString(kvp.Value));
                    }
                }

                PlayerPrefs.Save();
            }
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {

            IDictionary<string, object> dict = null;

            if (type == LogType.Assert) {

                if (this._writeEvent != null) {

                    dict = new Dictionary<string, object>();
                    dict.Add("type", "main_assert");
                    dict.Add("message", logString);
                    dict.Add("stack", stackTrace);

                    this._writeEvent("crash", dict);
                }
            }

            if (type == LogType.Exception) {

                if (this._writeEvent != null) {

                    dict = new Dictionary<string, object>();
                    dict.Add("type", "main_exception");
                    dict.Add("message", logString);
                    dict.Add("stack", stackTrace);

                    this._writeEvent("error", dict);
                }
            }

            if (dict != null) {

                this.SavePrefs();
                this._event.FireEvent(new EventData(Convert.ToString(dict["ev"]), dict));
            }
        }

        private void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {

            IDictionary<string, object> dict = null;

            if (type == LogType.Exception) {

                if (this._writeEvent != null) {

                    dict = new Dictionary<string, object>();
                    dict.Add("type", "threaded_exception");
                    dict.Add("message", logString);
                    dict.Add("stack", stackTrace);

                    this._writeEvent("error", dict);
                }
            }

            if (dict != null) {

                this._event.FireEvent(new EventData(Convert.ToString(dict["ev"]), dict));
            }
        }

        private Action<string, IDictionary<string, object>> _writeEvent;

        public void InitPrefs(string rumid_key, string events_key, Action<string, IDictionary<string, object>> writeEvent) {

            this._writeEvent = writeEvent;
            StoragePrefs = new Dictionary<string, object>();

            if (PlayerPrefs.HasKey(rumid_key)) {

                StoragePrefs.Add(rumid_key, Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(rumid_key)));
            } else {

                StoragePrefs.Add(rumid_key, new Dictionary<string, object>());
            }

            if (PlayerPrefs.HasKey(events_key)) {

                StoragePrefs.Add(events_key, Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(events_key)));
            } else {

                StoragePrefs.Add(events_key, new Dictionary<string, object>());
            }

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

        private class RUMErrorRecorder:ErrorRecorder {

            private Action<string, IDictionary<string, object>> _writeEvent;

            public RUMErrorRecorder(Action<string, IDictionary<string, object>> writeEvent):base() {

                this._writeEvent = writeEvent;
            }

            public override void recordError(Exception e) {
            
                IDictionary<string, object> dict = null;

                if (this._writeEvent != null) {

                    dict = new Dictionary<string, object>();
                    dict.Add("type", "rum_threaded_exception");
                    dict.Add("message", e.Message);
                    dict.Add("stack", e.StackTrace);

                    this._writeEvent("error", dict);
                }

                if (dict != null) {

                    Debug.LogError(e.Message);
                    Debug.LogError(e.StackTrace);
                }
            }
        }
    }
}