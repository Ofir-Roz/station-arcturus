using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("Bounds")]
    [SerializeField] private float minY = 5f;
    [SerializeField] private float maxY = 50f;

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    void HandleMovement()
    {
        Vector2 moveInput = Vector2.zero;

        // WASD movement
        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;

        // Arrow keys movement
        if (Keyboard.current.upArrowKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1;

        Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    void HandleRotation()
    {
        // Q/E rotation
        if (Keyboard.current.qKey.isPressed)
            transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime);
        if (Keyboard.current.eKey.isPressed)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    void HandleZoom()
    {
        // Mouse scroll zoom
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y - scroll * zoomSpeed * 0.01f, minY, maxY);
            transform.position = pos;
        }
    }
}
