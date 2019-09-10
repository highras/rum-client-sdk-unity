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

        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Platform_FPSInfo() {

        int count = 0;
        EventDelegate callback = (evd) => {

            IDictionary<string, object> dict = (IDictionary<string, object>) evd.GetPayload();

            if (dict.ContainsKey("fps_info")) {

                count++;

                if (count == 1) {

                    Debug.Log("fps_info: " + Json.SerializeToString(dict));
                }
            }
        };
        RUMPlatform.Instance.Event.AddListener(RUMPlatform.PLATFORM_EVENT, callback);

        yield return new WaitForSeconds(20.0f);
        RUMPlatform.Instance.Event.RemoveListener(RUMPlatform.PLATFORM_EVENT, callback);
        Assert.AreNotEqual(0, count);
    }

    [UnityTest]
    public IEnumerator Platform_SystemInfo() {

        int count = 0;
        EventDelegate callback = (evd) => {

            IDictionary<string, object> dict = (IDictionary<string, object>) evd.GetPayload();

            if (dict.ContainsKey("system_info")) {

                count++;

                if (count == 1) {

                    Debug.Log("system_info: " + Json.SerializeToString(dict));
                }
            }
        };
        RUMPlatform.Instance.Event.AddListener(RUMPlatform.PLATFORM_EVENT, callback);

        yield return new WaitForSeconds(30.0f);
        RUMPlatform.Instance.Event.RemoveListener(RUMPlatform.PLATFORM_EVENT, callback);
        Assert.AreNotEqual(0, count);
    }
}
