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
        private IDictionary<string, object> _prefs;

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
        }

        void Start() {

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

        void OnApplicationPause() {
 
            if (!this._isPause) {
             
                this._event.FireEvent(new EventData("app_bg"));
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

            if (this._prefs != null) {

                lock(this._prefs) {

                    foreach (KeyValuePair<string, object> kvp in this._prefs) {

                        PlayerPrefs.SetString(kvp.Key, Json.SerializeToString(kvp.Value));
                    }
                }

                PlayerPrefs.Save();
            }

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

            Invoke("OnTimer", RUMConfig.LOCAL_STORAGE_DELAY / 1000);
        }

        public void InitPrefs(string rumid_key, string events_key) {

            this._prefs = new Dictionary<string, object>();

            this.GetItem(rumid_key);
            this.GetItem(events_key);
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

        public void SetItem(string key, IDictionary<string, object> items) {

            if (this._prefs == null) {

                return;
            }

            lock(this._prefs) {

                if (this._prefs.ContainsKey(key)) {

                    this._prefs[key] = items;
                } else {

                    this._prefs.Add(key, items);
                }
            }
        }

        public IDictionary<string, object> GetItem(string key) {

            if (this._prefs == null) {

                return null;
            }

            lock(this._prefs) {

                if (!this._prefs.ContainsKey(key)) {

                    if (PlayerPrefs.HasKey(key)) {

                        this._prefs.Add(key, Json.Deserialize<IDictionary<string, object>>(PlayerPrefs.GetString(key)));
                    } else {

                        this._prefs.Add(key, new Dictionary<string, object>());
                    }
                } 
            }

            return (IDictionary<string, object>)this._prefs[key];
        }

        public void AddSelfListener() {}
    }
}