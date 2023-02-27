using System;
using System.Runtime.InteropServices;

using UnityEngine;

#if UNITY_EDITOR
abstract class PluginLoader : MonoBehaviour
{
    // Handle to the C++ DLL
    protected IntPtr libraryHandle;

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX

    [DllImport("__Internal")]
    private static extern IntPtr dlopen(
        string path,
        int flag);

    [DllImport("__Internal")]
    private static extern IntPtr dlsym(
        IntPtr handle,
        string symbolName);

    [DllImport("__Internal")]
    private static extern int dlclose(
        IntPtr handle);

    protected static IntPtr OpenLibrary(string path)
    {
        IntPtr handle = dlopen(path, 0);
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Couldn't open native library: " + path);
        }
        return handle;
    }

    protected static void CloseLibrary(IntPtr libraryHandle)
    {
        dlclose(libraryHandle);
    }

    protected static T GetDelegate<T>(
        IntPtr libraryHandle,
        string functionName) where T : class
    {
        IntPtr symbol = dlsym(libraryHandle, functionName);
        if (symbol == IntPtr.Zero)
        {
            throw new Exception("Couldn't get function: " + functionName);
        }
        return Marshal.GetDelegateForFunctionPointer(
            symbol,
            typeof(T)) as T;
    }


#elif UNITY_EDITOR_WIN
 
	[DllImport("kernel32")]
	private static extern IntPtr LoadLibrary(
		string path);
 
	[DllImport("kernel32")]
	private static extern IntPtr GetProcAddress(
		IntPtr libraryHandle,
		string symbolName);
 
	[DllImport("kernel32")]
	private static extern bool FreeLibrary(
		IntPtr libraryHandle);
 
	private static IntPtr OpenLibrary(string path)
	{
		IntPtr handle = LoadLibrary(path);
		if (handle == IntPtr.Zero)
		{
			throw new Exception("Couldn't open native library: " + path);
		}
		return handle;
	}
 
	private static void CloseLibrary(IntPtr libraryHandle)
	{
		FreeLibrary(libraryHandle);
	}
 
	private static T GetDelegate<T>(
		IntPtr libraryHandle,
		string functionName) where T : class
	{
		IntPtr symbol = GetProcAddress(libraryHandle, functionName);
		if (symbol == IntPtr.Zero)
		{
			throw new Exception("Couldn't get function: " + functionName);
		}
		return Marshal.GetDelegateForFunctionPointer(
			symbol,
			typeof(T)) as T;
	}
 
#endif

    public string libPath = "/Users/Abai/Desktop/Evolve/GlyphAtlas/cmake-build-debug/libGlyphAtlasLib.dylib";

    protected virtual void Awake()
    {
        // Open native library
        libraryHandle = OpenLibrary(libPath);
    }

    void OnApplicationQuit()
    {
        CloseLibrary(libraryHandle);
        libraryHandle = IntPtr.Zero;
    }

    ////////////////////////////////////////////////////////////////
    // C# functions for C++ to call
    ////////////////////////////////////////////////////////////////

    // static int GameObjectNew()
    // {
    // 	GameObject go = new GameObject();
    // 	return ObjectStore.Store(go);
    // }

    // static int GameObjectGetTransform(int thisHandle)
    // {
    // 	GameObject thiz = (GameObject)ObjectStore.Get(thisHandle);
    // 	Transform transform = thiz.transform;
    // 	return ObjectStore.Store(transform);
    // }

    // static void TransformSetPosition(int thisHandle, Vector3 position)
    // {
    // 	Transform thiz = (Transform)ObjectStore.Get(thisHandle);
    // 	thiz.position = position;
    // }
}
#endif