using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using com.fpnn;
using com.rum;

public class Unit_RUMFile {
    
    private RUMFile _file;
    
    [SetUp]
    public void SetUp() {

        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}


    /**
     *  RUMFile(int pid, bool debug)
     */
    [Test]
    public void File_ZeroPid() {

        int count = 0;
        RUMFile file = new RUMFile(0, false);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void File_NegativePid() {

        int count = 0;
        RUMFile file = new RUMFile(-1, false);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void File_Debug() {

        int count = 0;
        RUMFile file = new RUMFile(100, true);
        Assert.AreEqual(0, count);
    }


    /**
     *  SaveRumLog(int index, byte[] content)
     */
    [Test]
    public void File_SaveRumLog_ZeroIndex() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveRumLog(0, new byte[100]);
        Assert.IsTrue(res.success);
        Assert.AreEqual(100, ((byte[])res.content).Length);

        file.ClearAllFile();
    }

    [Test]
    public void File_SaveRumLog_NegativeIndex() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveRumLog(-1, new byte[100]);
        Assert.IsTrue(res.success);
        Assert.AreEqual(100, ((byte[])res.content).Length);

        file.ClearAllFile();
    }

    [Test]
    public void File_SaveRumLog_NullContent() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveRumLog(1, null);
        Assert.IsFalse(res.success);
        Assert.IsNotNull(res.content);

        file.ClearAllFile();
    }

    [Test]
    public void File_SaveRumLog_EmptyContent() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveRumLog(1, new byte[0]);
        Assert.IsTrue(res.success);
        Assert.AreEqual(0, ((byte[])res.content).Length);

        file.ClearAllFile();
    }


    /**
     *  LoadRumLog()
     */
    [Test]
    public void File_LoadRumLog() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.LoadRumLog();
        Assert.IsFalse(res.success);
        Assert.IsNotNull(res.content);

        file.ClearAllFile();
    }


    /**
     *  SaveStorage(byte[] content)
     */
    [Test]
    public void File_SaveStorage_NullContent() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveStorage(null);
        Assert.IsFalse(res.success);
        Assert.IsNotNull(res.content);

        file.ClearAllFile();
    }

    [Test]
    public void File_SaveStorage_EmptyContent() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.SaveStorage(new byte[0]);
        Assert.IsTrue(res.success);
        Assert.AreEqual(0, ((byte[])res.content).Length);

        file.ClearAllFile();
    }


    /**
     *  LoadStorage()
     */
    [Test]
    public void File_LoadStorage() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.LoadStorage();
        Assert.IsFalse(res.success);
        Assert.IsNotNull(res.content);

        file.ClearAllFile();
    }


    /**
     *  ClearAllFile()
     */
    [Test]
    public void File_ClearAllFile() {

        RUMFile file = new RUMFile(100, false);
        RUMFile.Result res = file.ClearAllFile();
        Assert.IsTrue(res.success);
    }
}
