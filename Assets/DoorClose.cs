using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorClose : MonoBehaviour
{
    [SerializeField] GameObject closeDoor;

    private void OnTriggerStay2D()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(closeDaDoor());
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            StopCoroutine(closeDaDoor());
        }
    }

    IEnumerator closeDaDoor()
    {
        yield return new WaitForSeconds(2);
        closeDoor.SetActive(true);
        gameObject.SetActive(false);
    }
}
