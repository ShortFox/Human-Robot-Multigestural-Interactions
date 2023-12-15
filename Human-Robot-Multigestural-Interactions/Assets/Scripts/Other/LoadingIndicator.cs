using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingIndicator : MonoBehaviour
{
    public static LoadingIndicator Instance { get; private set; }

    Animator LoadingAnimator;

    void Awake()
    {
        Instance = this;
        LoadingAnimator = gameObject.GetComponent<Animator>();
    }

    public void SetAnimation(bool state)
    {
        LoadingAnimator.SetBool("Loading", state);
    }
}
