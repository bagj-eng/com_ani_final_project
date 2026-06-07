using UnityEngine;

public class DashAbility : MonoBehaviour
{
    public float dashSpeed = 15f;
    public float dashTime = 0.15f;
    public float cooldown = 3f;

    private float cooldownTimer = 0f;
    private Rigidbody2D rigid;
    private bool isDashing = false;
    private float dashTimer = 0f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;


    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.LeftShift) &&!isDashing && cooldownTimer <= 0f)
        {

            isDashing = true;
            dashTimer = dashTime;
            cooldownTimer = cooldown;

            spriteRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {

            float dir = transform.localScale.x > 0 ? 1f : -1f;

            rigid.linearVelocity = new Vector2(dir * dashSpeed, rigid.linearVelocity.y);

            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                spriteRenderer.color = originalColor;
            }
        }
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}