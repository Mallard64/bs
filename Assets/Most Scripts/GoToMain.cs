using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMain : MonoBehaviour
{
    // Start is called before the first frame update
    public void pressed()
    {
        SceneManager.LoadScene("New Scene");
    }
}
