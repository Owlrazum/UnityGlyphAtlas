using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

class RectAtlasTester : PluginLoader
{
    delegate int InitTestDelegate(int testNumber); // returns passCount
    delegate int InitUpdatePassDelegate(int passNumber); // returns updateStepsCount;
    delegate void UpdateStepDelegate();

    delegate int GetTexturesCountDelegate();
    delegate void PreparePlacedByTextureBufferDelegate();
    delegate int GetPlacedCountDelegate(int textureId);
    delegate CppRect GetPlacedGlyphDelegate(int textureId, int glyphIndex);

    delegate void PrepareFreeByTextureBufferDelegate();
    delegate int2 GetFreeCountsDelegate(int textureId);
    delegate CppRect GetFreeShelfRectDelegate(int textureId, int freeShelfIndex);
    delegate CppRect GetFreeSlotRectDelegate(int textureId, int freeSlotIndex);

    delegate int InitGetModifiedFreePassDelegate();
    delegate CppRect GetModifiedFreeStepDelegate();

    delegate int InitRemovePlacedPassDelegate();
    delegate CppRect RemovePlacedStepDelegate();

    InitTestDelegate InitTest;
    InitUpdatePassDelegate InitUpdatePass;
    UpdateStepDelegate UpdateStep;

    GetTexturesCountDelegate GetTexturesCount;
    PreparePlacedByTextureBufferDelegate PreparePlacedByTextureBuffer;
    GetPlacedCountDelegate GetPlacedCount;
    GetPlacedGlyphDelegate GetPlacedGlyph;

    PrepareFreeByTextureBufferDelegate PrepareFreeByTextureBuffer;
    GetFreeCountsDelegate GetFreeCounts;
    GetFreeShelfRectDelegate GetFreeShelfRect;
    GetFreeSlotRectDelegate GetFreeSlotRect;

    InitGetModifiedFreePassDelegate InitGetModifiedFreePass;
    GetModifiedFreeStepDelegate GetModifiedFreeStep;

    InitRemovePlacedPassDelegate InitRemovePlacedPass;
    RemovePlacedStepDelegate RemovePlacedStep;

    public CppRectRenderer texturePrefab;
    public CppRectRenderer glyphPrefab;
    public CppRectRenderer shelfPrefab;
    public CppRectRenderer slotPrefab;

    Dictionary<CppRect, GameObject> rects;
    [SerializeField]
    float stepTime = 0.01f;

    [SerializeField]
    bool shouldPauseAtNewRectAdded = false;

    [SerializeField]
    bool shouldPauseAtRectRemoval = false;

    [SerializeField]
    bool shouldPauseAtSequenceEnd = false;

    [SerializeField]
    float sequencePauseTime = 0.01f;

    List<(CppRect, GameObject)> renderedTextures;

    protected override void Awake()
    {
        base.Awake();

        InitTest = GetDelegate<InitTestDelegate>(libraryHandle, "InitTest");
        InitUpdatePass = GetDelegate<InitUpdatePassDelegate>(libraryHandle, "InitUpdatePass");
        UpdateStep = GetDelegate<UpdateStepDelegate>(libraryHandle, "UpdateStep");

        GetTexturesCount = GetDelegate<GetTexturesCountDelegate>(libraryHandle, "GetTexturesCount");
        PreparePlacedByTextureBuffer = GetDelegate<PreparePlacedByTextureBufferDelegate>(libraryHandle, "PreparePlacedByTextureBuffer");
        GetPlacedCount = GetDelegate<GetPlacedCountDelegate>(libraryHandle, "GetPlacedCount");
        GetPlacedGlyph = GetDelegate<GetPlacedGlyphDelegate>(libraryHandle, "GetPlacedGlyph");

        PrepareFreeByTextureBuffer = GetDelegate<PrepareFreeByTextureBufferDelegate>(libraryHandle, "PrepareFreeByTextureBuffer");
        GetFreeCounts = GetDelegate<GetFreeCountsDelegate>(libraryHandle, "GetFreeCounts");
        GetFreeShelfRect = GetDelegate<GetFreeShelfRectDelegate>(libraryHandle, "GetFreeShelfRect");
        GetFreeSlotRect = GetDelegate<GetFreeSlotRectDelegate>(libraryHandle, "GetFreeSlotRect");

        InitGetModifiedFreePass = GetDelegate<InitGetModifiedFreePassDelegate>(libraryHandle, "InitGetModifiedFreePass");
        GetModifiedFreeStep = GetDelegate<GetModifiedFreeStepDelegate>(libraryHandle, "GetModifiedFreeStep");

        InitRemovePlacedPass = GetDelegate<InitRemovePlacedPassDelegate>(libraryHandle, "InitRemovePlacedPass");
        RemovePlacedStep = GetDelegate<RemovePlacedStepDelegate>(libraryHandle, "RemovePlacedStep");

        rects = new Dictionary<CppRect, GameObject>(200);
        renderedTextures = new List<(CppRect, GameObject)>(5);
        StartCoroutine(AtlasSequence(0));
    }

    IEnumerator AtlasSequence(int testNumber)
    {
        int passCount = InitTest(0);
        Debug.Log("passCount " + passCount);

        for (int p = 0; p < passCount; p++)
        {
            Debug.Log("New pass start");

            int updateStepsCount = InitUpdatePass(p);
            int maxSkipsCount = 40;
            for (int s = 0; s < updateStepsCount; s += maxSkipsCount)
            {
                int skipsCount = math.min(updateStepsCount - s, maxSkipsCount);
                yield return StartCoroutine(StepSequence(skipsCount));
            }

            yield return StartCoroutine(RemovePlacedSequence());
            yield return StartCoroutine(GetModifiedFreeSequence());
            yield return StartCoroutine(GetFreeFromBufferSequence());
        }
    }

    IEnumerator StepSequence(int skipsCount)
    {
        for (int i = 0; i < skipsCount; i++)
        {
            UpdateStep();
        }

        yield return StartCoroutine(GetModifiedFreeSequence());
        yield return StartCoroutine(GetPlacedGlyphSequence());
        yield return StartCoroutine(GetFreeFromBufferSequence());
        if (shouldPauseAtSequenceEnd) yield return new WaitForSeconds(sequencePauseTime);
    }

    IEnumerator GetPlacedGlyphSequence()
    {
        int texturesCount = GetTexturesCount();
        PreparePlacedByTextureBuffer();
        for (int t = 0; t < texturesCount; t++)
        {
            Transform parent;
            if (t == renderedTextures.Count)
            {
                ushort posX = (ushort)(t % 2 == 0 ? 0 : 562);
                ushort posY = (ushort)(t / 2 * 562);

                CppRect textureRect = new CppRect(posX, posY, 512, 512);
                parent = AddRectObject(textureRect, texturePrefab, null, out bool addedNew);
                renderedTextures.Add((textureRect, parent.gameObject));
                if (shouldPauseAtNewRectAdded) yield return new WaitForSeconds(stepTime);
            }
            else
            {
                parent = renderedTextures[t].Item2.transform;
            }

            int placedCount = GetPlacedCount(t);
            for (int glyph = 0; glyph < placedCount; glyph++)
            {
                CppRect r = GetPlacedGlyph(t, glyph);
                AddRectObject(r, glyphPrefab, parent, out bool addedNew);
                if (addedNew)
                {
                    if (shouldPauseAtNewRectAdded)  yield return new WaitForSeconds(stepTime);
                }
            }
        }

        if (shouldPauseAtSequenceEnd) yield return new WaitForSeconds(sequencePauseTime);
    }

    IEnumerator RemovePlacedSequence()
    {
        int removePlacedStepsCount = InitRemovePlacedPass();
        for (int i = 0; i < removePlacedStepsCount; i++)
        {
            CppRect r = RemovePlacedStep();
            RemoveRenderer(r);
            if (shouldPauseAtRectRemoval) yield return new WaitForSeconds(stepTime);
        }

        int texturesCount = GetTexturesCount();
        for (int i = texturesCount; i < renderedTextures.Count; i++)
        {
            RemoveRenderer(renderedTextures[i].Item1);
            renderedTextures.RemoveAt(i);
            i--;
        }
        if (shouldPauseAtSequenceEnd) yield return new WaitForSeconds(sequencePauseTime);
    }

    IEnumerator GetModifiedFreeSequence()
    {
        int getModifiedFreeStepsCount = InitGetModifiedFreePass();
        for (int i = 0; i < getModifiedFreeStepsCount; i++)
        {
            var r = GetModifiedFreeStep();
            RemoveRenderer(r);
            if (shouldPauseAtRectRemoval) yield return new WaitForSeconds(stepTime);
        }
        if (shouldPauseAtSequenceEnd) yield return new WaitForSeconds(sequencePauseTime);
    }

    IEnumerator GetFreeFromBufferSequence()
    {
        PrepareFreeByTextureBuffer();
        for (int t = 0; t < renderedTextures.Count; t++)
        {
            Transform parent = renderedTextures[t].Item2.transform;

            int2 freeCounts = GetFreeCounts(t);
            for (int shelf = 0; shelf < freeCounts.x; shelf++)
            {
                CppRect r = GetFreeShelfRect(t, shelf);
                AddRectObject(r, shelfPrefab, parent, out bool addedNew);
                if (addedNew)
                { 
                    if (shouldPauseAtNewRectAdded) yield return new WaitForSeconds(stepTime);
                }
            }
            for (int slot = 0; slot < freeCounts.y; slot++)
            {
                CppRect r = GetFreeSlotRect(t, slot);
                AddRectObject(r, slotPrefab, parent, out bool addedNew);
                if (addedNew)
                { 
                    if (shouldPauseAtNewRectAdded) yield return new WaitForSeconds(stepTime);
                }
            }
        }

        if (shouldPauseAtSequenceEnd) yield return new WaitForSeconds(sequencePauseTime);
    }

    Transform AddRectObject(CppRect rect, CppRectRenderer prefab, Transform parent, out bool addedNew)
    {
        if (rects.ContainsKey(rect))
        {
            addedNew = false;
            return rects[rect].transform;
        }
        else
        {
            addedNew = true;
            var rend = Instantiate(prefab);
            rend.Render(rect, 1, 1);
            if (parent != null)
            {
                rend.transform.SetParent(parent.transform, false);
            }

            rects.Add(rect, rend.gameObject);
            return rend.gameObject.transform;
        }
    }

    void RemoveRenderer(in CppRect r)
    {
        if (rects.ContainsKey(r))
        {
            Destroy(rects[r]);
            rects.Remove(r);
        }
    }
}