# fpnn rum sdk websocket #

#### 关于三方包依赖 ####
* [fpnn.unitypackage](https://github.com/highras/fpnn-sdk-unity)
* [Json&MsgPack.unitypackage](https://github.com/deniszykov/msgpack-unity3d)

#### 关于线程 ####

* 一个线程池, 接口`ThreadPool.IThreadPool`
    * 默认实现`System.Threading.ThreadPool.QueueUserWorkItem`
    * 如需自己管理线程，实现该接口并注册线程池`ThreadPool.Instance.SetPool(IThreadPool value)`

* 不要阻塞事件触发和回调, 否则线程池将被耗尽

#### 一个例子 ####
```c#
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using com.rum;

RUMClient client = new RUMClient(
    41000013,
    "c23e9d90-bada-440d-8316-44790f615ec1",
    null,
    null,
    true
);

client.GetEvent().AddListener("error", (evd) => {

    Debug.Log("error: " + evd.GetException().Message);
});

client.GetEvent().AddListener("close", (evd) => {

    Debug.Log("closed!");
});

client.GetEvent().AddListener("ready", (evd) => {

    Debug.Log("ready!");
    client.CustomEvent("MY_EVENT", new Dictionary<string, object>());
});

client.Connect("52.83.220.166:13609", false);

// destory
// client.Destory();
// client = null;
```


#### Events ####
* `event`:
    * `ready`: 初始化完成 

    * `error`: 异常
        * `exception`: **(Exception)**

    * `close`: 连接关闭

#### API ####
* `Constructor(int pid, string token, string uid, string appv, bool debug)`: 构造RUMClient
    * `pid`: **(int)** 应用ID, RUM项目控制台获取
    * `token`: **(string)** 应用Token, RUM项目控制台获取
    * `uid`: **(string)** 应用开放用户ID 
    * `appv`: **(string)** 应用版本号
    * `debug`: **(bool)** 是否开启调试日志

* `Destroy()`: 断开链接并销毁 

* `Connect(string endpoint, bool clearStorage)`: 连接服务器
    * `endpoint`: **(string)** RUMAgent接入地址, 由RUM项目控制台获取
    * `clearStorage`: **(bool)** 是否格式化本地事件缓存

* `GetSession`: **(long)** 会话 ID, 设备唯一, 可用于服务端事件关联

* `GetRumId`: **(string)** RUM ID, 唯一, 可用于服务端事件关联

* `SetUid(string value)`: 设置用户ID
    * `value`: **(string)** 用户ID


* `CustomEvent(string ev, IDictionary<string, object> attrs)`: 上报自定义事件 
    * `ev`: **(string)** 自定义事件名称
    * `attrs`: **(IDictionary[string,object])** 自定义事件内容
