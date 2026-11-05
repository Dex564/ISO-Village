using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IsometricPlayerController : MonoBehaviour
{

    // linter suggestion
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    private static readonly int YHash = Animator.StringToHash("Y");

    private static readonly int XHash = Animator.StringToHash("X");

    private Rigidbody2D rb;
    private Vector2 currentVelocity;
    private Vector2 inputVector;

    [Header("Player Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 30f;
    [SerializeField] private float stopThreshold = 0.1f;

    [Header("Player animator")]
    public Animator playerSpriteAnimator;

    private float lastX = 0f;
    private float lastY = -1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;

        inputVector = new Vector2(x, y).normalized;
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = inputVector * moveSpeed;
        float rate = inputVector.sqrMagnitude > 0f ? acceleration : deceleration;

        currentVelocity = Vector2.MoveTowards(currentVelocity, targetVelocity, rate * Time.fixedDeltaTime);

        if (currentVelocity.sqrMagnitude < stopThreshold * stopThreshold)
            currentVelocity = Vector2.zero;

        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);

        UpdateSpriteDirection(currentVelocity);
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        bool isMoving = direction.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            Vector2 dir = direction.normalized;
            lastX = dir.x > 0.5f ? 1 : dir.x < -0.5f ? -1 : 0;
            lastY = dir.y > 0.5f ? 1 : dir.y < -0.5f ? -1 : 0;
        }

        playerSpriteAnimator.SetFloat(XHash, lastX);
        playerSpriteAnimator.SetFloat(YHash, lastY);
        playerSpriteAnimator.SetBool(IsMovingHash, isMoving);
    }
}