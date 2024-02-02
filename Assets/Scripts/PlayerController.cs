using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerIO))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public PlayerIO PlayerIO { get; private set; }

    [SerializeField] private float walkSpeed = 6.0f;
    [SerializeField] private float runSpeed = 11.0f;
    [SerializeField] private bool limitDiagonalSpeed = true;
    [SerializeField] private bool toggleRun = false;
    [SerializeField] private float jumpSpeed = 8.0f;
    [SerializeField] private float gravity = 20.0f;
    [SerializeField] private float fallingThreshold = 10.0f;
    [SerializeField] private bool slideWhenOverSlopeLimit = false;
    [SerializeField] private bool slideOnTaggedObjects = false;
    [SerializeField] private float slideSpeed = 12.0f;
    [SerializeField] private bool airControl = false;
    [SerializeField] private float antiBumpFactor = 0.75f;
    [SerializeField] private int antiBunnyHopFactor = 1;
    [SerializeField] private float mouseSensitivity = 7.0f;
    [SerializeField] private Transform cameraTransform;
    private float verticalRotation = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool isGrounded = false;
    private CharacterController controller;
    private float speed;
    private float fallStartLevel;
    private bool isFalling;
    private float slideLimit;
    private bool playerControl = false;
    private int jumpTimer;
    private float rayDistance;
    public bool controlsEnabled = true;

    private void Awake()
    {
        Instance = this;
        PlayerIO = GetComponent<PlayerIO>();
        controller = GetComponent<CharacterController>();
        speed = walkSpeed;
        slideLimit = controller.slopeLimit - 0.1f;
        jumpTimer = antiBunnyHopFactor;
        rayDistance = controller.height * 0.5f + controller.radius;
    }

    private void Update()
    {
        if (!controlsEnabled || PlayerIO.inventory.activeSelf || PauseMenu.pauseMenu.paused)
        {
            SetCursorVisibilityAndLock(true);
            return;
        }

        SetCursorVisibilityAndLock(false);
        RotateView();
    }

    private void FixedUpdate()
    {
        if (!controlsEnabled || PlayerIO.inventory.activeSelf || PauseMenu.pauseMenu.paused) return;

        Vector2 input = GetInput();
        if (isGrounded)
        {
            HandleGroundedMovement(input);
            CheckSlideCondition();
        }
        else
        {
            HandleAirborneMovement(input);
        }
        ApplyGravity();
        UpdateGroundedStatus();
    }

    private void RotateView()
    {
        if (!cameraTransform) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * (PauseMenu.pauseMenu.invertMouse ? -1 : 1);

        verticalRotation = Mathf.Clamp(verticalRotation - mouseY, -90, 90);
        cameraTransform.localEulerAngles = new Vector3(verticalRotation, 0, 0);
        transform.Rotate(0, mouseX, 0);
    }

    private Vector2 GetInput()
    {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");
        float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && limitDiagonalSpeed) ? 0.7071f : 1.0f;
        return new Vector2(inputX * inputModifyFactor, inputY * inputModifyFactor);
    }

    private void HandleGroundedMovement(Vector2 input)
    {
        speed = toggleRun && Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        moveDirection = transform.TransformDirection(new Vector3(input.x, -antiBumpFactor, input.y)) * speed;
        if (Input.GetButton("Jump") && jumpTimer >= antiBunnyHopFactor)
        {
            moveDirection.y = jumpSpeed;
            jumpTimer = 0;
        }
        else jumpTimer++;
    }

    private void HandleAirborneMovement(Vector2 input)
    {
        if (airControl && playerControl)
        {
            moveDirection.x = input.x * speed;
            moveDirection.z = input.y * speed;
            moveDirection = transform.TransformDirection(moveDirection);
        }
    }

    private void ApplyGravity()
    {
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
    }

    private void UpdateGroundedStatus()
    {
        isGrounded = (controller.Move(moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
        if (!isGrounded && !isFalling)
        {
            isFalling = true;
            fallStartLevel = transform.position.y;
        }
        else if (isFalling && isGrounded)
        {
            isFalling = false;
            float fallDistance = fallStartLevel - transform.position.y;
            if (fallDistance > fallingThreshold) OnFell(fallDistance);
        }
    }

    private void CheckSlideCondition()
    {
        if (slideWhenOverSlopeLimit || slideOnTaggedObjects)
        {
            if (Physics.Raycast(transform.position, -Vector3.up, out RaycastHit hit, rayDistance))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (slopeAngle > slideLimit)
                {
                    // 如果是在斜坡上滑行
                    if (slideWhenOverSlopeLimit && slopeAngle > controller.slopeLimit)
                    {
                        Vector3 hitNormal = hit.normal;
                        moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                        Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                        moveDirection *= slideSpeed;
                        playerControl = false;
                    }
                    // 如果是在标记为"Slide"的对象上滑行
                    else if (slideOnTaggedObjects && hit.collider.CompareTag("Slide"))
                    {
                        Vector3 hitNormal = hit.normal;
                        moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                        Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                        moveDirection *= slideSpeed;
                        playerControl = false;
                    }
                }
            }
        }
    }


    private void SetCursorVisibilityAndLock(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void OnFell(float fallDistance)
    {
        // Assuming SoundManager and World are accessible static classes
        if (fallDistance >= 4 && fallDistance < 12)
            SoundManager.PlayAudio("fallsmall", 0.25f, Random.Range(0.9f, 1.1f));
        else if (fallDistance >= 12)
        {
            SoundManager.PlayAudio("fallbig", 0.25f, Random.Range(0.9f, 1.1f));
            World.Instance.SpawnLandParticles();
        }
    }
}
