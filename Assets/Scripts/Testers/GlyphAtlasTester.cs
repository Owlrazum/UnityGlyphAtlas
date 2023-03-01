using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace GlyphAtlasTest
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct AtlasTextures
    {
        public byte** textures;
        public int* widths;
        public int* heights;
        public int count;
    };

    class GlyphAtlasTester : PluginLoader
    {
        delegate void RenderAtlasTexturesDelegate(IntPtr atlasTextures, int testNumber);
        delegate void FreeAtlasTexturesDelegate(IntPtr atlasTextures);

        public TextureViewer textureViewerPrefab;

        RenderAtlasTexturesDelegate RenderAtlasTextures;
        FreeAtlasTexturesDelegate FreeAtlasTextures;

        protected override void Awake()
        {
            base.Awake();

            RenderAtlasTextures = GetDelegate<RenderAtlasTexturesDelegate>(libraryHandle, "RenderAtlasTextures");
            FreeAtlasTextures = GetDelegate<FreeAtlasTexturesDelegate>(libraryHandle, "FreeAtlasTextures");

            int2 dims = new int2(512, 512);

            AtlasTextures[] dataManaged = new AtlasTextures[1];
            unsafe
            {
                fixed (AtlasTextures* texturesPtr = dataManaged)
                {
                    IntPtr ptr = new IntPtr(texturesPtr);
                    RenderAtlasTextures(ptr, 0);
                    AtlasTextures textures = *texturesPtr;
                    for (int i = 0; i < textures.count; i++)
                    {
                        CreateTexture(dims, textures.textures[i], $"texture_{i}", new float2(i, 0));
                    }
                    FreeAtlasTextures(ptr);
                }
            }
        }

        unsafe void CreateTexture(int2 dims, byte* data, string name, float2 offset)
        {
            Texture2D texture = new Texture2D(dims.x, dims.y, TextureFormat.R8, false);
            texture.filterMode = FilterMode.Point;

            NativeArray<byte> textureData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, dims.x * dims.y, Allocator.None);
            NativeArray<byte> temp = new NativeArray<byte>(0, Allocator.Temp);
            AtomicSafetyHandle handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(temp);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref textureData, handle);

            texture.SetPixelData(textureData, 0);
            texture.Apply();

            File.WriteAllBytes($"{Application.dataPath}/{name}.png", texture.EncodeToPNG());

            var textureViewer = Instantiate(textureViewerPrefab);
            textureViewer.SetTexture(texture);
            textureViewer.transform.position = new float3(offset.x, 0, offset.y);
        }
    }
}