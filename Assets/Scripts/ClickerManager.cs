using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class ClickerManager : MonoBehaviour
    {
        
        const string PlayerRequestUrl = "http://localhost:5006/api/Player/";
        
        private int clickCount = 0;
        [SerializeField] private TextMeshProUGUI textCount;
        [SerializeField] private TMP_InputField nameInput;
        
        
        public void OnClick()
        {
            clickCount++;
            textCount.SetText(clickCount.ToString());
        }
        
        public void TryGetClickerName()
        {
            StartCoroutine(RequestClicker());
        }

        private IEnumerator RequestClicker()
        {
            string name = nameInput.text;

            using var get = UnityWebRequest.Get($"http://localhost:5006/api/player/{name}");
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Found player: " + get.downloadHandler.text);
                Player player = JsonConvert.DeserializeObject<Player>(get.downloadHandler.text);
                clickCount = player.NumberOfClicks;
                textCount.SetText(clickCount.ToString());
                yield break;
            }

            if (get.responseCode != 404)
            {
                Debug.LogError("GET failed: " + get.error);
                yield break;
            }

            var dto = new Player(name, clickCount, Random.Range(0, 10), Random.Range(0, 10));
            string json = JsonUtility.ToJson(dto);

            var post = new UnityWebRequest("http://localhost:5006/api/player", "POST");
            post.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            post.downloadHandler = new DownloadHandlerBuffer();
            post.SetRequestHeader("Content-Type", "application/json");

            yield return post.SendWebRequest();

            if (post.result != UnityWebRequest.Result.Success)
                Debug.LogError("POST failed: " + post.error + " " + post.downloadHandler.text);
        }

        public void PrintLeaderboard()
        {
            StartCoroutine(printLeaderboard());
        }
        private IEnumerator printLeaderboard()
        {
            using var get = UnityWebRequest.Get($"http://localhost:5006/api/leaderboard");
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success)
            {
                List<Player> players = JsonConvert.DeserializeObject<List<Player>>(get.downloadHandler.text);
                foreach (Player player in players )
                {
                    print($"Player: {player.Name} \n Matches: {player.MatchesWon}");
                }

            }
        }

        public void RandomizePlayerData()
        {
            StartCoroutine(RandomizePlayerDataRequest());
        }
        private IEnumerator RandomizePlayerDataRequest()
        {
            string playerName = nameInput.text;
            
            using var get = UnityWebRequest.Get($"http://localhost:5006/api/player/{playerName}");
            yield return get.SendWebRequest();

            if (get.result == UnityWebRequest.Result.Success)
            {
                Player player = JsonConvert.DeserializeObject<Player>(get.downloadHandler.text);
                
                var dto = new Player(player.Name, player.NumberOfClicks, Random.Range(0, 10), Random.Range(0, 10));
                string json = JsonUtility.ToJson(dto);

                var post = new UnityWebRequest("http://localhost:5006/api/player", "POST");
                post.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                post.downloadHandler = new DownloadHandlerBuffer();
                post.SetRequestHeader("Content-Type", "application/json");

                Debug.Log("randomized player data");
                
                yield return post.SendWebRequest();

                if (post.result != UnityWebRequest.Result.Success)
                    Debug.LogError("POST failed: " + post.error + " " + post.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Player not found, create player first");
            }
        }
        
     }
}