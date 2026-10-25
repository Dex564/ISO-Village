using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class IsometricPlayerController : MonoBehaviour
{
    [Header("Player Settings")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 inputVector;
    private PlayerControls controls;

    [Header("Player Input Settings")]
    public bool isoMovement;
    public Vector2 isoRight = new Vector2(1, 0.5f);
    public Vector2 isoUp = new Vector2(-1, 0.5f);

    [Header("8-directional Sprite")]
    public SpriteRenderer spriteRenderer;

    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRigth;
    public Sprite spriteUpLeft;
    public Sprite spriteUpRigth;
    public Sprite spriteDownLeft;
    public Sprite spriteDownRigth;

    [Header("Directional Movement")]
    public Animator playerSpriteAnimator;

    private float lastX = 0f;
    private float lastY = -1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Player.Move.performed += ctx => inputVector = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => inputVector = Vector2.zero;
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void FixedUpdate()
    {
        if (!isoMovement)
        {
            Vector2 moveDir = inputVector.normalized;
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
        else
        {
            Vector2 isoDirection = (isoRight * inputVector.x + isoUp * inputVector.y).normalized;
            rb.MovePosition(rb.position + isoDirection * moveSpeed * Time.fixedDeltaTime);
        }

        UpdateSpriteDirection(inputVector);
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        direction.Normalize();

        if (direction.sqrMagnitude > 0.01f)
        {
            lastX = (direction.x > 0 ? (direction.x > 0.5f ? 1 : 0) : (direction.x < -0.5f ? -1 : 0));
            lastY = (direction.y > 0 ? (direction.y > 0.5f ? 1 : 0) : (direction.y < -0.5f ? -1 : 0));
        }

        playerSpriteAnimator.SetFloat("X", lastX);
        playerSpriteAnimator.SetFloat("Y", lastY);

        playerSpriteAnimator.SetBool("IsMoving", direction.sqrMagnitude > 0.01f);
    }
}
