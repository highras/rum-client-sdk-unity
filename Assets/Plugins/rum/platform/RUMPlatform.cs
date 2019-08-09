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

        private IDictionary<string, object> _infoDict;

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

            this._nw = Application.internetReachability.ToString();
            this._lang = Application.systemLanguage.ToString();
            this._model = SystemInfo.deviceModel;
            this._os = SystemInfo.operatingSystem;
            this._sh = Screen.height;
            this._sw = Screen.width;
            this._isMobile = Application.isMobilePlatform;
            this._memorySize = SystemInfo.systemMemorySize;
            this._unityVersion = Application.unityVersion;
            this._installMode = Application.installMode.ToString();
            
            this._deviceToken = null;
            #if UNITY_IPHONE
            this._deviceToken = UnityEngine.iOS.NotificationServices.deviceToken;
            #endif

            if (this._infoDict == null) {

                this._infoDict = new Dictionary<string, object>();

                this._infoDict.Add("network", this._nw);
                this._infoDict.Add("systemLanguage", this._lang);
                this._infoDict.Add("deviceToken", this._deviceToken);
                this._infoDict.Add("deviceModel", this._model);
                this._infoDict.Add("operatingSystem", this._os);
                this._infoDict.Add("screenHeight", this._sh);
                this._infoDict.Add("screenWidth", this._sw);
                this._infoDict.Add("isMobile", this._isMobile);
                this._infoDict.Add("systemMemorySize", this._memorySize);
                this._infoDict.Add("unityVersion", this._unityVersion);
                this._infoDict.Add("installMode", this._installMode);

                //支持多种复制纹理功能的情况
                this._infoDict.Add("copyTextureSupport", SystemInfo.copyTextureSupport.ToString());
                //用户定义的设备名称
                this._infoDict.Add("deviceName", SystemInfo.deviceName);
                //返回程序运行所在的设备类型
                this._infoDict.Add("deviceType", SystemInfo.deviceType.ToString());
                //设备的唯一标识符。每一台设备都有唯一的标识符
                this._infoDict.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
                //显卡的唯一标识符ID
                this._infoDict.Add("graphicsDeviceID", SystemInfo.graphicsDeviceID);
                //显卡的名称
                this._infoDict.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
                //显卡的类型
                this._infoDict.Add("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
                //显卡的供应商
                this._infoDict.Add("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
                //显卡供应商的唯一识别码ID
                this._infoDict.Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
                //显卡的类型和版本
                this._infoDict.Add("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
                //显存大小
                this._infoDict.Add("graphicsMemorySize", SystemInfo.graphicsMemorySize);
                //是否支持多线程渲染
                this._infoDict.Add("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
                //显卡着色器的级别
                this._infoDict.Add("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
                //支持的最大纹理大小
                this._infoDict.Add("maxTextureSize", SystemInfo.maxTextureSize);
                //GPU支持的NPOT纹理
                this._infoDict.Add("npotSupport", SystemInfo.npotSupport.ToString());
                //当前处理器的数量
                this._infoDict.Add("processorCount", SystemInfo.processorCount);
                //处理器的频率
                this._infoDict.Add("processorFrequency", SystemInfo.processorFrequency);
                //处理器的名称
                this._infoDict.Add("processorType", SystemInfo.processorType);
                //支持渲染多少目标纹理
                this._infoDict.Add("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
                //是否支持2D数组纹理
                this._infoDict.Add("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
                //是否支持3D（体积）纹理
                this._infoDict.Add("supports3DTextures", SystemInfo.supports3DTextures);
                //是否支持获取加速度计
                this._infoDict.Add("supportsAccelerometer", SystemInfo.supportsAccelerometer);
                //是否支持获取用于回放的音频设备
                this._infoDict.Add("supportsAudio", SystemInfo.supportsAudio);
                //是否支持计算着色器
                this._infoDict.Add("supportsComputeShaders", SystemInfo.supportsComputeShaders);
                //是否支持获取陀螺仪
                this._infoDict.Add("supportsGyroscope", SystemInfo.supportsGyroscope);
                //是否支持图形特效
                this._infoDict.Add("supportsImageEffects", SystemInfo.supportsImageEffects);
                //是否支持实例化GPU的Draw Call
                this._infoDict.Add("supportsInstancing", SystemInfo.supportsInstancing);
                //是否支持定位功能
                this._infoDict.Add("supportsLocationService", SystemInfo.supportsLocationService);
                //是否支持运动向量
                this._infoDict.Add("supportsMotionVectors", SystemInfo.supportsMotionVectors);
                //是否支持阴影深度
                this._infoDict.Add("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
                //是否支持渲染纹理
                this._infoDict.Add("supportsRenderTextures", SystemInfo.supportsRenderTextures);
                //是否支持立方体纹理
                this._infoDict.Add("supportsRenderToCubemap", SystemInfo.supportsRenderToCubemap);
                //是否支持内置阴影
                this._infoDict.Add("supportsShadows", SystemInfo.supportsShadows);
                //是否支持稀疏纹理
                this._infoDict.Add("supportsSparseTextures", SystemInfo.supportsSparseTextures);
                //是否支持模版缓存
                this._infoDict.Add("supportsStencil", SystemInfo.supportsStencil);
                //是否支持用户触摸震动反馈
                this._infoDict.Add("supportsVibration", SystemInfo.supportsVibration);
                //不支持运行在当前设备的SystemInfo属性值
                this._infoDict.Add("unsupportedIdentifier", SystemInfo.unsupportedIdentifier);
            }

            if (!this.IsInvoking("OnInfo")) {

                this.Invoke("OnInfo", 5.0f);
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

            if (this._infoDict != null && this.SystemInfo_Action != null) {

                this.SystemInfo_Action(this._infoDict);
            }
        }

        private void OnTimer() {

            string nw = Application.internetReachability.ToString();

            if (this._nw != nw) {

                this._nw = nw;

                if (this.NetworkChange_Action != null) {

                    this.NetworkChange_Action(this._nw);
                }
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

        private string _installMode;

        public string GetInstallMode() {

            return this._installMode;
        }

        private byte[] _deviceToken;

        public byte[] GetDeviceToken() {

            return this._deviceToken; 
        }

        public void AddSelfListener() {

        }
    }
}