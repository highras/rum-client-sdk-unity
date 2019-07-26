using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using GameDevWare.Serialization;
using com.rum;
using com.test;

public class Main : MonoBehaviour {

    public interface ITestCase {

        void StartTest();
        void StopTest();
    }

    private ITestCase _testCase;

    void Start() {

        //TestCase
        this._testCase = new TestCase();

        //SingleClientSend
        this._testCase = new SingleClientSend();

        if (this._testCase != null) {

            this._testCase.StartTest();
        }

        // if (!this.IsInvoking("SendHttpRequest")) {

        //     InvokeRepeating("SendHttpRequest", 5.0f, 10.0f);
        // }
    }

    void SendHttpRequest() {

        // HttpWebRequest
        AsyncGetWithWebRequest("http://www.baidu.com");

        // UnityWebRequest
        StartCoroutine(UnityWebRequestGet("http://www.google.com"));
    }

    void Update() {}

    void OnApplicationQuit() {

        if (this.IsInvoking("SendHttpRequest")) {

            CancelInvoke("SendHttpRequest");
        }

        if (this._testCase != null) {

            this._testCase.StopTest();
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
            // Debug.Log(resultString);
        }
    }

    IEnumerator UnityWebRequestGet(string url) {

        _stime = DateTime.Now;

        using(UnityWebRequest req = UnityWebRequest.Get(url)) {

            yield return req.SendWebRequest();
     
            int latency = Convert.ToInt32((DateTime.Now - _stime).TotalMilliseconds);
            RUMPlatform.Instance.HookHttp(req, latency);

            if(req.isNetworkError || req.isHttpError) {

                // Debug.Log(req.error);
            } else {

                // Show results as text
                // Debug.Log(req.downloadHandler.text);
     
                // Or retrieve results as binary data
                byte[] results = req.downloadHandler.data;
            }
        }
    }
}
