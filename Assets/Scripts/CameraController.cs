using UnityEngine;

class CameraController : MonoBehaviour
{
    [SerializeField]
    GameObject glyphTestCamera;

    [SerializeField]
    GameObject rectTestCamera;

    public bool isGlyphTest;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            if (isGlyphTest) glyphTestCamera.transform.position += Vector3.right * 5;
            else rectTestCamera.transform.position += Vector3.right * 960;
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            if (isGlyphTest) glyphTestCamera.transform.position += Vector3.left * 5;
            else rectTestCamera.transform.position += Vector3.left * 960;
        }
        else if (Input.GetKeyDown(KeyCode.W))
        { 
            if (isGlyphTest) glyphTestCamera.transform.position += Vector3.up * 5;
            else rectTestCamera.transform.position += Vector3.up * 960;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        { 
            if (isGlyphTest) glyphTestCamera.transform.position += Vector3.down * 5;
            else rectTestCamera.transform.position += Vector3.down * 960;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            rectTestCamera.SetActive(true);
            glyphTestCamera.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.G))
        { 
            glyphTestCamera.SetActive(true);
            rectTestCamera.SetActive(false);
        }
    }
}
