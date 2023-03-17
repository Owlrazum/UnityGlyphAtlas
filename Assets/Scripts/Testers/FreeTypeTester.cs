using System;
using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FreeTypeTest
{ 
    class FreeTypeTester : PluginLoader
    {
        unsafe delegate void RenderCharTestDelegate(GlyphData* dataPtr, char character, int size);
        RenderCharTestDelegate RenderCharTest;

        public int size;
        public Renderer textureRenderer;

        protected override void Awake()
        {
            base.Awake();

            RenderCharTest = GetDelegate<RenderCharTestDelegate>(libraryHandle, "RenderCharTest");

            GlyphData[] dataManaged = new GlyphData[1];
            unsafe
            {
                fixed (GlyphData* dataPtr = dataManaged)
                {
                    RenderCharTest(dataPtr, 'Z', size);
                    GlyphData data = *dataPtr;

                    Texture2D texture = new Texture2D(data.width, data.rowCount, TextureFormat.R8, false);
                    texture.filterMode = FilterMode.Point;

                    NativeArray<byte> textureData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data.bitmap, data.width * data.rowCount, Allocator.None);
                    NativeArray<byte> temp = new NativeArray<byte>(0, Allocator.Temp);
                    AtomicSafetyHandle handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(temp);
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref textureData, handle);

                    texture.SetPixelData(textureData, 0);

                    texture.Apply();
                    File.WriteAllBytes($"{Application.dataPath}/Generated/temp.png", texture.EncodeToPNG());
                    textureRenderer.material.mainTexture = texture;
                    textureRenderer.transform.localScale = new Vector3(data.width, 1, data.rowCount);
                }
            }
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
}