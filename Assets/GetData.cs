using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase;
using UnityEngine.Assertions;

public class GetData : MonoBehaviour
{
    [SerializeField] public string path;
    [SerializeField] public TextMeshProUGUI username;
    [SerializeField] public TextMeshProUGUI password;
    [SerializeField] public TextMeshProUGUI id;
    [SerializeField] public TextMeshProUGUI score;

    public void GetUserData()
    {
        var firestore = FirebaseFirestore.DefaultInstance;
        firestore.Document(path).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            //Assert.IsNull(task.Exception);

            var data = task.Result.ConvertTo<UserData>();
            username.text = data.username;
            password.text = data.password;
            id.text = data.id.ToString();
            score.text = data.score.ToString();
        });
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
