using UnityEngine;
using UnityEngine.UIElements;

public class CWorldInfoUI : MonoBehaviour
{
    private UIDocument m_WorldInfoUI;
    private VisualElement m_WorldInfoPanel;
    private Label m_DayInfoLabel;
    private Label m_WorldInfoLabel;

    private void OnEnable()
    {
        m_WorldInfoUI = GetComponent<UIDocument>();
        m_WorldInfoPanel = m_WorldInfoUI.rootVisualElement.Q<VisualElement>("WorldInfoPanel");
        m_DayInfoLabel = m_WorldInfoPanel.Q<Label>("DayInfo");
        m_WorldInfoLabel = m_WorldInfoPanel.Q<Label>("WorldInfo");
    }

    public void InitializeWorldInfoPanel()
    {
        m_DayInfoLabel.text = "Day 0";
        m_WorldInfoLabel.text = "";
    }

    public void UpdateDayInfo(string dayInfo)
    {
        m_DayInfoLabel.text = dayInfo;
    }

    public void UpdateWorldInfo(string worldInfo)
    {
        m_WorldInfoLabel.text = worldInfo;
    }
}
