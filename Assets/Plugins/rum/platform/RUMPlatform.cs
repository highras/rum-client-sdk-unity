using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using GameDevWare.Serialization;
using com.fpnn;

using UnityEngine;
using UnityEngine.Networking;

namespace com.rum {

    public class RUMPlatform:Singleton<RUMPlatform> {

        public Action AppFg_Action;
        public Action AppBg_Action;
        public Action<int> LowMemory_Action;
        public Action<string> NetworkChange_Action;
        public Action<IDictionary<string, object>> SystemInfo_Action;

        private bool _isPause;
        private bool _isFocus;

        private static RUMPlatform instance_self = null;
        private static object lock_obj = new object();

        public static bool HasInstance() {

            lock (lock_obj) {

                return instance_self != null;
            }
        }

        void Awake() {}

        void OnEnable() {

            lock (lock_obj) {

                instance_self = this;
            }

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

            if (!this.IsInvoking("OnInfo")) {

                this.Invoke("OnInfo", 20.0f);
            }

            if (!this.IsInvoking("OnTimer")) {

                this.InvokeRepeating("OnTimer", 10.0f, 10.0f);
            }
        }

        void Start() {}

        void OnDisable() {

            Application.lowMemory -= OnLowMemory;
            Application.logMessageReceived -= OnLogCallback;
            Application.logMessageReceivedThreaded -= OnLogCallbackThreaded;

            if (this.IsInvoking("OnInfo")) {

                this.CancelInvoke("OnInfo");
            }

            if (this.IsInvoking("OnTimer")) {

                this.CancelInvoke("OnTimer");
            }
        }

        void OnApplicationPause() {
 
            if (!this._isPause) {
             
                if (this.AppBg_Action != null) {

                    this.AppBg_Action();
                }
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

                this._isFocus = true;

                if (this.AppFg_Action != null) {

                    this.AppFg_Action();
                }
            }
        }

        void OnLowMemory() {

            if (this.LowMemory_Action != null) {

                this.LowMemory_Action(this.GetMemorySize());
            }
        }

        private void OnInfo() {

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("system_memory", this.GetMemorySize());
            dict.Add("unity_version", this.GetUnityVersion());
            dict.Add("install_mode", this.GetInstallMode());
            dict.Add("device_token", this.GetDeviceToken());

            if (this.SystemInfo_Action != null) {

                this.SystemInfo_Action(dict);
            }
        }

        private void OnTimer() {

            bool change = false;
            NetworkReachability internetReachability = Application.internetReachability;

            if (internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork) {

                if (this._nw != "3G/4G") {

                    change = true;
                    this._nw = "3G/4G";
                }
            }

            if (internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork) {

                if (this._nw != "WIFI") {

                    change = true;
                    this._nw = "WIFI";
                }
            }

            if (internetReachability == NetworkReachability.NotReachable){

                if (this._nw != "NONE") {

                    change = true;
                    this._nw = "NONE";
                }
            }

            if (change && this.NetworkChange_Action != null) {

                this.NetworkChange_Action(this._nw);
            }
        }

        private void OnLogCallback(string logString, string stackTrace, LogType type) {

            if (type == LogType.Assert) {

                this.WriteException("crash", "main_assert", logString, stackTrace);
            }

            if (type == LogType.Exception) {

                this.WriteException("error", "main_exception", logString, stackTrace);
            }
        }

        private void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {

            if (type == LogType.Exception) {

                this.WriteException("error", "threaded_exception", logString, stackTrace);
            }
        }

        private void WriteException(string ev, string type, string message, string stack) {

            IDictionary<string, object> dict = new Dictionary<string, object>();

            dict.Add("type", type);

            if (!string.IsNullOrEmpty(message)) {

                dict.Add("message", message);
            }

            if (!string.IsNullOrEmpty(stack)) {

                dict.Add("stack", stack);
            }

            if (this._writeEvent != null) {

                this._writeEvent(ev, dict);
            }
        }

        private Action<string, IDictionary<string, object>> _writeEvent;

        public void InitPrefs(Action<string, IDictionary<string, object>> writeEvent) {

            this._writeEvent = writeEvent;
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

        private string _androidID;

        public string GetAndroidID () {

            if (!string.IsNullOrEmpty(this._androidID)) {

                return this._androidID;
            }

            #if !UNITY_EDITOR && UNITY_ANDROID
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var contentResolver = currentActivity.Call<AndroidJavaObject> ("getContentResolver"))
            using (var secure = new AndroidJavaClass ("android.provider.Settings$Secure")) {

                this._androidID = secure.CallStatic<string> ("getString", contentResolver, "android_id");
            }
            #endif

            return this._androidID;
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
    }
}