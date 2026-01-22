using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{

    [SerializeField] GameObject openDoor;

    [SerializeField] GameObject e;

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
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            e.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            e.SetActive(false);
        }
    }

    IEnumerator openDaDoor()
    {
        yield return new WaitForSeconds(2);
        openDoor.SetActive(true);
        gameObject.SetActive(false);
    }

}
