using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header("enemy probablity between 1 & 0")]
    public float enemyPercentage = 0.5f;

    private float speed = 1.9f;

    Rigidbody2D rb;

    AudioSource audioSource;

    public bool canMove = true;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

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
            Vector2 playerPosition = other.gameObject.transform.position;

            Vector2 newPosition = Vector2.MoveTowards(gameObject.transform.position, playerPosition, speed * Time.deltaTime);
            
            rb.MovePosition(newPosition);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            
        }
    }
}
