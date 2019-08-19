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

        /**
         *  NotReachable                     Network is not reachable. (NONE)
         *  ReachableViaCarrierDataNetwork   Network is reachable via carrier data network. (2G/3G/4G)
         *  ReachableViaLocalAreaNetwork     Network is reachable via WiFi or cable. (WIFI)
         */
        public static string Network;

        /**
         *  Unknown         Application install mode unknown.
         *  Store           Application installed via online store.
         *  DeveloperBuild  Application installed via developer build.
         *  Adhoc           Application installed via ad hoc distribution.
         *  Enterprise      Application installed via enterprise distribution.
         *  Editor          Application running in editor.
         */
        public static string InstallMode;

        public static string SystemLanguage;
        public static string DeviceModel;
        public static string OperatingSystem;
        public static int ScreenHeight;
        public static int ScreenWidth;
        public static bool IsMobilePlatform;
        public static int SystemMemorySize;
        public static string UnityVersion;
        public static string DeviceToken;
        public static string VendorIdentifier;
        public static string AndroidID;
        public static string SecureDataPath;

        public static string Manu;
        public static string SystemVersion;
        public static string Carrier;
        public static string From;

        public static bool HasInit() {

            lock (lock_obj) {

                return RUMPlatform.isInit;
            }
        }

        public Action AppFg_Action;
        public Action AppBg_Action;
        public Action<int> LowMemory_Action;
        public Action<string> NetworkChange_Action;
        public Action<IDictionary<string, object>> SystemInfo_Action;
        public Action<string, IDictionary<string, object>> WriteEvent_Action;

        private IDictionary<string, object> _infoDict;

        private bool _isPause;
        private bool _isFocus;

        private static bool isInit;
        private static object lock_obj = new object();

        public void InitSelfListener() {}

        void Awake() {}
        void OnEnable() {

            this._isPause = false;
            this._isFocus = false;

            Application.lowMemory += OnLowMemory;
            Application.logMessageReceived += OnLogCallback;
            Application.logMessageReceivedThreaded += OnLogCallbackThreaded;

            if (!this.IsInvoking("OnInfo")) {

                this.Invoke("OnInfo", 5.0f);
            }

            if (!this.IsInvoking("OnTimer")) {

                this.InvokeRepeating("OnTimer", 10.0f, 10.0f);
            }

            lock (lock_obj) {

                if (RUMPlatform.isInit) {

                    return;
                }

                RUMPlatform.SystemLanguage = Application.systemLanguage.ToString();
                RUMPlatform.DeviceModel = SystemInfo.deviceModel;
                RUMPlatform.Network = Application.internetReachability.ToString();
                RUMPlatform.OperatingSystem = SystemInfo.operatingSystem;
                RUMPlatform.ScreenHeight = Screen.height;
                RUMPlatform.ScreenWidth = Screen.width;
                RUMPlatform.IsMobilePlatform = Application.isMobilePlatform;
                RUMPlatform.SystemMemorySize = SystemInfo.systemMemorySize;
                RUMPlatform.UnityVersion = Application.unityVersion;
                RUMPlatform.InstallMode = Application.installMode.ToString();
                RUMPlatform.SecureDataPath = Application.temporaryCachePath;

                #if !UNITY_EDITOR && UNITY_IPHONE
                byte[] token = UnityEngine.iOS.NotificationServices.deviceToken;
                if (token != null) {
                    RUMPlatform.DeviceToken = System.BitConverter.ToString(token).Replace("-", "");
                }
                RUMPlatform.VendorIdentifier = UnityEngine.iOS.Device.vendorIdentifier;
                RUMPlatform.SecureDataPath = Application.persistentDataPath;
                #elif !UNITY_EDITOR && UNITY_ANDROID
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity")) {
                    using (var getFilesDir = currentActivity.Call<AndroidJavaObject>("getFilesDir")) {
                        RUMPlatform.SecureDataPath = getFilesDir.Call<string>("getCanonicalPath");
                    }
                    using (var contentResolver = currentActivity.Call<AndroidJavaObject> ("getContentResolver"))
                    using (var secure = new AndroidJavaClass ("android.provider.Settings$Secure")) {
                        RUMPlatform.AndroidID = secure.CallStatic<string> ("getString", contentResolver, "android_id");
                    }
                }
                #endif

                RUMPlatform.isInit = true;
                Debug.Log("[RUMPlatform] Init Complete!");
            }
        }

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

                this.LowMemory_Action(RUMPlatform.SystemMemorySize);
            }
        }

        void OnInfo() {

            if (this._infoDict == null) {

                this._infoDict = new Dictionary<string, object>() {

                    { "network", RUMPlatform.Network },
                    { "systemLanguage", RUMPlatform.SystemLanguage },
                    { "deviceModel", RUMPlatform.DeviceModel },
                    { "operatingSystem", RUMPlatform.OperatingSystem },
                    { "screenHeight", RUMPlatform.ScreenHeight },
                    { "screenWidth", RUMPlatform.ScreenWidth },
                    { "isMobile", RUMPlatform.IsMobilePlatform },
                    { "systemMemorySize", RUMPlatform.SystemMemorySize },
                    { "unityVersion", RUMPlatform.UnityVersion },
                    { "installMode", RUMPlatform.InstallMode },

                    //支持多种复制纹理功能的情况
                    { "copyTextureSupport", SystemInfo.copyTextureSupport.ToString() },
                    //用户定义的设备名称
                    { "deviceName", SystemInfo.deviceName },
                    //返回程序运行所在的设备类型
                    { "deviceType", SystemInfo.deviceType.ToString() },
                    //设备的唯一标识符。每一台设备都有唯一的标识符
                    { "deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier },
                    //显卡的唯一标识符ID
                    { "graphicsDeviceID", SystemInfo.graphicsDeviceID },
                    //显卡的名称
                    { "graphicsDeviceName", SystemInfo.graphicsDeviceName },
                    //显卡的类型
                    { "graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString() },
                    //显卡的供应商
                    { "graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor },
                    //显卡供应商的唯一识别码ID
                    { "graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID },
                    //显卡的类型和版本
                    { "graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion },
                    //显存大小
                    { "graphicsMemorySize", SystemInfo.graphicsMemorySize },
                    //是否支持多线程渲染
                    { "graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded },
                    //显卡着色器的级别
                    { "graphicsShaderLevel", SystemInfo.graphicsShaderLevel },
                    //支持的最大纹理大小
                    { "maxTextureSize", SystemInfo.maxTextureSize },
                    //GPU支持的NPOT纹理
                    { "npotSupport", SystemInfo.npotSupport.ToString() },
                    //当前处理器的数量
                    { "processorCount", SystemInfo.processorCount },
                    //处理器的频率
                    { "processorFrequency", SystemInfo.processorFrequency },
                    //处理器的名称
                    { "processorType", SystemInfo.processorType },
                    //支持渲染多少目标纹理
                    { "supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount },
                    //是否支持2D数组纹理
                    { "supports2DArrayTextures", SystemInfo.supports2DArrayTextures },
                    //是否支持3D（体积）纹理
                    { "supports3DTextures", SystemInfo.supports3DTextures },
                    //是否支持获取加速度计
                    { "supportsAccelerometer", SystemInfo.supportsAccelerometer },
                    //是否支持获取用于回放的音频设备
                    { "supportsAudio", SystemInfo.supportsAudio },
                    //是否支持计算着色器
                    { "supportsComputeShaders", SystemInfo.supportsComputeShaders },
                    //是否支持获取陀螺仪
                    { "supportsGyroscope", SystemInfo.supportsGyroscope },
                    //是否支持图形特效
                    { "supportsImageEffects", SystemInfo.supportsImageEffects },
                    //是否支持实例化GPU的Draw Call
                    { "supportsInstancing", SystemInfo.supportsInstancing },
                    //是否支持定位功能
                    { "supportsLocationService", SystemInfo.supportsLocationService },
                    //是否支持运动向量
                    { "supportsMotionVectors", SystemInfo.supportsMotionVectors },
                    //是否支持阴影深度
                    { "supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling },
                    //是否支持渲染纹理
                    { "supportsRenderTextures", SystemInfo.supportsRenderTextures },
                    //是否支持立方体纹理
                    { "supportsRenderToCubemap", SystemInfo.supportsRenderToCubemap },
                    //是否支持内置阴影
                    { "supportsShadows", SystemInfo.supportsShadows },
                    //是否支持稀疏纹理
                    { "supportsSparseTextures", SystemInfo.supportsSparseTextures },
                    //是否支持模版缓存
                    { "supportsStencil", SystemInfo.supportsStencil },
                    //是否支持用户触摸震动反馈
                    { "supportsVibration", SystemInfo.supportsVibration },
                    //不支持运行在当前设备的SystemInfo属性值
                    { "unsupportedIdentifier", SystemInfo.unsupportedIdentifier },
                    //AndroidID only for android 
                    { "androidID", RUMPlatform.AndroidID },
                    //DeviceToken only for ios
                    { "deviceToken", RUMPlatform.DeviceToken },
                    //vendorIdentifier only for ios
                    { "vendorIdentifier", RUMPlatform.VendorIdentifier },
                    //SecureDataPath
                    { "secureDataPath", RUMPlatform.SecureDataPath }
                };
            }

            if (this.SystemInfo_Action != null) {

                this.SystemInfo_Action(this._infoDict);
            }
        }

        void OnTimer() {

            string network = Application.internetReachability.ToString();

            if (RUMPlatform.Network != network) {

                RUMPlatform.Network = network;

                if (this.NetworkChange_Action != null) {

                    this.NetworkChange_Action(RUMPlatform.Network);
                }
            }
        }

        void OnLogCallback(string logString, string stackTrace, LogType type) {

            if (type == LogType.Assert) {

                this.OnException("crash", "main_assert", logString, stackTrace);
            }

            if (type == LogType.Exception) {

                this.OnException("error", "main_exception", logString, stackTrace);
            }
        }

        void OnLogCallbackThreaded(string logString, string stackTrace, LogType type) {

            if (type == LogType.Exception) {

                this.OnException("error", "threaded_exception", logString, stackTrace);
            }
        }

        void OnException(string ev, string type, string message, string stack) {

            IDictionary<string, object> dict = new Dictionary<string, object>() {

                { "type", type },
                { "message", message },
                { "stack", stack }
            };

            if (this.WriteEvent_Action != null) {

                this.WriteEvent_Action(ev, dict);
            }
        }
    }
}