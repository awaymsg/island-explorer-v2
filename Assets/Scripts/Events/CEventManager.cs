using UnityEngine;
using UnityEngine.UIElements;

public class CEventManager : MonoBehaviour
{
    private static CEventManager m_Instance;

    [SerializeField, Tooltip("Pool of local events")]
    private SBiomeEventsPool[] m_LocalEventsPool;

    public static CEventManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = FindFirstObjectByType<CEventManager>();
            }

            return m_Instance;
        }
    }

    public void StartLocalEvent(CLocalEvent localEvent)
    {
        if (localEvent == null)
        {
            Debug.Log("StartLocalEvent - localEvent is null!");
            return;
        }


    }
}
