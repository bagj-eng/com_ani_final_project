using UnityEngine;

public class PlayerControll : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.5f;

    private bool isJumping = false;
    private float jumpTimer = 0f;
    private Vector3 startPosition;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();

        isJumping = false;
        jumpTimer = 0f;
        startPosition = transform.position;
    }

    void Update()
    {
        Vector2 moveDirection = Vector2.zero;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveDirection.x -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveDirection.x += 1f;
        }

        animator.SetBool("isRun", moveDirection.x != 0f);

        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            StartJump();
        }

        if (isJumping)
        {
            UpdateJump();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            animator.SetTrigger("Attack");
        }

        moveDirection = moveDirection.normalized;
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);
    }

    void StartJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        startPosition = transform.position;

        animator.SetBool("isJump", true);
    }

    void UpdateJump()
    {
        jumpTimer += Time.deltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress >= 1f)
        {
            transform.position = new Vector3(transform.position.x, startPosition.y, transform.position.z);

            isJumping = false;
            animator.SetBool("isJump", false);
        }
        else
        {
            float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = new Vector3(transform.position.x, startPosition.y + height, transform.position.z);
        }
    }
}