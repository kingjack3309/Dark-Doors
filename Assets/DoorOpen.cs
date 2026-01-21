using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{

    [SerializeField] GameObject openDoor;

    private void OnTriggerStay2D()
    {
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            StartCoroutine(openDaDoor());
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            StopCoroutine(openDaDoor());
        }
    }

    IEnumerator openDaDoor()
    {
        yield return new WaitForSeconds(2);
        openDoor.SetActive(true);
        gameObject.SetActive(false);
    }

}
