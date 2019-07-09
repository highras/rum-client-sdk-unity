using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using GameDevWare.Serialization;
using com.rum;

public class Main : MonoBehaviour
{
    private RUMClient client;

    // Start is called before the first frame update
    void Start() {

        client = new RUMClient(
            // 41000013,
            // "c23e9d90-bada-440d-8316-44790f615ec1",
            41000006,
            "7e592712-01ea-4250-bf39-e51e00c004e9",
            null,
            null,
            false 
        );

        client.GetEvent().AddListener("close", (evd) => {

            Debug.Log("Main test closed!");
        });

        client.GetEvent().AddListener("ready", (evd) => {

            Debug.Log("Main test ready!");
            client.SetUid("uid:11111111111");
        });

        if (!this.IsInvoking("SendCustomEvent")) {

            Invoke("SendCustomEvent", 5f);
        }

        if (!this.IsInvoking("SendQPS")) {

            InvokeRepeating("SendQPS", 3.0f, (1000f / 50f) / 1000f);
        }

        // client.Connect("52.83.220.166:13609", false, false);
        client.Connect("rum-us-frontgate.funplus.com:13609", false, false);
    }

    void SendCustomEvent() {

        IDictionary<string, object> attrs = new Dictionary<string, object>();
        attrs.Add("custom_debug", "test text");

        client.CustomEvent("MY_EVENT", attrs);
    }

    void SendHttpRequest() {

        // HttpWebRequest
        // AsyncGetWithWebRequest("http://www.baidu.com");

        // UnityWebRequest
        // StartCoroutine(UnityWebRequestGet("http://www.baidu.com"));
    }

    void SendQPS() {

        IDictionary<string, object> attrs = new Dictionary<string, object>();
        attrs.Add("custom_debug", "test text");

        client.CustomEvent("info", attrs);
    }

    // Update is called once per frame
    void Update() {
        
    }

    void OnApplicationQuit() {

        if (this.IsInvoking("SendCustomEvent")) {

            CancelInvoke("SendCustomEvent");
        }

        if (this.IsInvoking("SendQPS")) {

            CancelInvoke("SendQPS");
        }

        if (client != null) {

            client.Destroy();
        }
    }

    private DateTime _stime;

    void AsyncGetWithWebRequest(string url) {

        _stime = DateTime.Now;

        var request = (HttpWebRequest) WebRequest.Create(new Uri(url));
        request.BeginGetResponse(new AsyncCallback(ReadCallback), request);

        // fail: timeout or error...
        // RUMPlatform.Instance.HookHttp(request, null, 0);
    }

    void ReadCallback(IAsyncResult asynchronousResult) {

        int latency = Convert.ToInt32((DateTime.Now - _stime).TotalMilliseconds);

        HttpWebRequest request = (HttpWebRequest) asynchronousResult.AsyncState;
        HttpWebResponse response = (HttpWebResponse) request.EndGetResponse(asynchronousResult);

        RUMPlatform.Instance.HookHttp(request, response, latency);

        using (var streamReader = new StreamReader(response.GetResponseStream())) {

            var resultString = streamReader.ReadToEnd();
            Debug.Log(resultString);
        }
    }

    IEnumerator UnityWebRequestGet(string url) {

        _stime = DateTime.Now;

        using(UnityWebRequest req = UnityWebRequest.Get(url)) {

            yield return req.SendWebRequest();
     
            int latency = Convert.ToInt32((DateTime.Now - _stime).TotalMilliseconds);
            RUMPlatform.Instance.HookHttp(req, latency);

            if(req.isNetworkError || req.isHttpError) {

                Debug.Log(req.error);
            } else {

                // Show results as text
                Debug.Log(req.downloadHandler.text);
     
                // Or retrieve results as binary data
                byte[] results = req.downloadHandler.data;
            }
        }
    }
}
