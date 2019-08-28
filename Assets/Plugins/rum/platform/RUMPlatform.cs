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
        private NetworkReachability _netWork;

        private static bool isInit;
        private static object lock_obj = new object();

        public void InitSelfListener() {}

        void Awake() {}
        void OnEnable() {

            this._isPause = false;
            this._isFocus = false;
            this._netWork = Application.internetReachability;

            Application.lowMemory += OnLowMemory;
            Application.logMessageReceived += OnLogCallback;
            Application.logMessageReceivedThreaded += OnLogCallbackThreaded;

            if (!this.IsInvoking("OnInfo")) {

                this.InvokeRepeating("OnInfo", 1.0f, 0.2f);
            }

            if (!this.IsInvoking("OnTimer")) {

                this.InvokeRepeating("OnTimer", 10.0f, 10.0f);
            }

            lock (lock_obj) {

                if (RUMPlatform.isInit) {

                    return;
                }

                FPManager.Instance.Init();

                RUMPlatform.Network = this._netWork.ToString();
                RUMPlatform.SystemLanguage = Application.systemLanguage.ToString();
                RUMPlatform.DeviceModel = SystemInfo.deviceModel;
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
                    //返回程序运行所在的设备类型
                    { "deviceType", SystemInfo.deviceType.ToString() },
                    //显卡的类型
                    { "graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString() },
                    //GPU支持的NPOT纹理
                    { "npotSupport", SystemInfo.npotSupport.ToString() },
                    //TimeZone
                    { "timeZone", DateTime.Now.ToString("%z") }
                };

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

            //用户定义的设备名称
            if (!this._infoDict.ContainsKey("deviceName")) {
                this._infoDict.Add("deviceName", SystemInfo.deviceName);
                return;
            }
            //设备的唯一标识符。每一台设备都有唯一的标识符
            if (!this._infoDict.ContainsKey("deviceUniqueIdentifier")) {
                this._infoDict.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier);
                return;
            }
            //显卡的唯一标识符ID
            if (!this._infoDict.ContainsKey("graphicsDeviceID")) {
                this._infoDict.Add("graphicsDeviceID", SystemInfo.graphicsDeviceID);
                return;
            }
            //显卡的名称
            if (!this._infoDict.ContainsKey("graphicsDeviceName")) {
                this._infoDict.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
                return;
            }
            //显卡的供应商
            if (!this._infoDict.ContainsKey("graphicsDeviceVendor")) {
                this._infoDict.Add("graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor);
                return;
            }
            //显卡供应商的唯一识别码ID
            if (!this._infoDict.ContainsKey("graphicsDeviceVendorID")) {
                this._infoDict.Add("graphicsDeviceVendorID", SystemInfo.graphicsDeviceVendorID);
                return;
            }
            //显卡的类型和版本
            if (!this._infoDict.ContainsKey("graphicsDeviceVersion")) {
                this._infoDict.Add("graphicsDeviceVersion", SystemInfo.graphicsDeviceVersion);
                return;
            }
            //显存大小
            if (!this._infoDict.ContainsKey("graphicsMemorySize")) {
                this._infoDict.Add("graphicsMemorySize", SystemInfo.graphicsMemorySize);
                return;
            }
            //是否支持多线程渲染
            if (!this._infoDict.ContainsKey("graphicsMultiThreaded")) {
                this._infoDict.Add("graphicsMultiThreaded", SystemInfo.graphicsMultiThreaded);
                return;
            }
            //显卡着色器的级别
            if (!this._infoDict.ContainsKey("graphicsShaderLevel")) {
                this._infoDict.Add("graphicsShaderLevel", SystemInfo.graphicsShaderLevel);
                return;
            }
            //支持的最大纹理大小
            if (!this._infoDict.ContainsKey("maxTextureSize")) {
                this._infoDict.Add("maxTextureSize", SystemInfo.maxTextureSize);
                return;
            }
            //当前处理器的数量
            if (!this._infoDict.ContainsKey("processorCount")) {
                this._infoDict.Add("processorCount", SystemInfo.processorCount);
                return;
            }
            //处理器的频率
            if (!this._infoDict.ContainsKey("processorFrequency")) {
                this._infoDict.Add("processorFrequency", SystemInfo.processorFrequency);
                return;
            }
            //处理器的名称
            if (!this._infoDict.ContainsKey("processorType")) {
                this._infoDict.Add("processorType", SystemInfo.processorType);
                return;
            }
            //支持渲染多少目标纹理
            if (!this._infoDict.ContainsKey("supportedRenderTargetCount")) {
                this._infoDict.Add("supportedRenderTargetCount", SystemInfo.supportedRenderTargetCount);
                return;
            }
            //是否支持2D数组纹理
            if (!this._infoDict.ContainsKey("supports2DArrayTextures")) {
                this._infoDict.Add("supports2DArrayTextures", SystemInfo.supports2DArrayTextures);
                return;
            }
            //是否支持3D（体积）纹理
            if (!this._infoDict.ContainsKey("supports3DTextures")) {
                this._infoDict.Add("supports3DTextures", SystemInfo.supports3DTextures);
                return;
            }
            //是否支持获取加速度计
            if (!this._infoDict.ContainsKey("supportsAccelerometer")) {
                this._infoDict.Add("supportsAccelerometer", SystemInfo.supportsAccelerometer);
                return;
            }
            //是否支持获取用于回放的音频设备
            if (!this._infoDict.ContainsKey("supportsAudio")) {
                this._infoDict.Add("supportsAudio", SystemInfo.supportsAudio);
                return;
            }
            //是否支持计算着色器
            if (!this._infoDict.ContainsKey("supportsComputeShaders")) {
                this._infoDict.Add("supportsComputeShaders", SystemInfo.supportsComputeShaders);
                return;
            }
            //是否支持获取陀螺仪
            if (!this._infoDict.ContainsKey("supportsGyroscope")) {
                this._infoDict.Add("supportsGyroscope", SystemInfo.supportsGyroscope);
                return;
            }
            //是否支持图形特效 always returns true
            if (!this._infoDict.ContainsKey("supportsImageEffects")) {
                this._infoDict.Add("supportsImageEffects", SystemInfo.supportsImageEffects);
                return;
            }
            //是否支持实例化GPU的Draw Call
            if (!this._infoDict.ContainsKey("supportsInstancing")) {
                this._infoDict.Add("supportsInstancing", SystemInfo.supportsInstancing);
                return;
            }
            //是否支持定位功能
            if (!this._infoDict.ContainsKey("supportsLocationService")) {
                this._infoDict.Add("supportsLocationService", SystemInfo.supportsLocationService);
                return;
            }
            //是否支持运动向量
            if (!this._infoDict.ContainsKey("supportsMotionVectors")) {
                this._infoDict.Add("supportsMotionVectors", SystemInfo.supportsMotionVectors);
                return;
            }
            //是否支持阴影深度
            if (!this._infoDict.ContainsKey("supportsRawShadowDepthSampling")) {
                this._infoDict.Add("supportsRawShadowDepthSampling", SystemInfo.supportsRawShadowDepthSampling);
                return;
            }
            //是否支持渲染纹理 always returns true
            if (!this._infoDict.ContainsKey("supportsRenderTextures")) {
                this._infoDict.Add("supportsRenderTextures", SystemInfo.supportsRenderTextures);
                return;
            }
            //是否支持立方体纹理 always returns true
            if (!this._infoDict.ContainsKey("supportsRenderToCubemap")) {
                this._infoDict.Add("supportsRenderToCubemap", SystemInfo.supportsRenderToCubemap);
                return;
            }
            //是否支持内置阴影
            if (!this._infoDict.ContainsKey("supportsShadows")) {
                this._infoDict.Add("supportsShadows", SystemInfo.supportsShadows);
                return;
            }
            //是否支持稀疏纹理
            if (!this._infoDict.ContainsKey("supportsSparseTextures")) {
                this._infoDict.Add("supportsSparseTextures", SystemInfo.supportsSparseTextures);
                return;
            }
            //是否支持模版缓存 always returns true
            if (!this._infoDict.ContainsKey("supportsStencil")) {
                this._infoDict.Add("supportsStencil", SystemInfo.supportsStencil);
                return;
            }
            //是否支持用户触摸震动反馈
            if (!this._infoDict.ContainsKey("supportsStencil")) {
                this._infoDict.Add("supportsVibration", SystemInfo.supportsVibration);
                return;
            }
            //不支持运行在当前设备的SystemInfo属性值
            if (!this._infoDict.ContainsKey("unsupportedIdentifier")) {
                this._infoDict.Add("unsupportedIdentifier", SystemInfo.unsupportedIdentifier);
                return;
            }
            //AndroidID only for android 
            if (!this._infoDict.ContainsKey("androidID")) {
                this._infoDict.Add("androidID", RUMPlatform.AndroidID);
                return;
            }
            //DeviceToken only for ios
            if (!this._infoDict.ContainsKey("deviceToken")) {
                this._infoDict.Add("deviceToken", RUMPlatform.DeviceToken);
                return;
            }
            //vendorIdentifier only for ios
            if (!this._infoDict.ContainsKey("vendorIdentifier")) {
                this._infoDict.Add("vendorIdentifier", RUMPlatform.VendorIdentifier);
                return;
            }
            //SecureDataPath
            if (!this._infoDict.ContainsKey("secureDataPath")) {
                this._infoDict.Add("secureDataPath", RUMPlatform.SecureDataPath);
                return;
            }

            if (this.SystemInfo_Action != null) {

                this.SystemInfo_Action(this._infoDict);
            }

            if (this.IsInvoking("OnInfo")) {

                this.CancelInvoke("OnInfo");
            }
        }

        void OnTimer() {

            NetworkReachability network = Application.internetReachability;

            if (this._netWork != network) {

                this._netWork = network;
                RUMPlatform.Network = this._netWork.ToString();

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