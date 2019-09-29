using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using com.fpnn;
using com.rum;

public class Unit_RUMEvent {

    [SetUp]
    public void SetUp() {
        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}


    /**
     *  RUMEvent(int pid, bool debug, Action sendQuest, Action openEvent)
     */
    [Test]
    public void Event_ZeroPid() {
        int count = 0;
        RUMEvent evt = new RUMEvent(0, false, () => {}, () => {});
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_NegativePid() {
        int count = 0;
        RUMEvent evt = new RUMEvent(-1, false, () => {}, () => {});
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_Debug() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, true, () => {}, () => {});
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_NullSendQuest() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, null, () => {});
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_NullOpenEvent() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, null);
        Assert.AreEqual(0, count);
    }


    /**
     *  Init(bool clearRumId, bool clearEvents, DumpDelegate clientDump)
     */
    [Test]
    public void Event_Init_ClearRumId() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Init(true, false, () => {
            return "";
        });
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_Init_ClearEvents() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Init(false, true, () => {
            return "";
        });
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_Init_ClearRumId_ClearEvents() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Init(true, true, () => {
            return "";
        });
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_Init_NullDelegate() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Init(false, false, null);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_Init_DelegateNull() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Init(false, false, () => {
            return "";
        });
        Assert.AreEqual(0, count);
    }


    /**
     *  SetSession(long value)
     */
    [Test]
    public void Event_SetSession_ZeroValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetSession(0);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_SetSession_NegativeValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetSession(-1);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_SetSession_SimpleValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetSession(1568102471001);
        Assert.AreEqual(0, count);
    }


    /**
     *  UpdateConfig(IDictionary<string, object> value)
     */
    [Test]
    public void Event_UpdateConfig_NullValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.UpdateConfig(null);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_UpdateConfig_EmptyValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.UpdateConfig(new Dictionary<string, object>());
        Assert.AreEqual(0, count);
    }


    /**
     *  WriteEvent(IDictionary<string, object> dict)
     */
    [Test]
    public void Event_WriteEvent_NullDict() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.WriteEvent(null);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_WriteEvent_EmptyDict() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.WriteEvent(new Dictionary<string, object>());
        Assert.AreEqual(0, count);
    }


    /**
     *  WriteEvents(ICollection<object> items)
     */
    [Test]
    public void Event_WriteEvents_NullItems() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.WriteEvents(null);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_WriteEvents_EmptyItems() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.WriteEvents(new List<object>());
        Assert.AreEqual(0, count);
    }


    /**
     *  GetRumId()
     */
    [Test]
    public void Event_GetRumId_NullItems() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.IsNull(evt.GetRumId());
    }


    /**
     *  GetTimestamp()
     */
    [Test]
    public void Event_GetTimestamp_NullItems() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.AreEqual(0, evt.GetTimestamp());
    }


    /**
     *  SetTimestamp(long value)
     */
    [Test]
    public void Event_SetTimestamp_ZeroValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetTimestamp(0);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_SetTimestamp_NegativeValue() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetTimestamp(-1);
        Assert.AreEqual(0, count);
    }


    /**
     *  IsFirst()
     */
    [Test]
    public void Event_IsFirst() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.IsFalse(evt.IsFirst());
    }


    /**
     *  Destroy()
     */
    [Test]
    public void Event_Destroy() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.Destroy();
        Assert.AreEqual(0, count);
    }


    /**
     *  GetStorageSize()
     */
    [Test]
    public void Event_GetStorageSize() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.AreEqual(0, evt.GetStorageSize());
    }


    /**
     *  SetSizeLimit(int value)
     */
    [Test]
    public void Event_SetSizeLimit_ZeroValue() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetSizeLimit(0);
        Assert.AreEqual(0, evt.GetStorageSize());
    }

    [Test]
    public void Event_SetSizeLimit_NegativeValue() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.SetSizeLimit(-1);
        Assert.AreEqual(0, evt.GetStorageSize());
    }


    /**
     *  HasConfig()
     */
    [Test]
    public void Event_HasConfig() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.IsFalse(evt.HasConfig());
    }


    /**
     *  RemoveFromCache(ICollection<object> items)
     */
    [Test]
    public void Event_RemoveFromCache_NullItems() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.RemoveFromCache(null);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_RemoveFromCache_EmptyItems() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.RemoveFromCache(new List<object>());
        Assert.AreEqual(0, count);
    }


    /**
     *  GetSentEvents()
     */
    [Test]
    public void Event_GetSentEvents() {
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        Assert.AreEqual(0, evt.GetSentEvents().Count);
    }


    /**
     *  OnSecond(long timestamp)
     */
    [Test]
    public void Event_OnSecond_ZeroTimestamp() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.OnSecond(0);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_OnSecond_NegativeTimestamp() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.OnSecond(-1);
        Assert.AreEqual(0, count);
    }

    [Test]
    public void Event_OnSecond_SimpleTimestamp() {
        int count = 0;
        RUMEvent evt = new RUMEvent(100, false, () => {}, () => {});
        evt.OnSecond(1568105641);
        Assert.AreEqual(0, count);
    }
}