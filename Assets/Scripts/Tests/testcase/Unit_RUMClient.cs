using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using com.fpnn;
using com.rum;

public class Unit_RUMClient {
    
    [SetUp]
    public void SetUp() {

        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}


    /**
     *  RUMClient(int pid, string secret, string uid, string appv, bool debug)
     */
    [Test]
    public void Client_ZeroPid() {

        int count = 0;
        RUMClient client = new RUMClient(0, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_NegativePid() {

        int count = 0;
        RUMClient client = new RUMClient(-1, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_NullSecret() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, null, null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_EmptySecret() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_NullUid() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_EmptyUid() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", "", null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_NullAppv() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_EmptyAppv() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, "", false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_Debug(){

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, true);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_SimpleCall(){

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", "xxx-xxxxx-xxxxxxxxxxx", "2.9.00", true);
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  Destroy()
     */
    [Test]
    public void Client_Destroy() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  Connect(string endpoint, bool clearRumId, bool clearEvents)
     */
    [Test]
    public void Client_Connect_NullEndpoint() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.Connect(null);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_Connect_EmptyEndpoint() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.Connect("");
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  GetSession()
     */
    [Test]
    public void Client_GetSession() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.GetSession();
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  GetRumId()
     */
    [Test]
    public void Client_GetRumId() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.GetRumId();
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  SetUid(string value)
     */
    [Test]
    public void Client_SetUid_NullValue() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.SetUid(null);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_SetUid_EmptyValue() {

        int count = 0;
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.SetUid("");
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  CustomEvent(string ev, IDictionary<string, object> attrs)
     */
    [Test]
    public void Client_CustomEvent_NullEv() {

        int count = 0;
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {

            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.CustomEvent(null, ev_dict);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_CustomEvent_EmptyEv() {

        int count = 0;
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {

            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.CustomEvent("", ev_dict);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_CustomEvent_NullAttrs() {

        int count = 0;
        IDictionary<string, object> ev_dict = null;
        
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.CustomEvent("test", ev_dict);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_CustomEvent_EmptyAttrs() {

        int count = 0;
        IDictionary<string, object> ev_dict = new Dictionary<string, object>();
        
        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.CustomEvent("test", ev_dict);
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  HttpEvent(string url, string method, int status, long reqsize, long respsize, int latency, IDictionary<string, object> attrs)
     */
    [Test]
    public void Client_HttpEvent_NullUrl() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent(null, "POST", 200, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HttpEvent_EmptyUrl() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent("", "POST", 200, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HttpEvent_NullMethod() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent("http://www.github.com", null, 200, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HttpEvent_EmptyMethod() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent("http://www.github.com", "", 200, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HttpEvent_ZeroStatus() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent("http://www.github.com", "POST", 0, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HttpEvent_NegativeStatus() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HttpEvent("http://www.github.com", "POST", -1, 10, 10, 10, attrs);
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  HookHttp(System.Net.HttpWebRequest req, System.Net.HttpWebResponse res, int latency)
     */
    [Test]
    public void Client_HookHttp_NullHttpWebRequest() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HookHttp(null, null, 10);
        Assert.AreEqual(0, count);

        client.Destroy();
    }

    [Test]
    public void Client_HookHttp_NullHttpWebResponse() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HookHttp(null, null, 10);
        Assert.AreEqual(0, count);

        client.Destroy();
    }


    /**
     *  HookHttp(UnityEngine.Networking.UnityWebRequest req, int latency)
     */
    [Test]
    public void Client_HookHttp_NullUnityWebRequest() {

        int count = 0;
        IDictionary<string, object> attrs = new Dictionary<string, object>() {

            { "test", "test text" }
        };

        RUMClient client = new RUMClient(41000013, "6212d7c7-adb7-46c0-bd82-2fed00ce90c9", null, null, false);
        client.HookHttp(null, 10);
        Assert.AreEqual(0, count);

        client.Destroy();
    }
}