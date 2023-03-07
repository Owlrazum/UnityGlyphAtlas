using UnityEngine;

class CameraController : MonoBehaviour
{
    [SerializeField]
    GameObject glyphTestCamera;

    [SerializeField]
    GameObject rectTestCamera;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            glyphTestCamera.transform.position += Vector3.right * 5;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            glyphTestCamera.transform.position += Vector3.left * 5;
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
