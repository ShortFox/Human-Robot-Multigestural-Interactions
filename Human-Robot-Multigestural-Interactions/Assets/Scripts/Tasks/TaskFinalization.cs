using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskFinalization : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(FinalizeExperiment());
    }

    IEnumerator FinalizeExperiment()
    {
        // Wait a couple of seconds and disable partner.
        yield return new WaitForSeconds(2);
        AvatarInfo.PartnerPlayer.SetActive(false);
    }

}
