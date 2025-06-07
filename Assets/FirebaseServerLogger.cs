using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

public class FirebaseServerLogger : MonoBehaviour
{
    private FirebaseFirestore db;
    private string serverIP;

    public void Awake()
    {
        // Get the server's IP address
        serverIP = GetServerIP();

        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("[IPLogger] Firebase initialized");

                // Log this IP
                LogServerIP();
            }
            else
            {
                Debug.LogError($"[IPLogger] Firebase failed: {task.Result}");
            }
        });
    }

    private string GetServerIP()
    {
        // Try to get external IP first (for dedicated servers)
        string ip = Environment.GetEnvironmentVariable("SERVER_IP");

        if (string.IsNullOrEmpty(ip))
        {
            // Fallback to network address
            var networkManager = GetComponent<NetworkManager>();
            ip = networkManager.networkAddress;
        }

        // If still local, try to get public IP (you'd need to implement this)
        if (ip == "localhost" || ip.StartsWith("192.168") || ip.StartsWith("10."))
        {
            ip = "local_" + SystemInfo.deviceUniqueIdentifier.Substring(0, 8);
        }

        return ip;
    }

    private void LogServerIP()
    {
        // Reference to the IP list document
        var ipListRef = db.Collection("server_data").Document("ip_list");

        // Get current list
        ipListRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                var snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // Document exists, update the array
                    ipListRef.UpdateAsync("ips", FieldValue.ArrayUnion(serverIP))
                        .ContinueWithOnMainThread(updateTask =>
                        {
                            if (updateTask.IsCompletedSuccessfully)
                            {
                                Debug.Log($"[IPLogger] Added IP to list: {serverIP}");
                            }
                            else
                            {
                                Debug.LogError($"[IPLogger] Failed to update: {updateTask.Exception}");
                            }
                        });
                }
                else
                {
                    // Document doesn't exist, create it
                    var data = new Dictionary<string, object>
                    {
                        ["ips"] = new List<string> { serverIP },
                        ["created"] = Timestamp.GetCurrentTimestamp()
                    };

                    ipListRef.SetAsync(data).ContinueWithOnMainThread(setTask =>
                    {
                        if (setTask.IsCompletedSuccessfully)
                        {
                            Debug.Log($"[IPLogger] Created IP list with: {serverIP}");
                        }
                        else
                        {
                            Debug.LogError($"[IPLogger] Failed to create: {setTask.Exception}");
                        }
                    });
                }
            }
            else
            {
                Debug.LogError($"[IPLogger] Failed to get document: {task.Exception}");
            }
        });

        // Also log with timestamp for history
        var logData = new Dictionary<string, object>
        {
            ["ip"] = serverIP,
            ["timestamp"] = Timestamp.GetCurrentTimestamp(),
            ["platform"] = Application.platform.ToString()
        };

        db.Collection("ip_logs").AddAsync(logData);
    }
}