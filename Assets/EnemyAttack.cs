using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyAttack : MonoBehaviour
{

    Slider healthBar;
    private bool canAttack = true;

    private void Start()
    {
        healthBar = GameObject.FindGameObjectWithTag("Healthbar").GetComponent<Slider>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && gameObject.GetComponentInParent<EnemyScript>().gameObject.tag == "Enemy")
        {
            canAttack = true;
            StartCoroutine(AttackPlayer());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && gameObject.GetComponentInParent<EnemyScript>().gameObject.tag == "Enemy")
        {
            canAttack= false;
        }
    }

    IEnumerator AttackPlayer()
    {
        healthBar.value -= 5;
        yield return new WaitForSeconds(0.3f);
        
        if (canAttack ) 
        { 
            StartCoroutine(AttackPlayer()); 
        }
    }
}
