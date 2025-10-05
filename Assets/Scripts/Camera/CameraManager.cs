using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 20f;
    public float rotationSpeed = 100f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    // Input values
    private static Vector2 moveInput;
    private static float zoomInput;
    private static Vector2 rotateInput;
    private static bool isRotating;
    private static bool isDragging = false;
    private static Vector3 dragOrigin;
    private static Vector3 dragDifference;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleDrag();
    }

    // Input System Event Handlers
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue<float>();
    }

    public void OnClickDrag(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            dragOrigin = GetMousePosition();
        }

        isDragging = context.started || context.performed;
    }

    private void HandleMovement()
    {
        if (moveInput != Vector2.zero)
        {
            Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
    }

    private void HandleZoom()
    {
        if (zoomInput != 0)
        {
            float zoomChange = -zoomInput * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + zoomChange, minZoom, maxZoom);
        }
    }

    private void HandleDrag()
    {
        if (isDragging)
        {
            dragDifference = GetMousePosition() - transform.position;
            transform.position = dragOrigin - dragDifference;
        }
    }

    private Vector3 GetMousePosition()
    {
        return cam.ScreenToWorldPoint((Vector3)Mouse.current.position.ReadValue());
    }
}
