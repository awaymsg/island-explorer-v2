using UnityEngine;
using UnityEngine.InputSystem;

public class CCameraManager : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float m_MoveSpeed = 10f;
    [SerializeField]
    private float m_ZoomSpeed = 20f;
    [SerializeField]
    private float m_MinZoom = 2f;
    [SerializeField]
    private float m_MaxZoom = 20f;
    [SerializeField]
    private float m_FollowSpeed = 1f;

    // Input values
    private static Vector2 m_MoveInput;
    private static float m_ZoomInput;
    private static bool m_bEnableMapMovement = false;

    private Camera m_Camera;
    private GameObject m_TargetPlayer;

    // getters and setters
    public static bool IsCameraMapMovementEnabled
    {
        get { return m_bEnableMapMovement; }
    }

    public GameObject TargetPlayer
    {
        set { m_TargetPlayer = value; }
    }
    //--

    private void Awake()
    {
        m_Camera = GetComponent<Camera>();
    }

    void Update()
    {
        HandleZoom();

        if (m_bEnableMapMovement)
        {
            HandleMovement();
        }
        else
        {
            if (m_TargetPlayer != null)
            {
                Vector2 cameraPosition = m_Camera.transform.position;
                Vector2 playerPosition = m_TargetPlayer.transform.position;
                Vector2 newPosition = Vector2.Lerp(cameraPosition, playerPosition, m_FollowSpeed * Time.deltaTime);

                m_Camera.transform.position = new Vector3(newPosition.x, newPosition.y, m_Camera.transform.position.z);
            }
        }
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
