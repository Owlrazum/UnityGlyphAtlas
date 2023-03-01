using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;
class RectAtlasTester : PluginLoader
{ 
    delegate int InitTestDelegate(int testNumber); // returns passCount
    delegate int InitPassDelegate(int passIndex); // returns stepsCount;
    delegate int StepDelegate(); // returns texturesCount;

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

        GetRectsCount = GetDelegate<GetRectsCountDelegate>(libraryHandle, "GetRectsCount");
        GetPlacedGlyph = GetDelegate<GetPlacedGlyphDelegate>(libraryHandle, "GetPlacedGlyph");
        GetFreeShelfRect = GetDelegate<GetFreeShelfRectDelegate>(libraryHandle, "GetFreeShelfRect");
        GetFreeSlotRect = GetDelegate<GetFreeSlotRectDelegate>(libraryHandle, "GetFreeSlotRect");

        StartCoroutine(AtlasSequence(0));
    }

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
}