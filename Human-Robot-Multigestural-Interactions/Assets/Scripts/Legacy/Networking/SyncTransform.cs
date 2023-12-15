using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SyncTransform : NetworkBehaviour
{
    [SyncVar] Vector3 Position;
    [SyncVar] Quaternion Rotation;

    // Start is called before the first frame update
    void Start()
    {
        syncInterval = 0f;  //Should send updates immediately.
    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            Position = this.transform.position;
            Rotation = this.transform.rotation;
        }
        if (!isServer)
        {
            this.transform.position = Position;
            this.transform.rotation = Rotation;
        }
    }
}
