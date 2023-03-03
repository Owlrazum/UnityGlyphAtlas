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
        public int count;
    };

    unsafe class GlyphAtlasTester : PluginLoader
    {
        delegate void RenderAtlasTexturesDelegate(AtlasTextures* atlasTextures, int testNumber);
        delegate void FreeAtlasTexturesDelegate(AtlasTextures* atlasTextures);

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
            fixed (AtlasTextures* texturesPtr = dataManaged)
            {
                RenderAtlasTextures(texturesPtr, 0);
                AtlasTextures textures = *texturesPtr;
                for (int i = 0; i < textures.count; i++)
                {
                    CreateTexture(dims, texturesPtr->textures[i], $"texture_{i}", new float2(i * 15, 0));
                }
                FreeAtlasTextures(texturesPtr);
            }
        }

        unsafe void CreateTexture(int2 dims, byte* data, string name, float2 offset)
        {
            Texture2D texture = new Texture2D(dims.x, dims.y, TextureFormat.R8, false);
            texture.filterMode = FilterMode.Point;

            NativeArray<byte> textureData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(
                data, dims.x * dims.y, Allocator.None);
            NativeArray<byte> temp = new NativeArray<byte>(0, Allocator.Temp);
            AtomicSafetyHandle handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(temp);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref textureData, handle);

            texture.SetPixelData(textureData, 0);
            texture.Apply();

            File.WriteAllBytes($"{Application.dataPath}/Generated/{name}.png", texture.EncodeToPNG());

            var textureViewer = Instantiate(textureViewerPrefab);
            textureViewer.SetTexture(texture);
            textureViewer.transform.position = new float3(offset.x, 0, offset.y);
        }
    }
}