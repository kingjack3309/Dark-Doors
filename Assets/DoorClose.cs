using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DoorClose : MonoBehaviour
{
    [SerializeField] GameObject closeDoor;

    [SerializeField] GameObject e;

    public NavMeshObstacle navObstacle;

    VisionCone visionCone;
    PlayerController playerController;

    private void Start()
    {
        visionCone = GameObject.FindGameObjectWithTag("Vision Cone").GetComponent<VisionCone>();
        playerController = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                StartCoroutine(closeDaDoor());
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

    IEnumerator closeDaDoor()
    {
        visionCone.canSee = false;
        playerController.speed = 0;
        yield return new WaitForSeconds(1.5f);
        playerController.speed = 7;
        visionCone.canSee = true; 
        navObstacle.enabled = true;
        closeDoor.SetActive(true);
        gameObject.SetActive(false);
    }
}
