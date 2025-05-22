using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

[FirestoreData]
public struct UserData
{
    [FirestoreProperty]
    public string username { get; set; }
    [FirestoreProperty]
    public string password { get; set; }
    [FirestoreProperty]
    public long id { get; set; }
    [FirestoreProperty]
    public int score { get; set; }
}
