using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

class RectObject
{
    public GameObject Gb;
    public bool IsValid;
}

class RectAtlasTester : PluginLoader
{
    delegate int InitTestDelegate(int testNumber); // returns passCount
    delegate int InitPassDelegate(int passIndex); // returns stepsCount;
    delegate int StepDelegate(); // returns texturesCount;
    delegate void RemoveUnusedDelegate();

    delegate int3 GetRectsCountDelegate(int textureId);
    delegate CppRect GetPlacedGlyphDelegate(int textureId, int glyphIndex);
    delegate CppRect GetFreeShelfRectDelegate(int textureId, int freeShelfIndex);
    delegate CppRect GetFreeSlotRectDelegate(int textureId, int freeSlotIndex);

    public CppRectRenderer texturePrefab;
    public CppRectRenderer glyphPrefab;
    public CppRectRenderer shelfPrefab;
    public CppRectRenderer slotPrefab;

    InitTestDelegate InitTest;
    InitPassDelegate InitPass;
    StepDelegate Step;
    RemoveUnusedDelegate RemoveUnused;
    GetRectsCountDelegate GetRectsCount;
    GetPlacedGlyphDelegate GetPlacedGlyph;
    GetFreeShelfRectDelegate GetFreeShelfRect;
    GetFreeSlotRectDelegate GetFreeSlotRect;

    protected override void Awake()
    {
        base.Awake();

        InitTest = GetDelegate<InitTestDelegate>(libraryHandle, "InitTest");
        InitPass = GetDelegate<InitPassDelegate>(libraryHandle, "InitPass");
        Step = GetDelegate<StepDelegate>(libraryHandle, "Step");
        RemoveUnused = GetDelegate<RemoveUnusedDelegate>(libraryHandle, "RemoveUnused");

        GetRectsCount = GetDelegate<GetRectsCountDelegate>(libraryHandle, "GetRectsCount");
        GetPlacedGlyph = GetDelegate<GetPlacedGlyphDelegate>(libraryHandle, "GetPlacedGlyph");
        GetFreeShelfRect = GetDelegate<GetFreeShelfRectDelegate>(libraryHandle, "GetFreeShelfRect");
        GetFreeSlotRect = GetDelegate<GetFreeSlotRectDelegate>(libraryHandle, "GetFreeSlotRect");

        StartCoroutine(AtlasSequence(0));
    }

    Dictionary<CppRect, RectObject> rects;

    IEnumerator AtlasSequence(int testNumber)
    {
        const float rectStepTime = 0.1f;
        const float stepTime = 0.1f;

        rects = new Dictionary<CppRect, RectObject>(200);

        int passCount = InitTest(0);
        for (int p = 0; p < passCount; p++)
        {
            Debug.Log("New pass start");

            int stepsCount = InitPass(p);
            int skipsCount = 5;
            for (int s = 0; s < stepsCount; s += skipsCount)
            {
                // List<CppRectRenderer> stepRects = new();
                int skips = math.min(stepsCount - s, skipsCount);
                int texturesCount = 0;
                for (int i = 0; i < skips; i++)
                {
                    texturesCount = Step();
                }

                for (int t = 0; t < texturesCount; t++)
                {
                    CppRect textureRect = new CppRect((ushort)(t * (512 + 50)), 0, 512, 512);
                    Transform parent = AddRectObject(textureRect, texturePrefab, null);
                    int3 counts = GetRectsCount(t);
                    for (int g = 0; g < counts.x; g++)
                    {
                        CppRect r = GetPlacedGlyph(t, g);
                        AddRectObject(r, glyphPrefab, parent);
                        // yield return new WaitForSeconds(rectStepTime);
                    }
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
                RemoveUnusedRectObjects();

                yield return new WaitForSeconds(stepTime);
            }
            RemoveUnused();


            yield return new WaitForSeconds(1);
        }
    }

    Transform AddRectObject(CppRect rect, CppRectRenderer prefab, Transform parent)
    {
        if (rects.ContainsKey(rect))
        {
            var rectObj = rects[rect];
            rectObj.IsValid = true;
            return rectObj.Gb.transform;
        }
        else
        {
            var rend = Instantiate(prefab);
            rend.Render(rect, 1, 1);
            if (parent != null)
            { 
                rend.transform.SetParent(parent.transform, false);
            }

            var rectObj = new RectObject();
            rectObj.IsValid = true;
            rectObj.Gb = rend.gameObject;
            rects.Add(rect, rectObj);
            return rectObj.Gb.transform;
        }
    }

    void RemoveUnusedRectObjects()
    {
        List<CppRect> toRemoves = new List<CppRect>();
        foreach (var pair in rects)
        {
            if (!pair.Value.IsValid)
            {
                var rectObj = pair.Value;
                Destroy(rectObj.Gb);
                toRemoves.Add(pair.Key);
            }
            else
            {
                pair.Value.IsValid = false;
            }
        }

        foreach (var toRemove in toRemoves)
        {
            rects.Remove(toRemove);
        }
    }
}