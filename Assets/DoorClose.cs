using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorClose : MonoBehaviour
{
    [SerializeField] GameObject closeDoor;

    [SerializeField] GameObject e;

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

    IEnumerator closeDaDoor()
    {
        yield return new WaitForSeconds(2);
        closeDoor.SetActive(true);
        gameObject.SetActive(false);
    }
}
