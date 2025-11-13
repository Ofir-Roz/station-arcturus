using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Planet center
    public float targetDistance = 100f; // Start point

    [Header("Rotation")]
    public float rotationSpeed = 100f;
    public float verticalRotationLimit = 85f; 
    [Header("Zoom")]
    public float zoomSpeed = 10f;
    public float minDistance = 40f;
    public float maxDistance = 140f; //zooming out 

    [Header("Mouse Control")]
    public bool useMouseDrag = true;

    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 30f;
    private float currentDistance;

    private Mouse mouse;
    private Keyboard keyboard;

    void Start()
    {
        currentDistance = targetDistance;

        mouse = Mouse.current;
        keyboard = Keyboard.current;

        if (target == null)
        {
            GameObject planet = GameObject.Find("PlanetSystem");
            if (planet != null)
            {
                target = planet.transform;
            }
        }

        UpdateCameraPosition();
    }

    void Update()
    {
        HandleRotationInput();
        HandleZoomInput();
        UpdateCameraPosition();
    }

    void HandleRotationInput()
    {
        // Keyboard rotation
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed) horizontalInput -= 1f;
            if (keyboard.dKey.isPressed) horizontalInput += 1f;
            if (keyboard.wKey.isPressed) verticalInput += 1f;
            if (keyboard.sKey.isPressed) verticalInput -= 1f;
        }

        // Mouse drag rotation
        if (useMouseDrag && mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                horizontalInput += mouseDelta.x * 0.1f;
                verticalInput -= mouseDelta.y * 0.1f;
            }
        }

        // Apply rotation
        currentHorizontalAngle += horizontalInput * rotationSpeed * Time.deltaTime;
        currentVerticalAngle += verticalInput * rotationSpeed * Time.deltaTime;

        // Clamp vertical angle
        currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, -verticalRotationLimit, verticalRotationLimit);
    }

    void HandleZoomInput()
    {
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            currentDistance -= scroll * zoomSpeed * Time.deltaTime;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
        }
    }

    void UpdateCameraPosition()
    {
        if (target == null) return;

        // Calculate orbital position
        Quaternion rotation = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0);
        Vector3 offset = rotation * (Vector3.back * currentDistance);

        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}
