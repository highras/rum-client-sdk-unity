using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Integration_RUMClient {

    private int _pid = 41000013;
    private string _secret = "6212d7c7-adb7-46c0-bd82-2fed00ce90c9";
    private string _endpoint = "rum-nx-front.ifunplus.cn:13609";

    [SetUp]
    public void SetUp() {

        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Client_Connect_GetRumId() {

        string rum_id = null;
        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        yield return new WaitForSeconds(0.5f);
        rum_id = client.GetRumId();
        // Debug.Log("rum_id: " + rum_id);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.IsNotNull(rum_id);
    }

    [UnityTest]
    public IEnumerator Client_Connect_GetSession() {

        long session_id = 0;
        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        yield return new WaitForSeconds(0.5f);
        session_id = client.GetSession();
        // Debug.Log("session_id: " + session_id);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.AreNotEqual(0, session_id);
    }

    [UnityTest]
    public IEnumerator Client_Connect_ClearRumId() {

        string rum_id = null;
        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        yield return new WaitForSeconds(0.5f);
        rum_id = client.GetRumId();
        // Debug.Log("rum_id: " + rum_id);

        client.Connect(this._endpoint, true, false);
        yield return new WaitForSeconds(0.5f);
        string new_id = client.GetRumId();
        // Debug.Log("new_id: " + new_id);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.IsNotNull(new_id);
        Assert.AreNotEqual(rum_id, new_id);
    }

    [UnityTest]
    public IEnumerator Client_Connect_SetUid() {

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);

        long ts = FPManager.Instance.GetMilliTimestamp();
        client.SetUid("xxx-xxxxxx-xxxxxxxxxxxx");
        client.Connect(this._endpoint, false, false);
        long ds = FPManager.Instance.GetMilliTimestamp();
        
        yield return new WaitForSeconds(1.0f);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Debug.Log("diff: " + (ds - ts));
    }
}
