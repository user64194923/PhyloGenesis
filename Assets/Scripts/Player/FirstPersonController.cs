using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float crouchTransitionSpeed = 5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private Transform cameraPitchHolder;
    [SerializeField] private Camera playerCamera;

    [Header("Camera FOV")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float runFOV = 80f;
    [SerializeField] private float fovTransitionSpeed = 10f;
    [SerializeField] private float tiltAmount = 5f;
    [SerializeField] private float tiltSpeed = 5f;

    [Header("Crouch Settings")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;

    [Header("Recoil")]
    [SerializeField] private float recoilReturnSpeed = 5f;
    [SerializeField] private float maxLookAngle = 90f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private float lastGroundedTime;
    private float groundCheckBuffer = 0.2f;

    private float xRotation;
    private float recoilPitch;
    private float recoilVelocity;
    private float speed;

    private PlayerSounds PlayerSounds;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;

        PlayerSounds = GetComponent<PlayerSounds>();
    }

    private void Update()
    {
        LookAround();
        HandleMovement();
        UpdateFOV();
        HandleTilt();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
    }

    private void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX); // Yaw

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        recoilPitch = Mathf.SmoothDamp(recoilPitch, 0f, ref recoilVelocity, 1f / recoilReturnSpeed);

        // Apply pitch + recoil
        cameraPitchHolder.localRotation = Quaternion.Euler(xRotation + recoilPitch, 0f, 0f);
    }

    public void ApplyRecoil(float amount)
    {
        recoilPitch -= amount;
    }

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded) lastGroundedTime = Time.time;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        if (Input.GetKey(KeyCode.LeftControl))
            Crouch();
        else
            StandUp();

        speed = isCrouching ? crouchSpeed :
                Input.GetKey(KeyCode.LeftShift) ? runSpeed :
                walkSpeed;

        controller.Move(move * speed * Time.deltaTime);

        if ((x != 0 || z != 0) && isGrounded && !isCrouching)
            PlayerSounds.isPlayingFootsteps = true;
        else
            PlayerSounds.isPlayingFootsteps = false;

        if (Input.GetKeyDown(KeyCode.Space) && (isGrounded || Time.time - lastGroundedTime < groundCheckBuffer))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            PlayerSounds.PlayRandomGearSound();
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    private void Crouch()
    {
        isCrouching = true;
        controller.height = Mathf.Lerp(controller.height, crouchHeight, crouchTransitionSpeed * Time.deltaTime);
        // controller.center = new Vector3(0, controller.height / 2f, 0); 
    }

    private void StandUp()
    {
        if (CanStandUp())
        {
            isCrouching = false;
            controller.height = standingHeight;
            // controller.center = new Vector3(0, controller.height / 2f, 0); 
        }
    }

    private bool CanStandUp()
    {
        float clearance = 0.1f;
        Vector3 top = transform.position + Vector3.up * controller.height;
        return !Physics.Raycast(top, Vector3.up, standingHeight - crouchHeight + clearance);
    }

    private void UpdateFOV()
    {
        Camera cam = playerCamera.GetComponent<Camera>();
        float targetFOV = (Input.GetKey(KeyCode.LeftShift) && !isCrouching) ? runFOV : normalFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    private void HandleTilt()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float targetZ = -horizontal * tiltAmount;

        Quaternion targetTilt = Quaternion.Euler(0f, 0f, targetZ);
        cameraHolder.localRotation = Quaternion.Slerp(cameraHolder.localRotation, targetTilt, tiltSpeed * Time.deltaTime);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        this.mouseSensitivity = sensitivity;
    }

    public void SetPlayerFOV()
    {
        if (PlayerPrefs.HasKey("FOV"))
        {
            normalFOV = PlayerPrefs.GetFloat("FOV");
            runFOV = normalFOV + 20f;
        }
    }
}