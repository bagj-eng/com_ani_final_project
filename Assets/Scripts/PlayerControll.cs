using UnityEngine;

public class PlayerControll : MonoBehaviour
{
    public int hp = 100;
    public float moveSpeed = 5f;
    public float jumpPower = 7f;
    public float dashDistance = 2f;
   


    private Rigidbody2D rigid;
    private Animator animator;

    private float moveInput;
    private bool isGrounded = false;

    public BoxCollider2D attackCollider;

    public AttackHitbox attackHitbox;
    private DashAbility dashAbility;
    void Start()
    {
        dashAbility = GetComponent<DashAbility>();
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        attackCollider.enabled = false;
    }

    void Update()
    {
        moveInput = 0f;
        //캐릭터 좌우 움직이는 키 설정, 왼쪽 오른쪽 바라보도록 설정
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveInput = -1f;
            transform.localScale = new Vector3(-4, 4, 1);
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            moveInput = 1f;
            transform.localScale = new Vector3(4, 4, 1);

        }

        animator.SetBool("isRun", moveInput != 0f);

        //이중 점프를 막기위한 isjump 처리
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower);
            isGrounded = false;
            animator.SetBool("isJump", true);
        }

        // 공격 키 설정
        if (Input.GetKeyDown(KeyCode.A))
        {
            animator.SetTrigger("Attack");
        }

        
    }

    void FixedUpdate()
    {
        if (dashAbility != null && dashAbility.IsDashing())
        {
            return;
        }

        rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
    }

    //groound 태그에 닿았을때 
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = true;
            animator.SetBool("isJump", false);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            isGrounded = false;
        }
    }

    public void AttackStart()
    {
        attackHitbox.ResetHit();
        attackCollider.enabled = true;
    }

    public void AttackEnd()
    {
        attackCollider.enabled = false;
    }
    public void TakeDamage(int damage)
    {
        hp -= damage;

        Debug.Log($"플레이어 체력 : {hp}");

        if (hp <= 0)
        {

            animator.SetBool("isRun", false);
            animator.SetTrigger("Death");
            return;
        }

        animator.SetTrigger("Take_Hit");
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}