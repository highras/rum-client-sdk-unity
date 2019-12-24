using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Integration_RUMPlatform {

    [SetUp]
    public void SetUp() {
#if RUM_ENABLE_LOCATION_SERVICE
        RUMRegistration.Register(null);
#else
        RUMRegistration.Register();
#endif
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Platform_Info() {
        int fps_count = 0;
        EventDelegate fps_callback = (evd) => {
            IDictionary<string, object> dict = (IDictionary<string, object>) evd.GetPayload();

            if (dict.ContainsKey("fps_info")) {
                fps_count++;

                if (fps_count == 1) {
                    Debug.Log("fps_info: " + Json.SerializeToString(dict));
                }
            }
        };
        RUMPlatform.Instance.Event.AddListener(RUMPlatform.PLATFORM_EVENT, fps_callback);
        int info_count = 0;
        EventDelegate info_callback = (evd) => {
            IDictionary<string, object> dict = (IDictionary<string, object>) evd.GetPayload();

            if (dict.ContainsKey("system_info")) {
                info_count++;

                if (info_count == 1) {
                    Debug.Log("system_info: " + Json.SerializeToString(dict));
                }
            }
        };
        RUMPlatform.Instance.Event.AddListener(RUMPlatform.PLATFORM_EVENT, info_callback);
        yield return new WaitForSeconds(80.0f);
        RUMPlatform.Instance.Event.RemoveListener(RUMPlatform.PLATFORM_EVENT, fps_callback);
        RUMPlatform.Instance.Event.RemoveListener(RUMPlatform.PLATFORM_EVENT, info_callback);
        Assert.AreNotEqual(0, fps_count);
        // Assert.AreNotEqual(0, info_count);
    }
}
