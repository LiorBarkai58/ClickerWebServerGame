using UnityEngine;

public class ClientSession : MonoBehaviour
{
    public static ClientSession Instance { get; private set; }

    public string Username { get; private set; }
    public string Jwt { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSession( string username, string jwt)
    {
        Username = username;
        Jwt = jwt;
    }
}