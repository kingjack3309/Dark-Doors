using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header("enemy probablity between 1 & 0")]
    public float enemyPercentage = 0.5f;

    private float speed = 2f;

    // Start is called before the first frame update
    void Start()
    {
        bool isEnemy = (Random.value > enemyPercentage);

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
        //play noise
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Vector2 playerPosition = other.gameObject.transform.position;

            transform.position = Vector2.Lerp(gameObject.transform.position, playerPosition, speed * Time.deltaTime);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            
        }
    }
}
