using UnityEngine;
using UnityEngine.InputSystem;

public class CCameraManager : MonoBehaviour
{
    [Header("Movement Settings")]
    public float m_MoveSpeed = 10f;
    public float m_ZoomSpeed = 20f;
    public float m_MinZoom = 2f;
    public float m_MaxZoom = 20f;

    // Input values
    private static Vector2 m_MoveInput;
    private static float m_ZoomInput;
    private static bool m_bEnableMapMovement = false;

    private Camera m_Camera;

    public static bool IsCameraMapMovementEnabled
    {
        get { return m_bEnableMapMovement; }
    }

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        m_MoveInput = context.ReadValue<Vector2>();
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        m_ZoomInput = context.ReadValue<float>();
    }

    public void OnEnableMapMovement(InputAction.CallbackContext context)
    {
        m_bEnableMapMovement = context.started || context.performed;
    }

    private void HandleMovement()
    {
        if (m_MoveInput != Vector2.zero && m_bEnableMapMovement)
        {
            Vector3 movement = new Vector3(m_MoveInput.x, m_MoveInput.y, 0) * m_MoveSpeed * Time.deltaTime;
            transform.Translate(movement, Space.World);
        }
    }

    private void HandleZoom()
    {
        if (m_ZoomInput != 0)
        {
            float zoomChange = -m_ZoomInput * m_ZoomSpeed;
            m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize + zoomChange, m_MinZoom, m_MaxZoom);
        }
    }

    public void MoveCameraToPosition(Vector3 worldPosition)
    {
        worldPosition.z = m_Camera.transform.position.z;
        m_Camera.transform.position = worldPosition;
    }
}
