using System.Collections;
using TMPro;
using UnityEngine;

public class LoadingMessage : MonoBehaviour
{
    string _baseMessage;
    int _showRequests;

    public TextMeshPro FrontText;
    public TextMeshPro BackText;

    // Start is called before the first frame update
    void Awake()
    {
        gameObject.SetActive(false);
        _baseMessage = FrontText.text;
        _showRequests = 0;
    }

    IEnumerator WorkingCycle()
    {
        var i = 0;
        while (true)
        {
            i = (i + 1) % 3;
            var newText = _baseMessage + new string('.', i);
            FrontText.text = newText;
            BackText.text = newText;
            yield return new WaitForSeconds(1);
        }
    }

    public void ShowMessage()
    {
        _showRequests++;
        if (_showRequests > 1)
        {
            return;
        }
        StartCoroutine(WorkingCycle());
        gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        _showRequests--;
        if (_showRequests > 0)
        {
            return;
        }
        StopCoroutine(WorkingCycle());
        gameObject.SetActive(false);
    }

}
