using UnityEngine;

public class CubeSpinning : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0, Time.deltaTime * 20, 0);
    }
}