using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OnDeathScript : MonoBehaviour
{

    Slider healthBar;

    private void Start()
    {
        healthBar = GetComponent<Slider>();
    }

    public void YouDiedMonkey()
    {
        if (healthBar.value <= 0)
        {

            SceneManager.LoadScene("DeathScreen");

        }
    }

}
