using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using TMPro;

public class SetData : MonoBehaviour
{
    [SerializeField] public string path = "data_sheets/thatguy";
    [SerializeField] public TMP_InputField user;
    [SerializeField] public TMP_InputField pw;
    [SerializeField] public TMP_InputField id;
    [SerializeField] public TMP_InputField score;
    [SerializeField] public Button submitButton;

    // Start is called before the first frame update
    void Start()
    {
        submitButton.onClick.AddListener(() =>
        {
            var data = new UserData
            {
                username = user.text,
                password = pw.text,
                id = long.Parse(id.text),
                score = int.Parse(score.text)
            };
            var firestore = FirebaseFirestore.DefaultInstance;
            firestore.Document(path).SetAsync(data);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
