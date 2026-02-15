using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ClientScript : MonoBehaviour
{
    const string URL = "http://localhost:5005";

    private LoginHandler LoginHandler;
    
    [SerializeField] private TMP_InputField usernameInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private ChatHandler chatHandler;

    [SerializeField] private GameObject loginParent;//To disable upon login
    private void Start()
    {
        LoginHandler = new LoginHandler();
        LoginHandler.LoginSuccessful += LoginSuccessful;
    }

    public void Login()
    {
        LoginHandler.TryLogin(usernameInput.text, passwordInput.text);
    }

    private void LoginSuccessful()
    {
        loginParent.SetActive(false);
        chatHandler.Connect();
    }

    public void Register()
    {
        LoginHandler.TryRegister(usernameInput.text, passwordInput.text);
    }

    
}
