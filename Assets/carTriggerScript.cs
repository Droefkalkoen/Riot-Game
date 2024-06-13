using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class carTriggerScript : MonoBehaviour
{
    private CarControl carControl;
    public bool frontTrigger;

    // Start is called before the first frame update
    void Start()
    {
        carControl = GetComponentInParent<CarControl>();
    }

    void OnTriggerEnter()
    {
        if (frontTrigger)
        {
            carControl.frontBlocked = true;
        }
        else
        {
            carControl.backBlocked = true;
        }
        
    }
}
