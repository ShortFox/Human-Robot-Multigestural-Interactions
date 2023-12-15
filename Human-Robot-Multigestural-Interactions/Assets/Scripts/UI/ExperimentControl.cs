using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MQ.MultiAgent.Box;

public class ExperimentControl : MonoBehaviour
{
    public BoxTask task;
    public Slider progressBar;
    public Text progressText;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        progressBar.value = (float)task.TrialNum / (float)task.ExperimentLen;
        progressText.text = $"{task.TrialNum}/{task.ExperimentLen}";
    }
}
