using System.Collections;
using System.Collections.Generic;
using JFramework.Net;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkWriter writer = new NetworkWriter();
        int bytes = 123;
        writer.WriteInt(bytes);
        Debug.Log(writer);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
