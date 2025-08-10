using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CameraFly : MonoBehaviour
{
    public float zoomSpeed = 10f;
    public float lookSpeed = 2f;
    public float panSpeed = 0.5f;
    public float minDistance = 1f;
    public float maxDistance = 1000f;

    private float yaw = 0f;
    private float pitch = 0f;
    private Vector3 lastMousePosition;
    private bool isRightMouseHeld = false;
    private bool isLeftMouseHeld = false;
    private float distanceToTarget = 10f;
    private Vector3 target;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        target = transform.position + transform.forward * distanceToTarget;
        distanceToTarget = Vector3.Distance(transform.position, target);
    }

    void Update()
    {
        // Only block camera controls if specifically interacting with Button, Toggle, or Slider
        if (IsInteractingWithSpecificUI())
        {
            isLeftMouseHeld = false;
            isRightMouseHeld = false;
            return;
        }

        // Left mouse: orbit/angle movement
        if (Input.GetMouseButtonDown(0))
        {
            isLeftMouseHeld = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isLeftMouseHeld = false;
        }

        if (isLeftMouseHeld)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            yaw += delta.x * lookSpeed * 0.1f;
            pitch -= delta.y * lookSpeed * 0.1f;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            lastMousePosition = Input.mousePosition;
        }

        // Right mouse: pan (shift/fly)
        if (Input.GetMouseButtonDown(1))
        {
            isRightMouseHeld = true;
            lastMousePosition = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRightMouseHeld = false;
        }

        if (isRightMouseHeld)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 right = transform.right;
            Vector3 up = transform.up;
            target -= right * delta.x * panSpeed * 0.01f;
            target -= up * delta.y * panSpeed * 0.01f;
            lastMousePosition = Input.mousePosition;
        }

        // Mouse scroll: zoom in/out
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distanceToTarget -= scroll * zoomSpeed;
            distanceToTarget = Mathf.Clamp(distanceToTarget, minDistance, maxDistance);
        }

        // Update camera position and rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 direction = rotation * Vector3.forward;
        transform.position = target - direction * distanceToTarget;
        transform.rotation = rotation;
    }

    private bool IsInteractingWithSpecificUI()
    {
        if (EventSystem.current == null) return false;

        // Check if currently selected object is a Button, Toggle, or Slider
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected.GetComponent<Button>() != null ||
                selected.GetComponent<Toggle>() != null ||
                selected.GetComponent<Slider>() != null)
            {
                return true;
            }
        }

        // Check if mouse is over a specific UI element (Button, Toggle, Slider)
        if (EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<Button>() != null ||
                    result.gameObject.GetComponent<Toggle>() != null ||
                    result.gameObject.GetComponent<Slider>() != null)
                {
                    return true;
                }
            }
        }

        return false;
    }
}