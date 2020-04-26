using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using com.fpnn.rum;

class RUMDemo : Main.ITestCase
{
    private volatile bool running;
    private List<Thread> workerThreads;
    private long startNewSession;

    public void Start()
    {
        running = true;
        startNewSession = 0;
        workerThreads = new List<Thread>();

        for (int i = 0; i < 10; i++)
        {
            Thread th = new Thread(DemoWorker);
            workerThreads.Add(th);
            th.Start();
        }

        DemoExceptionReport();

        ScheduleWorker();
    }
    public void Stop()
    {
        running = false;

        foreach (Thread th in workerThreads)
            th.Join();
    }

    private void DemoExceptionReport()
    {
        try
        {
            Dictionary<string, string> invalidDict = null;
            Debug.Log("-- count of invalidDict count: " + invalidDict.Count);
        }
        catch (Exception e)
        {
            RUM.CustomError("Demo Exception", "demo null exception", e);
        }
    }

    public void ScheduleWorker()
    {
        const int sleepMS = 200;
        const int period = 5 * 1000 * 60 / sleepMS;
        int tick = 0;

        while (running)
        {
            Thread.Sleep(sleepMS);
            tick += 1;
            if (tick >= period)
            {
                tick = 0;
                Interlocked.Increment(ref startNewSession);
            }
        }
    }

    public void DemoWorker()
    {
        while (running)
        {
            Thread.Sleep(3000);

            long v = Interlocked.Exchange(ref startNewSession, 0);
            if (v > 0)
            {
                RUM.NewSession();

                string uid = com.fpnn.ClientEngine.GetCurrentSeconds().ToString();
                RUM.SetUid(uid);
            }

            Dictionary<string, object> info = new Dictionary<string, object>();
            info.Add("demoA", 21212);
            info.Add("demoB", "sdsds");
            info.Add("demoC", null);
            info.Add("demoD", "");
            info.Add("demoE", "测试用例");
            info.Add("demoF", 123.45678);

            v = com.fpnn.ClientEngine.GetCurrentSeconds() % 3;
            if (v == 1)
            {
                RUM.CustomEvent("demo", info);
            }
            else
            {
                RUM.CustomEvent("test", info);
            }
        }
    }
}