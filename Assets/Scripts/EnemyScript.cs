using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyScript : MonoBehaviour
{
    [Header("enemy probablity between 1 & 0")]
    public float enemyPercentage = 0.5f;

    AudioSource audioSource;

    NavMeshAgent agent;

    GameObject target;

    public bool canMove = true;

    [HideInInspector]
    public bool isSeen = false;

    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        audioSource = GetComponent<AudioSource>();

        bool isEnemy = Random.value < enemyPercentage;

        if (isEnemy)
        {
            gameObject.tag = "Enemy";
        }
        else
        {
            gameObject.tag = "Statue";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && canMove && (gameObject.tag == "Enemy"))
        {
            audioSource.Play();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && canMove && (gameObject.tag == "Enemy"))
        {
            if (!isSeen)
            {
                agent.SetDestination(target.transform.position);
            }
        }
    }
}
