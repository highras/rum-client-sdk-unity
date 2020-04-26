# RUM Client Unity SDK

[TOC]

## Depends

* [msgpack-csharp](https://github.com/highras/msgpack-csharp)

* [fpnn-sdk-unity](https://github.com/highras/fpnn-sdk-unity)

### Compatibility Version:

C# .Net Standard 2.0

### Capability in Funture

Encryption Capability, depending on FPNN C# SDK.

## Usage

### Using package

	using com.fpnn.rum;

### Init

**Init MUST in the main thread.**

#### FPNN SDK Init (REQUIRED)

	using com.fpnn;
	ClientEngine.Init();
	//-- or
	ClientEngine.Init(Config config);

#### RUM SDK Init (REQUIRED)

	using com.fpnn.rum;
	RUM.Init(string endpoint, int pid, string secretKey, string appVersion, string uid = null, com.fpnn.common.ErrorRecorder errorRecorder = null);
	//-- or
	RUM.Init(RUMConfig config);

### SDK Using

Do nothing.

All are automatically.

About the manual triggered events, please refer: [API docs](API.md)

### Notice

* Although the SDK supports the events before SDK inited, but using events before SDK inited is not recommended.

* Initialize the SDK as early as possible. Events can be persisted to disk or sent to servers only when the SDK initialization done.

### SDK Version

	Unity `Debug.Log("com.fpnn.rum.RUMConfig.SDKVersion");`

## API docs

Please refer: [API docs](API.md)


## Directory structure

* **\<rum-client-sdk-unity\>/Assets/Plugins/fpnn**

	Codes of FPNN SDK.

* **\<rum-client-sdk-unity\>/Assets/Plugins/rum**

	Codes of RUM SDK.

* **\<rum-client-sdk-unity\>/Assets/**

	* Main.cs:

		Entery of all examples.

	* ErrorRecorder.cs:

		Demo implementation of com.fpnn.common.ErrorRecorder for RUM examples.

	* RUMDemo.cs:

		RUM example for using.
