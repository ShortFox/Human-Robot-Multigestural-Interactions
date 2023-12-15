using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventArguments : MonoBehaviour
{

    public delegate void TransformCallback(Transform obj);
    public event TransformCallback OnObjectEnabled;

    public delegate void TriggerCallback(Transform obj);
    public event TriggerCallback OnTouchTrigger;

    public delegate void Test(string test);
    public static event Test OnTest;

    private Vector3 _oldPos;

    public Vector3 DefaultPosition;

    private void Awake()
    {
        DefaultPosition = this.transform.localPosition;
        Debug.Log(name + " " + DefaultPosition);
    }
    private void Start()
    {
        _oldPos = this.transform.position;
    }
    private void OnEnable()
    {
        RaiseTransform(this.transform);
    }

    private void Update()
    {
        if (_oldPos != this.transform.position)
        {
            RaiseTransform(this.transform);
            _oldPos = this.transform.position;

            if (_oldPos == DefaultPosition) RaiseTransform(this.transform);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Finger")
        {
            OnTouchTrigger?.Invoke(this.transform);
        }
    }

    private void RaiseTransform(Transform obj)
    {
        OnTest?.Invoke("This test is working! I am in EventArguments.");
        OnObjectEnabled?.Invoke(obj);
    }
}
