using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

class FreeTypeTester : PluginLoader
{
    unsafe delegate void RenderGlyphTestDelegate(GlyphData* dataPtr, char character);
    RenderGlyphTestDelegate RenderGlyphTest;

    public Renderer textureRenderer;

    protected override void Awake()
    {
        base.Awake();

        RenderGlyphTest = GetDelegate<RenderGlyphTestDelegate>(libraryHandle, "RenderGlyphTest");

        int2 dims = new int2(300, 300);

        Texture2D texture = new Texture2D(dims.x, dims.y, TextureFormat.R8, false);
        texture.filterMode = FilterMode.Point;

        NativeArray<byte> textureData = new NativeArray<byte>(dims.x * dims.y, Allocator.Temp);
        NativeArray<byte> temp = new NativeArray<byte>(0, Allocator.Temp);
        AtomicSafetyHandle handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(temp);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref textureData, handle);

        const int gap = 10;
        int2 pos = new int2(dims.x - 1, -1);
        int rowMaxHeight = 0;
        int baseLine = dims.y - 1 - gap;
        string text = "Hello, freetype world!";

        GlyphData[] dataManaged = new GlyphData[1];
        unsafe
        {
            fixed (GlyphData* dataPtr = dataManaged)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    RenderGlyphTest(dataPtr, text[i]);
                    GlyphData data = *dataPtr;

                    if (pos.x - data.pitch - gap < 0)
                    {
                        pos.x = dims.x - 1;
                        baseLine -= rowMaxHeight + gap;
                        rowMaxHeight = 0;
                    }
                    pos.x -= data.pitch + gap;
                    pos.y = baseLine - data.rowCount;

                    rowMaxHeight = math.max(data.rowCount, rowMaxHeight);

                    NativeArray<byte> glyphData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data.bitmap, data.width * data.rowCount, Allocator.None);
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref glyphData, handle);
                    for (int y = 0; y < data.rowCount; y++)
                    {
                        for (int x = 0; x < data.pitch / 2; x++)
                        {
                            byte tempo = glyphData[y * data.pitch + x];
                            glyphData[y * data.pitch + x] = glyphData[y * data.pitch + data.width - x - 1];
                            glyphData[y * data.pitch + data.width - x - 1] = tempo;
                        }
                        NativeArray<byte>.Copy(glyphData, y * data.pitch, textureData, pos.y * dims.x + pos.x + y * dims.x, data.pitch);
                    }
                }


                texture.SetPixelData(textureData, 0);

                // replace with SetPixelData.
                // data[index + 1], data[index + 2]
            }
        }

        texture.Apply();
        File.WriteAllBytes($"{Application.dataPath}/temp.png", texture.EncodeToPNG());
        textureRenderer.material.mainTexture = texture;
    }
}

[StructLayout(LayoutKind.Sequential)]
unsafe struct GlyphData
{
    public byte* bitmap;

    public int width;
    public int rowCount;

    public int pitch;
}