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
    const float stepTime = 0.2f;
    const float sequenceTime = 0.1f;

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
            int maxSkipsCount = 1;
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
        yield return new WaitForSeconds(sequenceTime);
    }

    IEnumerator GetPlacedGlyphSequence()
    {
        int texturesCount = GetTexturesCount();
        PreparePlacedByTextureBuffer();
        for (int t = 0; t < texturesCount; t++)
        {
            CppRect textureRect = new CppRect(0, (ushort)(t * (512 + 50)), 512, 512);
            Transform parent = AddRectObject(textureRect, texturePrefab, null);
            yield return new WaitForSeconds(1);
            int placedCount = GetPlacedCount(t);
            for (int glyph = 0; glyph < placedCount; glyph++)
            {
                CppRect r = GetPlacedGlyph(t, glyph);
                AddRectObject(r, glyphPrefab, parent);
                Debug.Log($"Texture {t} Glyph {glyph} Rect {r}");
                yield return new WaitForSeconds(stepTime);
            }
        }

        yield return new WaitForSeconds(sequenceTime);
    }

    IEnumerator RemovePlacedSequence()
    {
        int removePlacedStepsCount = InitRemovePlacedPass();
        for (int i = 0; i < removePlacedStepsCount; i++)
        {
            CppRect r = RemovePlacedStep();
            RemoveRenderer(r);
            Debug.Log($"RemovedPlacedGlyphRect {r}");
            yield return new WaitForSeconds(stepTime);

        }
        yield return new WaitForSeconds(sequenceTime);
    }

    IEnumerator GetModifiedFreeSequence()
    {
        int getModifiedFreeStepsCount = InitGetModifiedFreePass();
        for (int i = 0; i < getModifiedFreeStepsCount; i++)
        {
            var r = GetModifiedFreeStep();
            RemoveRenderer(r);
            Debug.Log($"RemovedModifiedFreeRect {r}");
            yield return new WaitForSeconds(stepTime);
        }
        yield return new WaitForSeconds(sequenceTime);
    }

    IEnumerator GetFreeFromBufferSequence()
    {
        PrepareFreeByTextureBuffer();
        int texturesCount = GetTexturesCount();
        for (int t = 0; t < texturesCount; t++)
        {
            CppRect textureRect = new CppRect(0, (ushort)(t * (512 + 50)), 512, 512);
            Transform parent = AddRectObject(textureRect, texturePrefab, null);

            int2 freeCounts = GetFreeCounts(t);
            for (int shelf = 0; shelf < freeCounts.x; shelf++)
            {
                CppRect r = GetFreeShelfRect(t, shelf);
                AddRectObject(r, shelfPrefab, parent);
                Debug.Log($"Texture {t} Shelf {shelf} Rect {r}");
                yield return new WaitForSeconds(stepTime);
            }
            for (int slot = 0; slot < freeCounts.y; slot++)
            {
                CppRect r = GetFreeSlotRect(t, slot);
                AddRectObject(r, slotPrefab, parent);
                Debug.Log($"Texture {t} Slot {slot} Rect {r}");
                yield return new WaitForSeconds(stepTime);
            }
        }

        yield return new WaitForSeconds(sequenceTime);
    }

    Transform AddRectObject(CppRect rect, CppRectRenderer prefab, Transform parent)
    {
        if (rects.ContainsKey(rect))
        {
            return rects[rect].transform;
        }
        else
        {
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

/*
removeStepsCount = InitRemoveFreeSteps();
                for (int r = 0; r < removeStepsCount; r += maxSkipsCount)
                {
                    skips = math.min(removeStepsCount - r, maxSkipsCount);
                    for (int i = 0; i < skips; i++)
                    {
                        CppRect removedFreeRec = RemoveFreeStep();
                        RemoveRenderer(removedFreeRec);
                    }
                    // yield return new WaitForSeconds(stepTime / 2);
                }

            removeStepsCount = InitRemovePlacedSteps();
            for (int r = 0; r < removeStepsCount; r += maxSkipsCount)
            {
                int skips = math.min(removeStepsCount - r, maxSkipsCount);
                for (int i = 0; i < skips; i++)
                {
                    CppRect removedFreeRec = RemovePlacedStep();
                    RemoveRenderer(removedFreeRec);
                }
                yield return new WaitForSeconds(stepTime / 2);
            }

            removeStepsCount = InitRemoveFreeSteps();
            for (int r = 0; r < removeStepsCount; r += maxSkipsCount)
            {
                int skips = math.min(removeStepsCount - r, maxSkipsCount);
                for (int i = 0; i < skips; i++)
                {
                    CppRect removedFreeRec = RemoveFreeStep();
                    RemoveRenderer(removedFreeRec);
                }
                // yield return new WaitForSeconds(stepTime / 2);
            }

            for (int t = 0; t < texturesCount; t++)
            {
                CppRect textureRect = new CppRect(0, (ushort)(t * (512 + 50)), 512, 512);
                Transform parent = AddRectObject(textureRect, texturePrefab, null);
                int3 counts = GetRectsCount(t);
                for (int shelf = 0; shelf < counts.y; shelf++)
                {
                    CppRect r = GetFreeShelfRect(t, shelf);
                    AddRectObject(r, shelfPrefab, parent);
                    // yield return new WaitForSeconds(rectStepTime);
                }
                for (int slot = 0; slot < counts.z; slot++)
                {
                    CppRect r = GetFreeSlotRect(t, slot);
                    AddRectObject(r, slotPrefab, parent);
                    // yield return new WaitForSeconds(rectStepTime);
                }
            }
*/