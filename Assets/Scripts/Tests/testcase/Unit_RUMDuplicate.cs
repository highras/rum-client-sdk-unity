using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using com.fpnn;
using com.rum;

public class Unit_RUMDuplicate {

    [SetUp]
    public void SetUp() {
        RUMDuplicate.Instance.Init(1002);
    }

    [TearDown]
    public void TearDown() {}

    /**
     *  Init(int pid)
     */
    [Test]
    public void Duplicate_Init_ZeroPid() {
        int count = 0;
        RUMDuplicate.Instance.Init(0);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Init_NegativePid() {
        int count = 0;
        RUMDuplicate.Instance.Init(-1);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Init_Simple() {
        int count = 0;
        RUMDuplicate.Instance.Init(1002);
        Assert.AreEqual(0, count);
    }


    /**
     *  Check(string key, int ttl)
     */
    [Test]
    public void Duplicate_Check_NullKey() {
        int count = 0;
        RUMDuplicate.Instance.Check(null, 2);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Check_EmptyKey() {
        int count = 0;
        RUMDuplicate.Instance.Check("", 2);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Check_ZeroTTL() {
        int count = 0;
        RUMDuplicate.Instance.Check("Duplicate_Check_EmptyKey", 0);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Check_NegativeTTL() {
        int count = 0;
        RUMDuplicate.Instance.Check("Duplicate_Check_EmptyKey", -1);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Duplicate_Check_Simple() {
        int count = 0;
        RUMDuplicate.Instance.Check("Duplicate_Check_EmptyKey", 2);
        Assert.AreEqual(0, count);
    }
}
