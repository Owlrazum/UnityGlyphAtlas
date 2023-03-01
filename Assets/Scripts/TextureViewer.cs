using UnityEngine;

class TextureViewer : MonoBehaviour
{
    MeshRenderer rend;
    void Awake()
    {
        TryGetComponent(out rend);
    }

    public void SetTexture(Texture2D texture)
    {
        rend.material.mainTexture = texture;
    }
}