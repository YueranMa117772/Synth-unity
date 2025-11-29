using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turner : MonoBehaviour
{
    public float rate = 1;
    public float amount = 80;

    Vector3 localAngles;

    // Start is called before the first frame update
    void Start()
    {
        localAngles = transform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        localAngles.y = amount * Mathf.Sin(rate * Time.time);
        transform.localEulerAngles = localAngles;
    }
}
