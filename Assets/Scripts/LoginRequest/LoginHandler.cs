using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class LoginHandler
    {
        private string token;
        public event UnityAction LoginSuccessful;

        public async void TryLogin(string username, string password)
        {
            LoginDetails loginDetails = new LoginDetails(username, password);
            string json = JsonUtility.ToJson(loginDetails);

            var post = new UnityWebRequest($"{URLstorage.baseURL}/api/login/login", "POST");
            post.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            post.downloadHandler = new DownloadHandlerBuffer();
            post.SetRequestHeader("Content-Type", "application/json");

            await post.SendWebRequest();
            if (post.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Receieved key: " + post.downloadHandler.text);
                token = post.downloadHandler.text;
                PlayerPrefs.SetString($"LoginToken{username}", token);
                ClientSession.Instance?.SetSession(username, token);
                LoginSuccessful?.Invoke();
            }
            else
            {
                Debug.LogError("Login failed: " + post.error);
            }
        }
        public async void TryRegister(string username, string password)
        {
            LoginDetails loginDetails = new LoginDetails(username, password);
            string json = JsonUtility.ToJson(loginDetails);

            var post = new UnityWebRequest($"{URLstorage.baseURL}/api/login/register", "POST");
            post.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            post.downloadHandler = new DownloadHandlerBuffer();
            post.SetRequestHeader("Content-Type", "application/json");

            await post.SendWebRequest();
            if (post.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Registered Player: " + post.downloadHandler.text);
                TryLogin(username, password);
            }
            else
            {
                Debug.LogError("Login failed: " + post.error);
            }
        }

        
    }
}