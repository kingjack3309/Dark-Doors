using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class DoorOpen : MonoBehaviour
{

    [SerializeField] GameObject openDoor;

    [SerializeField] GameObject e;

    public NavMeshObstacle navObstacle;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
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
        yield return new WaitForSeconds(1.5f);
        navObstacle.enabled = false;
        openDoor.SetActive(true);
        gameObject.SetActive(false);
    }

}
