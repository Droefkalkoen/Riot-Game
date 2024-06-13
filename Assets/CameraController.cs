using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float panSpeed = 20f;
    public float panBorderThickness = 10f;
    public bool edgeScrolling = false;
    public Vector2 panLimit;
    public float smoothTime = 0.5F;

    public float minY = 20f; // minimum camera height
    public float maxY = 100f; // maximum camera height
    public float zoomSpeed = 30f; // speed of zooming in/out

    public float shiftMultiplier = 3.0f; // how much the speed is multiplied while holding shift

    private Vector3 velocity = Vector3.zero;

    private Plane plane; 


    private void Start()
    {
        plane = new Plane(Vector3.up, 1.0f);
    }

    void FixedUpdate()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey("w") || edgeScrolling && Input.mousePosition.y >= Screen.height - panBorderThickness)
        {
            direction += Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
        if (Input.GetKey("s") || edgeScrolling && Input.mousePosition.y <= panBorderThickness)
        {
            direction -= Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }
        if (Input.GetKey("a") || edgeScrolling && Input.mousePosition.x <= panBorderThickness)
        {
            direction -= transform.right;
        }
        if (Input.GetKey("d") || edgeScrolling && Input.mousePosition.x >= Screen.width - panBorderThickness)
        {
            direction += transform.right;
        }

        Vector3 targetPosition = transform.position + direction * panSpeed * Time.fixedUnscaledDeltaTime * Mathf.Clamp(transform.position.y / 40f, 1f, 2.5f) * ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? (shiftMultiplier) : (1.0f));

        // Keep camera height
        targetPosition.y = transform.position.y;

        // limit  movement
        targetPosition.x = Mathf.Clamp(targetPosition.x, -panLimit.x, panLimit.x);
        targetPosition.z = Mathf.Clamp(targetPosition.z, -panLimit.y, panLimit.y);

        //  zooming
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        Vector3 scrollDirection = (scroll > 0 && transform.position.y > (minY + 0.1f)) ? (GetMouseWorldPosition() - transform.position) : (Vector3.down + (transform.forward));
        targetPosition += scrollDirection.normalized * scroll * zoomSpeed * Time.fixedUnscaledDeltaTime;

        // Clamp y pos of camera
        targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);

        // Smooth
        float smoothTimeAdjusted = smoothTime / Time.timeScale;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTimeAdjusted, Mathf.Infinity, Time.unscaledDeltaTime);
    }

    private Vector3 GetMouseWorldPosition()
    {
        float distance;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (plane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}
