using UnityEngine;

public class TimeController : MonoBehaviour
{
    private float lastSpeed;

    void Start()
    {
        lastSpeed = 1f; // Default to normal speed
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Time.timeScale = 0.5f;
            lastSpeed = Time.timeScale;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Time.timeScale = 1f;
            lastSpeed = Time.timeScale;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Time.timeScale = 2f;
            lastSpeed = Time.timeScale;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Time.timeScale = 3f;
            lastSpeed = Time.timeScale;
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.timeScale == 0)
            {
                Time.timeScale = lastSpeed;
            }
            else
            {
                lastSpeed = Time.timeScale;
                Time.timeScale = 0;
            }
        }
    }
}
