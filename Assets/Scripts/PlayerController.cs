using UnityEngine;

public class PlayerController : MonoBehaviour
{

    private float horizontal;
    private float vertical;
    private float speed = 7f;

    Rigidbody2D rb;

    private float smoothTime = 0.1f;
    private float currentVelocity = 0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        float smoothedAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.z,
            targetAngle,
            ref currentVelocity,
            smoothTime
        );

        transform.rotation = Quaternion.Euler(0, 0, smoothedAngle);

    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(speed * horizontal, speed * vertical);
    }

}