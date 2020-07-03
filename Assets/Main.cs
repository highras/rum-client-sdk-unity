using System.Threading;
using UnityEngine;
using com.fpnn;
using com.fpnn.rum;

public class Main : MonoBehaviour
{
    public interface ITestCase
    {
        void Start();
        void Stop();
    }

    Thread testThread;
    ITestCase tester;

    void Start()
    {
        //-- Early event
        RUM.InitialLoadingBeginEvent();

        //-- Event before RUM inited
        System.Collections.Generic.Dictionary<string, object> earlyDemo = new System.Collections.Generic.Dictionary<string, object>();
        earlyDemo.Add("earlyDemoA", 21212);
        earlyDemo.Add("earlyDemoB", "sdsds");
        earlyDemo.Add("earlyDemoC", null);
        earlyDemo.Add("earlyDemoD", "");
        RUM.CustomEvent("demo", earlyDemo);

        com.fpnn.common.ErrorRecorder errorRecorder = new ErrorRecorder();

        //-- Init FPNN SDK
        {
            Config config = new Config
            {
                errorRecorder = errorRecorder
            };
            ClientEngine.Init(config);
        }

        //-- Init RUM SDK
        {
            RUMConfig config = new RUMConfig("52.83.220.166:13609",
                41033, "fdb82dd0-6f33-49d9-b0b6-4f4a01bc4054", "demo version")
            {
                errorRecorder = errorRecorder
            };

            RUM.Init(config);
        }

        testThread = new Thread(TestMain)
        {
            IsBackground = true
        };
        testThread.Start();

        RUM.InitialLoadingFinishEvent();
    }

    void TestMain()
    {
        tester = new RUMDemo();
        tester.Start();
    }

    void OnApplicationQuit()
    {
        tester.Stop();
        Debug.Log("Test App exited.");
    }
}
