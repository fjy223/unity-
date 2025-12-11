using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class APIManager : MonoBehaviour
{
    [Header("API 配置")]
    [SerializeField] private string apiEndpoint = "https://api.deepseek.com/chat/completions";
    [SerializeField] private string apiKey = "sk-f0cae472e3ca4236abbcecb8af4d3557";
    [SerializeField] private string model = "deepseek-chat";

    [Header("系统提示")]
    [TextArea(3, 5)]
    [SerializeField] private string systemPrompt = "你是一个电路模拟器软件的智能助手。请根据用户的问题，提供关于软件功能使用的帮助。回答要简洁明了，不超过200字。";

    /// <summary>
    /// 获取AI响应（异步）
    /// </summary>
    public void GetAIResponse(string userMessage, string topic, System.Action<string> callback)
    {
        StartCoroutine(GetAIResponseCoroutine(userMessage, topic, callback));
    }

    private IEnumerator GetAIResponseCoroutine(string userMessage, string topic, System.Action<string> callback)
    {
        // 构建请求体
        ChatRequest request = new ChatRequest
        {
            model = model,
            messages = new Message[]
            {
                new Message { role = "system", content = systemPrompt + $"\n当前话题: {topic}" },
                new Message { role = "user", content = userMessage }
            },
            temperature = 0.7f,
            max_tokens = 500
        };

        string jsonData = JsonUtility.ToJson(request);

        Debug.Log($"[API] 发送请求到: {apiEndpoint}");
        Debug.Log($"[API] 请求体: {jsonData}");

        using (UnityWebRequest webRequest = new UnityWebRequest(apiEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return webRequest.SendWebRequest();

            Debug.Log($"[API] 响应状态: {webRequest.responseCode}");
            Debug.Log($"[API] 响应文本: {webRequest.downloadHandler.text}");

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string responseText = webRequest.downloadHandler.text;
                string aiResponse = ParseResponse(responseText);

                Debug.Log($"[API] 解析后的回复: {aiResponse}");
                callback?.Invoke(aiResponse);
            }
            else
            {
                Debug.LogError($"[API] 请求失败 - 错误: {webRequest.error}");
                Debug.LogError($"[API] 响应代码: {webRequest.responseCode}");
                Debug.LogError($"[API] 响应文本: {webRequest.downloadHandler.text}");
                callback?.Invoke("API请求失败，请检查网络和API密钥");
            }
        }
    }

    /// <summary>
    /// 解析API响应
    /// </summary>
    private string ParseResponse(string responseJson)
    {
        try
        {
            ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseJson);

            if (response == null)
            {
                Debug.LogError("[API] 响应为null");
                return "解析失败：响应为空";
            }

            if (response.choices == null || response.choices.Length == 0)
            {
                Debug.LogError("[API] choices数组为空");
                return "解析失败：没有选择项";
            }

            string content = response.choices[0].message.content;
            Debug.Log($"[API] 成功解析内容: {content}");
            return content;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[API] 解析响应失败: {e.Message}\n{e.StackTrace}");
            return $"解析失败: {e.Message}";
        }
    }

    // ===== 序列化类 =====
    [System.Serializable]
    public class ChatRequest
    {
        public string model;
        public Message[] messages;
        public float temperature;
        public int max_tokens;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class ChatResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }
}
