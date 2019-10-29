using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;

using GameDevWare.Serialization;
using com.fpnn;
using com.rum;

public class Integration_RUMEvent {

    [SetUp]
    public void SetUp() {
        RUMRegistration.Register(null);
    }

    [TearDown]
    public void TearDown() {}

    [UnityTest]
    public IEnumerator Event_Init_NullSendQuest() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            openEventCount++;
            evt.IsFirst();
        };
        evt = new RUMEvent(100, false, null, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(0, sendQuestCount);
        Assert.AreEqual(1, openEventCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_NullOpenEvent() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            openEventCount++;
            evt.IsFirst();
        };
        evt = new RUMEvent(101, false, sendQuest, null);
        evt.Init(false, true, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, sendQuestCount);
        Assert.AreEqual(0, openEventCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_NullDumpDelegate() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            openEventCount++;
            evt.IsFirst();
        };
        evt = new RUMEvent(101, false, sendQuest, null);
        evt.Init(false, true, null);
        yield return new WaitForSeconds(2.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, sendQuestCount);
        Assert.AreEqual(0, openEventCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_DumpDelegateNull() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            openEventCount++;
            evt.IsFirst();
        };
        evt = new RUMEvent(101, false, sendQuest, null);
        evt.Init(false, true, () => {
            return null;
        });
        yield return new WaitForSeconds(2.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, sendQuestCount);
        Assert.AreEqual(0, openEventCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_Write() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        string rum_id = null;
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            evt.IsFirst();
            rum_id = evt.GetRumId();
            openEventCount++;
        };
        evt = new RUMEvent(102, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        int count = evt.GetSentEvents().Count;
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, sendQuestCount);
        Assert.AreEqual(1, openEventCount);
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual(0, count);
        Assert.IsNotNull(rum_id);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEmptyDict() {
        int sendQuestCount = 0;
        int openEventCount = 0;
        IDictionary<string, object> ev_dict = new Dictionary<string, object>();
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {
            if (sendQuestCount == 0) {
                sendQuestCount++;
            }
        };
        Action openEvent = () => {
            openEventCount++;
            evt.IsFirst();
        };
        evt = new RUMEvent(103, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, sendQuestCount);
        Assert.AreEqual(1, openEventCount);
        Assert.AreEqual(0, list.Count);
    }

    // [UnityTest]
    // public IEnumerator Event_Init_WriteEvents() {
    //     IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
    //         { "ev", "test" },
    //         { "eid", 1568109465001 },
    //         { "custom_debug", "test text" }
    //     };
    //     ICollection<object> items = new List<object>();
    //     items.Add(ev_dict);
    //     RUMEvent evt = null;
    //     List<object> list = null;
    //     Action sendQuest = () => {};
    //     Action openEvent = () => {
    //         evt.IsFirst();
    //     };
    //     evt = new RUMEvent(104, false, sendQuest, openEvent);
    //     evt.Init(false, true, () => {
    //         return "";
    //     });
    //     yield return new WaitForSeconds(1.0f);
    //     evt.WriteEvents(items);
    //     yield return new WaitForSeconds(2.0f);
    //     list = evt.GetSentEvents();
    //     evt.Destroy();
    //     yield return new WaitForSeconds(1.0f);
    //     Assert.AreEqual(1, list.Count);
    // }

    // [UnityTest]
    // public IEnumerator Event_Init_WriteEventsEmptyDict() {
    //     ICollection<object> items = new List<object>();
    //     items.Add(new Dictionary<string, object>());
    //     RUMEvent evt = null;
    //     List<object> list = null;
    //     Action sendQuest = () => {};
    //     Action openEvent = () => {
    //         evt.IsFirst();
    //     };
    //     evt = new RUMEvent(105, false, sendQuest, openEvent);
    //     evt.Init(false, true, () => {
    //         return "";
    //     });
    //     yield return new WaitForSeconds(1.0f);
    //     evt.WriteEvents(items);
    //     yield return new WaitForSeconds(2.0f);
    //     list = evt.GetSentEvents();
    //     evt.Destroy();
    //     yield return new WaitForSeconds(1.0f);
    //     Assert.AreEqual(0, list.Count);
    // }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_GetSentEvent_RemoveFromCache() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt_1 = null;
        List<object> list_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {
            evt_1.IsFirst();
        };
        evt_1 = new RUMEvent(106, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list_1 = evt_1.GetSentEvents();
        Assert.AreEqual(1, list_1.Count);
        evt_1.RemoveFromCache(list_1, false);
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        List<object> list_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {
            evt_2.IsFirst();
        };
        evt_2 = new RUMEvent(106, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        list_2 = evt_2.GetSentEvents();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(0, list_2.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_GetSentEvent_RemoveFromCache_Unshift() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt_1 = null;
        List<object> list_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {
            evt_1.IsFirst();
        };
        evt_1 = new RUMEvent(133, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list_1 = evt_1.GetSentEvents();
        Assert.AreEqual(1, list_1.Count);
        evt_1.RemoveFromCache(list_1, true);
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        List<object> list_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {
            evt_2.IsFirst();
        };
        evt_2 = new RUMEvent(133, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        list_2 = evt_2.GetSentEvents();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, list_2.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_GetSentEvent_Init() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt_1 = null;
        List<object> list_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {
            evt_1.IsFirst();
        };
        evt_1 = new RUMEvent(107, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list_1 = evt_1.GetSentEvents();
        Assert.AreEqual(1, list_1.Count);
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        List<object> list_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {
            evt_2.IsFirst();
        };
        evt_2 = new RUMEvent(107, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        list_2 = evt_2.GetSentEvents();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, list_2.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_UpdateConfig_GetSentEvent0() {
        IDictionary<string, object> ev_config = new Dictionary<string, object>() {
            { "1", new List<object>() {
                    "Custom_Test", "append", "bg", "crash", "fg", "nwswitch", "open"
                }
            },
            { "2", new List<object>() {
                    "error", "http", "warn"
                }
            },
            { "3", new List<object>() { "info" } }
        };
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(108, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.UpdateConfig(ev_config);
        Assert.IsTrue(evt.HasConfig());
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(0, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_UpdateConfig_GetSentEvent1() {
        IDictionary<string, object> ev_config = new Dictionary<string, object>() {
            { "1", new List<object>() {
                    "Custom_Test", "append", "bg", "crash", "fg", "nwswitch", "open"
                }
            },
            { "2", new List<object>() {
                    "error", "http", "warn"
                }
            },
            { "3", new List<object>() {
                    "info", "test"
                }
            }
        };
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(109, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.UpdateConfig(ev_config);
        Assert.IsTrue(evt.HasConfig());
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_SetTimestamp_GetTimestamp() {
        long ts = 0;
        long timestamp = 1568170648;
        RUMEvent evt = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(110, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        EventDelegate eventDelegate = (evd) => {
            evt.OnSecond(evd.GetTimestamp());
        };
        FPManager.Instance.AddSecond(eventDelegate);
        evt.SetTimestamp(timestamp);
        yield return new WaitForSeconds(1.0f);
        ts = evt.GetTimestamp();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);

        if (eventDelegate != null) {
            FPManager.Instance.RemoveSecond(eventDelegate);
        }

        Assert.AreEqual(timestamp + 1, ts);
    }

    [UnityTest]
    public IEnumerator Event_SetTimestamp_SetTimestamp_GetTimestamp() {
        long ts = 0;
        long timestamp = 1568170648;
        RUMEvent evt = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(111, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        EventDelegate eventDelegate = (evd) => {
            evt.OnSecond(evd.GetTimestamp());
        };
        FPManager.Instance.AddSecond(eventDelegate);
        evt.SetTimestamp(timestamp);
        evt.SetTimestamp(timestamp + 5);
        yield return new WaitForSeconds(1.0f);
        ts = evt.GetTimestamp();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);

        if (eventDelegate != null) {
            FPManager.Instance.RemoveSecond(eventDelegate);
        }

        Assert.AreEqual(timestamp + 5 + 1, ts);
    }

    [UnityTest]
    public IEnumerator Event_SetTimestamp_SetTimestampNegative_GetTimestamp() {
        long ts = 0;
        long timestamp = 1568170648;
        RUMEvent evt = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(112, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        EventDelegate eventDelegate = (evd) => {
            evt.OnSecond(evd.GetTimestamp());
        };
        FPManager.Instance.AddSecond(eventDelegate);
        evt.SetTimestamp(timestamp);
        evt.SetTimestamp(timestamp - 5);
        yield return new WaitForSeconds(3.0f);
        ts = evt.GetTimestamp();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);

        if (eventDelegate != null) {
            FPManager.Instance.RemoveSecond(eventDelegate);
        }

        Assert.AreEqual(timestamp, ts);
    }

    [UnityTest]
    public IEnumerator Event_Init_GetStorageSize() {
        int size = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {};
        Action openEvent = () => {};
        evt = new RUMEvent(113, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        size = evt.GetStorageSize();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("StorageSize: " + size);
        Assert.AreEqual(117, size);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSession_Write() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(114, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSession(1568180419002);
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        Assert.AreEqual(1, list.Count);
        IDictionary<string, object> item = (IDictionary<string, object>)list[0];
        Assert.IsTrue(item.ContainsKey("rid"));
        Assert.IsTrue(item.ContainsKey("sid"));
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1B_WriteEvents() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 10; i++) {
        //     items.Add(ev_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(115, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 10; i++) {
            evt.WriteEvent(ev_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_WriteEvents() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 100; i++) {
        //     items.Add(ev_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(116, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 100; i++) {
            evt.WriteEvent(ev_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(12, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit15KB_WriteEvents100() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 100; i++) {
        //     items.Add(ev_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(117, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(15 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 100; i++) {
            evt.WriteEvent(ev_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(100, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit15KB_WriteEvents() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 400; i++) {
        //     items.Add(ev_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(118, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(15 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 400; i++) {
            evt.WriteEvent(ev_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(5.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(177, list.Count);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit15KB_WriteEvents1000() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 1000; i++) {
        //     items.Add(ev_dict);
        // }

        int count = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            count += evt.GetSentEvents().Count;
        };
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(132, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(15 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 1000; i++) {
            evt.WriteEvent(ev_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(8.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1000, count);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit15KB_WriteEvent1000() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        int count = 0;
        RUMEvent evt = null;
        Action sendQuest = () => {
            count += evt.GetSentEvents().Count;
        };
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(119, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(15 * 1024);

        for (int i = 0; i < 1000; i++) {
            evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(8.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1000, count);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent8_GetStorageSize() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" },
        };

        for (int i = 0; i < 1000; i++) {
            ev_dict.Add("custom_debug_1111111111111111111111111111111111111_" + i, "test text 11111111111111111111111111111111111111111 " + i);
        }

        RUMEvent evt_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {};
        evt_1 = new RUMEvent(120, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.SetSizeLimit(15 * 1024);

        for (int i = 0; i < 8; i++) {
            evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(2.0f);
        int size_1 = evt_1.GetStorageSize();
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {};
        evt_2 = new RUMEvent(120, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        int size_2 = evt_2.GetStorageSize();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log(size_1 + "  " + size_2);
        Assert.AreEqual(size_1, size_2);
        Assert.AreEqual(903075, size_2);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent12_GetStorageSize() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" },
        };

        for (int i = 0; i < 1000; i++) {
            ev_dict.Add("custom_debug_1111111111111111111111111111111111111_" + i, "test text 11111111111111111111111111111111111111111 " + i);
        }

        RUMEvent evt_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {};
        evt_1 = new RUMEvent(121, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.SetSizeLimit(15 * 1024);

        for (int i = 0; i < 12; i++) {
            evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(3.0f);
        int size_1 = evt_1.GetStorageSize();
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {};
        evt_2 = new RUMEvent(121, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        int size_2 = evt_2.GetStorageSize();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log(size_1 + "  " + size_2);
        Assert.AreEqual(size_1, size_2);
        Assert.AreEqual(790206, size_2);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent12_ClearEvents_GetStorageSize() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" },
        };

        for (int i = 0; i < 1000; i++) {
            ev_dict.Add("custom_debug_1111111111111111111111111111111111111_" + i, "test text 11111111111111111111111111111111111111111 " + i);
        }

        RUMEvent evt_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {};
        evt_1 = new RUMEvent(122, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        evt_1.SetSizeLimit(15 * 1024);

        for (int i = 0; i < 12; i++) {
            evt_1.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(3.0f);
        int size_1 = evt_1.GetStorageSize();
        yield return new WaitForSeconds(1.0f);
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {};
        evt_2 = new RUMEvent(122, false, sendQuest_2, openEvent_2);
        evt_2.Init(false, false, () => {
            return "";
        });
        yield return new WaitForSeconds(3.0f);
        int size_2 = evt_2.GetStorageSize();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_3 = null;
        Action sendQuest_3 = () => {};
        Action openEvent_3 = () => {};
        evt_3 = new RUMEvent(122, false, sendQuest_3, openEvent_3);
        evt_3.Init(false, true, () => {
            return "";
        });
        yield return new WaitForSeconds(2.0f);
        int size_3 = evt_3.GetStorageSize();
        evt_3.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log(size_1 + "  " + size_2 + "  " + size_3);
        Assert.AreEqual(size_1, size_2);
        Assert.AreEqual(564468, size_3);
    }

    [UnityTest]
    public IEnumerator Event_Init_ClearEvents_WriteEvent12_WriteEvent6() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" },
        };

        for (int i = 0; i < 1000; i++) {
            ev_dict.Add("custom_debug_1111111111111111111111111111111111111_" + i, "test text 11111111111111111111111111111111111111111 " + i);
        }

        RUMEvent evt = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {};
        evt = new RUMEvent(123, false, sendQuest_1, openEvent_1);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(15 * 1024);
        yield return new WaitForSeconds(2.0f);

        for (int i = 0; i < 12; i++) {
            evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(2.0f);

        for (int i = 0; i < 6; i++) {
            evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        }

        yield return new WaitForSeconds(2.0f);
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Debug.Log("double check the path'123', mast be have two files");
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_WriteEvents100_WriteEvent1_GetSentEvents() {
        IDictionary<string, object> open_dict = new Dictionary<string, object>() {
            { "ev", "open" },
            { "eid", 1568109465001 }
        };
        IDictionary<string, object> test_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465002 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 100; i++) {
        //     items.Add(test_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(124, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 100; i++) {
            evt.WriteEvent(test_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        string ev_name = null;
        IDictionary<string, object> ev_dict = null;
        IDictionary<string, object> open_ev = null;

        for (int i = 0; i < list.Count; i++) {
            ev_dict = (IDictionary<string, object>)list[i];
            ev_name = Convert.ToString(ev_dict["ev"]);

            if (ev_name == "open") {
                open_ev = ev_dict;
                Debug.Log("count: " + list.Count + ", ev: 'open', json: " + Json.SerializeToString(open_ev));
            }
        }

        Assert.IsNotNull(open_ev);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_WriteEvents100_WriteEvent3_GetSentEvents() {
        IDictionary<string, object> open_dict = new Dictionary<string, object>() {
            { "ev", "open" },
            { "eid", 1568109465001 }
        };
        IDictionary<string, object> info_dict = new Dictionary<string, object>() {
            { "ev", "info" },
            { "eid", 1568109465002 }
        };
        IDictionary<string, object> test_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        IDictionary<string, object> apen_dict = new Dictionary<string, object>() {
            { "ev", "apen" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 50; i++) {
        //     items.Add(apen_dict);
        // }

        // for (int i = 0; i < 50; i++) {
        //     items.Add(test_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            // evt.IsFirst();
        };
        evt = new RUMEvent(125, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 50; i++) {
            evt.WriteEvent(apen_dict);
        }
        for (int i = 0; i < 50; i++) {
            evt.WriteEvent(test_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(info_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        int openCount = 0;
        int apenCount = 0;
        int infoCount = 0;
        int testCount = 0;
        string ev_name = null;
        IDictionary<string, object> ev_dict = null;

        for (int i = 0; i < list.Count; i++) {
            ev_dict = (IDictionary<string, object>)list[i];
            ev_name = Convert.ToString(ev_dict["ev"]);

            if (ev_name == "open") {
                openCount++;
            }

            if (ev_name == "apen") {
                apenCount++;
            }

            if (ev_name == "info") {
                infoCount++;
            }

            if (ev_name == "test") {
                testCount++;
            }
        }

        // Debug.Log("count: " + list.Count + ", openCount: " + openCount + ", infoCount: " + infoCount + ", apenCount: " + apenCount + ", testCount: " + testCount);
        Assert.AreEqual(2, openCount);
        Assert.AreEqual(0, infoCount);
        Assert.AreEqual(10, apenCount);
        Assert.AreEqual(0, testCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_WriteEvents6_WriteEvent3_GetSentEvents() {
        IDictionary<string, object> open_dict = new Dictionary<string, object>() {
            { "ev", "open" },
            { "eid", 1568109465001 }
        };
        IDictionary<string, object> info_dict = new Dictionary<string, object>() {
            { "ev", "info" },
            { "eid", 1568109465002 }
        };
        IDictionary<string, object> test_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        IDictionary<string, object> apen_dict = new Dictionary<string, object>() {
            { "ev", "apen" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 3; i++) {
        //     items.Add(apen_dict);
        // }

        // for (int i = 0; i < 3; i++) {
        //     items.Add(test_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            // evt.IsFirst();
        };
        evt = new RUMEvent(126, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 3; i++) {
            evt.WriteEvent(apen_dict);
        }
        for (int i = 0; i < 3; i++) {
            evt.WriteEvent(test_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(info_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        int openCount = 0;
        int apenCount = 0;
        int infoCount = 0;
        int testCount = 0;
        string ev_name = null;
        IDictionary<string, object> ev_dict = null;

        for (int i = 0; i < list.Count; i++) {
            ev_dict = (IDictionary<string, object>)list[i];
            ev_name = Convert.ToString(ev_dict["ev"]);

            if (ev_name == "open") {
                openCount++;
            }

            if (ev_name == "apen") {
                apenCount++;
            }

            if (ev_name == "info") {
                infoCount++;
            }

            if (ev_name == "test") {
                testCount++;
            }
        }

        // Debug.Log("count: " + list.Count + ", openCount: " + openCount + ", infoCount: " + infoCount + ", apenCount: " + apenCount + ", testCount: " + testCount);
        Assert.AreEqual(2, openCount);
        Assert.AreEqual(1, infoCount);
        Assert.AreEqual(3, apenCount);
        Assert.AreEqual(3, testCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_WriteEvents20_WriteEvent3_GetSentEvents() {
        IDictionary<string, object> open_dict = new Dictionary<string, object>() {
            { "ev", "open" },
            { "eid", 1568109465001 }
        };
        IDictionary<string, object> info_dict = new Dictionary<string, object>() {
            { "ev", "info" },
            { "eid", 1568109465002 }
        };
        IDictionary<string, object> test_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        IDictionary<string, object> apen_dict = new Dictionary<string, object>() {
            { "ev", "apen" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 10; i++) {
        //     items.Add(apen_dict);
        // }

        // for (int i = 0; i < 10; i++) {
        //     items.Add(test_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            // evt.IsFirst();
        };
        evt = new RUMEvent(127, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1 * 1024);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 10; i++) {
            evt.WriteEvent(apen_dict);
        }
        for (int i = 0; i < 10; i++) {
            evt.WriteEvent(test_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(info_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        int openCount = 0;
        int apenCount = 0;
        int infoCount = 0;
        int testCount = 0;
        string ev_name = null;
        IDictionary<string, object> ev_dict = null;

        for (int i = 0; i < list.Count; i++) {
            ev_dict = (IDictionary<string, object>)list[i];
            ev_name = Convert.ToString(ev_dict["ev"]);

            if (ev_name == "open") {
                openCount++;
            }

            if (ev_name == "apen") {
                apenCount++;
            }

            if (ev_name == "info") {
                infoCount++;
            }

            if (ev_name == "test") {
                testCount++;
            }
        }

        // Debug.Log("count: " + list.Count + ", openCount: " + openCount + ", infoCount: " + infoCount + ", apenCount: " + apenCount + ", testCount: " + testCount);
        Assert.AreEqual(2, openCount);
        Assert.AreEqual(0, infoCount);
        Assert.AreEqual(10, apenCount);
        Assert.AreEqual(0, testCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_SetSizeLimit1KB_UpdateConfig_WriteEvents100_WriteEvent3_GetSentEvents() {
        IDictionary<string, object> ev_config = new Dictionary<string, object>() {
            { "1", new List<object>() {
                    "apen", "append", "bg", "crash", "fg", "nwswitch", "open"
                }
            },
            { "2", new List<object>() {
                    "error", "http", "warn", "test"
                }
            },
            { "3", new List<object>() { "info" } }
        };
        IDictionary<string, object> open_dict = new Dictionary<string, object>() {
            { "ev", "open" },
            { "eid", 1568109465001 }
        };
        IDictionary<string, object> info_dict = new Dictionary<string, object>() {
            { "ev", "info" },
            { "eid", 1568109465002 }
        };
        IDictionary<string, object> test_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        IDictionary<string, object> apen_dict = new Dictionary<string, object>() {
            { "ev", "apen" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" }
        };
        // ICollection<object> items = new List<object>();

        // for (int i = 0; i < 50; i++) {
        //     items.Add(test_dict);
        // }

        // for (int i = 0; i < 50; i++) {
        //     items.Add(apen_dict);
        // }

        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            // evt.IsFirst();
        };
        evt = new RUMEvent(128, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.SetSizeLimit(1 * 1024);
        evt.UpdateConfig(ev_config);
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 50; i++) {
            evt.WriteEvent(test_dict);
        }
        for (int i = 0; i < 50; i++) {
            evt.WriteEvent(apen_dict);
        }
        // evt.WriteEvents(items);
        yield return new WaitForSeconds(2.0f);
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        evt.WriteEvent(new Dictionary<string, object>(info_dict));
        evt.WriteEvent(new Dictionary<string, object>(open_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        // Debug.Log("count: " + list.Count +  ", json: " + Json.SerializeToString(list));
        int openCount = 0;
        int apenCount = 0;
        int infoCount = 0;
        int testCount = 0;
        string ev_name = null;
        IDictionary<string, object> ev_dict = null;

        for (int i = 0; i < list.Count; i++) {
            ev_dict = (IDictionary<string, object>)list[i];
            ev_name = Convert.ToString(ev_dict["ev"]);

            if (ev_name == "open") {
                openCount++;
            }

            if (ev_name == "apen") {
                apenCount++;
            }

            if (ev_name == "info") {
                infoCount++;
            }

            if (ev_name == "test") {
                testCount++;
            }
        }

        // Debug.Log("count: " + list.Count + ", openCount: " + openCount + ", infoCount: " + infoCount + ", apenCount: " + apenCount + ", testCount: " + testCount);
        Assert.AreEqual(2, openCount);
        Assert.AreEqual(0, infoCount);
        Assert.AreEqual(10, apenCount);
        Assert.AreEqual(0, testCount);
    }

    [UnityTest]
    public IEnumerator Event_Init_WriteEvent_EmptyKey() {
        IDictionary<string, object> ev_dict = new Dictionary<string, object>() {
            { "ev", "test" },
            { "eid", 1568109465001 },
            { "custom_debug", "test text" },
            { "empty", "" },
            { "null", null }
        };
        RUMEvent evt = null;
        List<object> list = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(129, false, sendQuest, openEvent);
        evt.Init(false, true, () => {
            return "";
        });
        evt.WriteEvent(new Dictionary<string, object>(ev_dict));
        yield return new WaitForSeconds(2.0f);
        list = evt.GetSentEvents();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.AreEqual(1, list.Count);
        IDictionary<string, object> item = (IDictionary<string, object>)list[0];
        Assert.IsFalse(item.ContainsKey("empty"));
        Assert.IsFalse(item.ContainsKey("null"));
    }

    [UnityTest]
    public IEnumerator Event_ClearRumId_Init() {
        string rum_id = null;
        RUMEvent evt_1 = null;
        Action sendQuest_1 = () => {};
        Action openEvent_1 = () => {
            evt_1.IsFirst();
        };
        evt_1 = new RUMEvent(130, false, sendQuest_1, openEvent_1);
        evt_1.Init(false, true, () => {
            return "";
        });
        yield return new WaitForSeconds(0.5f);
        rum_id = evt_1.GetRumId();
        evt_1.Destroy();
        yield return new WaitForSeconds(1.0f);
        RUMEvent evt_2 = null;
        Action sendQuest_2 = () => {};
        Action openEvent_2 = () => {
            evt_2.IsFirst();
        };
        evt_2 = new RUMEvent(130, false, sendQuest_2, openEvent_2);
        evt_2.Init(true, true, () => {
            return "";
        });
        yield return new WaitForSeconds(1.0f);
        string new_id = evt_2.GetRumId();
        evt_2.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.IsNotNull(rum_id);
        Assert.IsNotNull(new_id);
        Assert.AreNotEqual(rum_id, new_id);
    }

    [UnityTest]
    public IEnumerator Event_ClearRumId_Init_IsFirst() {
        RUMEvent evt = null;
        Action sendQuest = () => {};
        Action openEvent = () => {
            evt.IsFirst();
        };
        evt = new RUMEvent(131, false, sendQuest, openEvent);
        evt.Init(true, true, () => {
            return "";
        });
        yield return new WaitForSeconds(1.0f);
        string rum_id = evt.GetRumId();
        bool first = evt.IsFirst();
        evt.Destroy();
        yield return new WaitForSeconds(1.0f);
        Assert.IsNotNull(rum_id);
        Assert.IsFalse(first);
    }
}