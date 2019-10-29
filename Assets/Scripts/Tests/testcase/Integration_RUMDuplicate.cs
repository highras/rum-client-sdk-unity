using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Integration_RUMDuplicate {

    [SetUp]
    public void SetUp() {
        RUMDuplicate.Instance.Init(1002);
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Duplicate_Check_2s() {
        Assert.IsTrue(RUMDuplicate.Instance.Check("Duplicate_Check_2s", 2));
        yield return new WaitForSeconds(1.0f);
        Assert.IsFalse(RUMDuplicate.Instance.Check("Duplicate_Check_2s", 2));
        yield return new WaitForSeconds(2.0f);
        Assert.IsTrue(RUMDuplicate.Instance.Check("Duplicate_Check_2s", 2));
    }
}