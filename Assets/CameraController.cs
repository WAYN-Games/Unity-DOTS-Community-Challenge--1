using Unity.Mathematics;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera cam; 
    

    // Update is called once per frame
    void Update()
    {
        cam.orthographicSize -= Input.mouseScrollDelta.y * 50f;
        cam.orthographicSize = math.max(50f,cam.orthographicSize);
        
        if (Input.GetKey(KeyCode.W))
            cam.transform.position += cam.transform.up * (cam.orthographicSize * Time.deltaTime);
        if (Input.GetKey(KeyCode.S))
            cam.transform.position -= cam.transform.up * (cam.orthographicSize * Time.deltaTime);
        if (Input.GetKey(KeyCode.A))
            cam.transform.position -= cam.transform.right * (cam.orthographicSize * Time.deltaTime);
        if (Input.GetKey(KeyCode.D))
            cam.transform.position += cam.transform.right * (cam.orthographicSize * Time.deltaTime);

    }
}
