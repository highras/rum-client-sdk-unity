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

        RUMClient client_1 = new RUMClient(this._pid, this._secret, null, null, false);
        yield return new WaitForSeconds(0.5f);
        string rum_id_1 = client_1.GetRumId();
        // Debug.Log("rum_id_1: " + rum_id_1);
        client_1.Destroy();
        yield return new WaitForSeconds(1.0f);

        RUMClient client_2 = new RUMClient(this._pid, this._secret, null, null, false, true, false);
        yield return new WaitForSeconds(0.5f);
        string rum_id_2 = client_2.GetRumId();
        // Debug.Log("rum_id_2: " + rum_id_2); 
        client_2.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.IsNotNull(rum_id_2);
        Assert.AreNotEqual(rum_id_1, rum_id_2);
    }

    [UnityTest]
    public IEnumerator Client_Connect_SetUid() {

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);

        long ts = FPManager.Instance.GetMilliTimestamp();
        client.SetUid("xxx-xxxxxx-xxxxxxxxxxxx");
        client.Connect(this._endpoint);
        long ds = FPManager.Instance.GetMilliTimestamp();

        yield return new WaitForSeconds(1.0f);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Debug.Log("diff: " + (ds - ts));
    }

    [UnityTest]
    public IEnumerator Client_Connect_Delay_Destroy() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });
        client.Connect(this._endpoint);
        yield return new WaitForSeconds(2.0f);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);
        
        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(1, readyCount);
        Assert.AreEqual(1, configCount);
        Assert.AreEqual(0, errorCount);
    }

    [UnityTest]
    public IEnumerator Client_Connect_Connect_Destroy() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });

        client.Connect(this._endpoint);
        client.Connect(this._endpoint);
        yield return new WaitForSeconds(2.0f);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(1, readyCount);
        Assert.AreEqual(1, configCount);
        Assert.AreEqual(1, errorCount);
    }

    [UnityTest]
    public IEnumerator Client_Connect_Destroy() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });

        client.Connect(this._endpoint);
        client.Destroy();
        yield return new WaitForSeconds(2.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(0, readyCount);
        Assert.AreEqual(0, configCount);
        Assert.AreEqual(0, errorCount);
    }

    [UnityTest]
    public IEnumerator Client_Connect_Destroy_Connect() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });

        client.Connect(this._endpoint);
        client.Destroy();
        client.Connect(this._endpoint);
        yield return new WaitForSeconds(2.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(0, readyCount);
        Assert.AreEqual(0, configCount);
        Assert.AreEqual(0, errorCount);
    }

    [UnityTest]
    public IEnumerator Client_Connect_Delay_Destroy_Delay_Connect() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });

        client.Connect(this._endpoint);

        yield return new WaitForSeconds(2.0f);

        client.Destroy();
        yield return new WaitForSeconds(1.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(1, readyCount);
        Assert.AreEqual(1, configCount);
        Assert.AreEqual(0, errorCount);

        client.Connect(this._endpoint);
        yield return new WaitForSeconds(2.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(1, readyCount);
        Assert.AreEqual(1, configCount);
        Assert.AreEqual(0, errorCount);
    }

    [UnityTest]
    public IEnumerator Client_Destroy_Connect() {

        int closeCount = 0;
        int readyCount = 0;
        int configCount = 0;
        int errorCount = 0;

        RUMClient client = new RUMClient(this._pid, this._secret, null, null, false);
        client.GetEvent().AddListener("close", (evd) => {
            closeCount++;
        });
        client.GetEvent().AddListener("ready", (evd) => {
            readyCount++;
        });
        client.GetEvent().AddListener("config", (evd) => {
            configCount++;
        });
        client.GetEvent().AddListener("error", (evd) => {
            errorCount++;
        });

        client.Destroy();
        client.Connect(this._endpoint);
        yield return new WaitForSeconds(2.0f);

        Assert.AreEqual(1, closeCount);
        Assert.AreEqual(0, readyCount);
        Assert.AreEqual(0, configCount);
        Assert.AreEqual(0, errorCount);
    }
}
