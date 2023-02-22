using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Mathematics;

[Serializable]
public struct CppRect
{
    public ushort x;
    public ushort y;
    public ushort w;
    public ushort h;

    public CppRect(ushort xArg, ushort yArg, ushort wArg, ushort hArg)
    {
        x = xArg; y = yArg; w = wArg; h = hArg;
    }

    public override string ToString()
    {
        return $"{x} {y} {w} {h}";
    }
}

class PluginLoader : MonoBehaviour
{

#if UNITY_EDITOR

    // Handle to the C++ DLL
    public IntPtr libraryHandle;

#endif

#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX

    [DllImport("__Internal")]
    public static extern IntPtr dlopen(
        string path,
        int flag);

    [DllImport("__Internal")]
    public static extern IntPtr dlsym(
        IntPtr handle,
        string symbolName);

    [DllImport("__Internal")]
    public static extern int dlclose(
        IntPtr handle);

    public static IntPtr OpenLibrary(string path)
    {
        IntPtr handle = dlopen(path, 0);
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Couldn't open native library: " + path);
        }
        return handle;
    }

    public static void CloseLibrary(IntPtr libraryHandle)
    {
        dlclose(libraryHandle);
    }

    public static T GetDelegate<T>(
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
	public static extern IntPtr LoadLibrary(
		string path);
 
	[DllImport("kernel32")]
	public static extern IntPtr GetProcAddress(
		IntPtr libraryHandle,
		string symbolName);
 
	[DllImport("kernel32")]
	public static extern bool FreeLibrary(
		IntPtr libraryHandle);
 
	public static IntPtr OpenLibrary(string path)
	{
		IntPtr handle = LoadLibrary(path);
		if (handle == IntPtr.Zero)
		{
			throw new Exception("Couldn't open native library: " + path);
		}
		return handle;
	}
 
	public static void CloseLibrary(IntPtr libraryHandle)
	{
		FreeLibrary(libraryHandle);
	}
 
	public static T GetDelegate<T>(
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

    delegate int InitTestDelegate(int testNumber); // returns passCount
    delegate int InitPassDelegate(int passIndex); // returns stepsCount;
    delegate int StepDelegate(); // returns texturesCount;
                                 // The library's containers are flushed after each step

    delegate int3 GetRectsCountDelegate(int textureId);
    delegate CppRect GetPlacedGlyphDelegate(int textureId, int glyphIndex);
    delegate CppRect GetFreeShelfRectDelegate(int textureId, int freeShelfIndex);
    delegate CppRect GetFreeSlotRectDelegate(int textureId, int freeSlotIndex);

#if UNITY_EDITOR_OSX
    const string LIB_PATH = "/Users/Abai/Desktop/Evolve/GlyphAtlas/cmake-build-debug/libGlyphAtlasLib.dylib";
#elif UNITY_EDITOR_LINUX
	const string LIB_PATH = "";
#elif UNITY_EDITOR_WIN
	const string LIB_PATH = "";
#endif

    public CppRectRenderer texturePrefab;
    public CppRectRenderer glyphPrefab;
    public CppRectRenderer shelfPrefab;
    public CppRectRenderer slotPrefab;

	InitTestDelegate InitTest;
	InitPassDelegate InitPass;	
	StepDelegate Step;	
	GetRectsCountDelegate GetRectsCount;	
	GetPlacedGlyphDelegate GetPlacedGlyph;	
	GetFreeShelfRectDelegate GetFreeShelfRect;	
	GetFreeSlotRectDelegate GetFreeSlotRect;	

#if UNITY_EDITOR
    void Awake()
    {

        // Open native library
        libraryHandle = OpenLibrary(LIB_PATH);
        InitTest = GetDelegate<InitTestDelegate>(libraryHandle, "InitTest");
        InitPass = GetDelegate<InitPassDelegate>(libraryHandle, "InitPass");
        Step = GetDelegate<StepDelegate>(libraryHandle, "Step");

        GetRectsCount = GetDelegate<GetRectsCountDelegate>(libraryHandle, "GetRectsCount");
        GetPlacedGlyph = GetDelegate<GetPlacedGlyphDelegate>(libraryHandle, "GetPlacedGlyph");
        GetFreeShelfRect = GetDelegate<GetFreeShelfRectDelegate>(libraryHandle, "GetFreeShelfRect");
        GetFreeSlotRect = GetDelegate<GetFreeSlotRectDelegate>(libraryHandle, "GetFreeSlotRect");

        StartCoroutine(AtlasSequence(0));
    }
#endif

    IEnumerator AtlasSequence(int testNumber)
    {
        const float rectStepTime = 0.2f;
        const float stepTime = 1;


        int passCount = InitTest(0);
        Debug.Log(passCount);
        for (int p = 0; p < passCount; p++)
        {
            int stepsCount = InitPass(p);
            for (int s = 0; s < stepsCount; s++)
            {
                List<CppRectRenderer> stepRects = new();
                int texturesCount = Step();
                for (int t = 0; t < texturesCount; t++)
                {
                    CppRect textureRect = new CppRect((ushort)(t * (1024 + 100)), 0, 1024, 1024);
                    var texture = Instantiate(texturePrefab);
                    stepRects.Add(texture);
                    texture.Render(textureRect, 0, 0);
                    int3 counts = GetRectsCount(t);
                    Debug.Log(counts);
                    for (int g = 0; g < counts.x; g++)
                    {
                        CppRect r = GetPlacedGlyph(t, g);
                        var rend = Instantiate(glyphPrefab);
                        rend.Render(r, 10, 3);
                        stepRects.Add(rend);
                        yield return new WaitForSeconds(rectStepTime);
                    }
                    for (int shelf = 0; shelf < counts.y; shelf++)
                    {
                        CppRect r = GetFreeShelfRect(t, shelf);
						var rend = Instantiate(shelfPrefab);
                        rend.Render(r, 10, 1);
                        stepRects.Add(rend);
                        yield return new WaitForSeconds(rectStepTime);
                    }
                    for (int slot = 0; slot < counts.z; slot++)
                    {
						CppRect r = GetFreeSlotRect(t, slot);
						var rend = Instantiate(slotPrefab);
                        rend.Render(r, 10, 2);
                        stepRects.Add(rend);
                        yield return new WaitForSeconds(rectStepTime);
                    }
                    yield return new WaitForSeconds(stepTime);
                    foreach (var rect in stepRects)
                    {
                        Destroy(rect.gameObject);
                    }
                    stepRects.Clear();
                }
            }
            yield break;
        }
	}

    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        CloseLibrary(libraryHandle);
        libraryHandle = IntPtr.Zero;
#endif
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
