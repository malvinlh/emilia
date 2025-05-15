using UnityEngine;
using TMPro;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    public GameObject userBubblePrefab;
    public GameObject aiBubblePrefab;
    public Transform chatContentParent; // Content under ScrollView
    public TMP_InputField inputField;

    private bool isAwaitingResponse = false;

    public void OnSendClicked()
    {
        string userInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(userInput) || isAwaitingResponse) return;

        inputField.text = "";
        CreateBubble(userInput, isUser: true);

        // Start AI turn
        StartCoroutine(HandleAITurn(userInput));
    }

    private void CreateBubble(string message, bool isUser)
    {
        GameObject prefab = isUser ? userBubblePrefab : aiBubblePrefab;
        GameObject bubbleGO = Instantiate(prefab, chatContentParent);

        ChatBubbleController controller = bubbleGO.GetComponent<ChatBubbleController>();
        controller.SetText(message); // Only sets text visually
    }

    private IEnumerator HandleAITurn(string userMessage)
    {
        isAwaitingResponse = true;

        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            CreateBubble(response, isUser: false);
            isAwaitingResponse = false;
        });
    }
}
