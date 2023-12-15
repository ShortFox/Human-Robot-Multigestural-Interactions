using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadBoxScene : MonoBehaviour
{
    [SerializeField] private string _nextScene;

    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            SceneLoader.Instance.LoadNewScene(_nextScene);
        }
    }
}
