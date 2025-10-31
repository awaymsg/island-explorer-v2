using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.MessageBox;

public class CPopupsUI : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset m_ContinueMovementDialogueTemplate;

    private UIDocument m_BlankCanvasUI;

    public void OnEnable()
    {
        m_BlankCanvasUI = GetComponent<UIDocument>();
    }

    // TODO: we can make this generic actually, in retrospect
    public void CreateContinueMovementDialogue(float actual, Action<bool> bOnResponse)
    {
        // Create a new popup
        TemplateContainer continueDialogueInstance = m_ContinueMovementDialogueTemplate.Instantiate();
        if (continueDialogueInstance == null)
        {
            bOnResponse?.Invoke(false);
            return;
        }

        Label continueMovementLabel = continueDialogueInstance.Q<Label>("ContinueMovementDialogueLabel");
        Button yesButton = continueDialogueInstance.Q<Button>("ContinueMovementButtonYes");
        Button noButton = continueDialogueInstance.Q<Button>("ContinueMovementButtonNo");

        if (yesButton == null || noButton == null || continueMovementLabel == null)
        {
            bOnResponse?.Invoke(false);
            return;
        }

        // Create a blank canvas and justify center
        VisualElement blankCanvas = new VisualElement();
        blankCanvas.style.flexGrow = 1;
        blankCanvas.style.alignItems = Align.Center;
        blankCanvas.style.justifyContent = Justify.Center;

        m_BlankCanvasUI.rootVisualElement.Add(blankCanvas);
        blankCanvas.Add(continueDialogueInstance);

        continueMovementLabel.text = string.Format("The next tile will take considerably longer to travel ({0}d)!\nContinue?", actual);

        void CleanUp()
        {
            continueDialogueInstance.RemoveFromHierarchy();
            blankCanvas.RemoveFromHierarchy();
        }

        void YesButtonClicked()
        {
            bOnResponse?.Invoke(true);
            CleanUp();
        }

        void NoButtonClicked()
        {
            bOnResponse?.Invoke(false);
            CleanUp();
        }

        yesButton.clicked += YesButtonClicked;
        noButton.clicked += NoButtonClicked;
    }
}
