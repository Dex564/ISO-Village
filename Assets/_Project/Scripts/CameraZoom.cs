using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraZoom : MonoBehaviour
{
    [Tooltip("Zoom speed")]
    public float zoomSpeed = 5f;

    [Tooltip("Min zoom")]
    public float minSize = 2f;

    [Tooltip("Max zoom")]
    public float maxSize = 8f;

    [Tooltip("Zoom smooth")]
    public float smoothTime = 0.05f;
    private Camera cam;
    private float targetSize = 2f;
    private float velocity;
    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographicSize = targetSize;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetSize -= scroll * zoomSpeed;
            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
        }

        if (smoothTime > 0f)
        {
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref velocity, smoothTime);
        }
        else
        {
            cam.orthographicSize = targetSize;
        }
    }
}
