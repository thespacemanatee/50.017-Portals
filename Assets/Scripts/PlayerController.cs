using UnityEngine;

public class PlayerController : PortalUser
{
    private const float RotationSmoothTime = 0.1f;
    private const float MoveSmoothTime = 0.1f;
    private const float MouseSensitivity = 10f;
    public float walkSpeed = 5;
    public float runSpeed = 10;
    public float jumpForce = 10;
    public float gravity = 20;

    private readonly Vector2 _pitchMinMax = new(-40, 85);

    private Camera _cam;
    private CharacterController _controller;
    private Vector3 _currentRotation;
    private bool _disabled;
    private bool _jumping;
    private float _lastGroundedTime;
    private float _pitch;
    private float _pitchSmoothV;
    private Vector3 _rotationSmoothVelocity;
    private float _smoothPitch;
    private Vector3 _smoothV;
    private float _smoothYaw;
    private Vector3 _velocity;
    private float _verticalVelocity;
    private float _yaw;
    private float _yawSmoothV;

    private void Start()
    {
        _cam = Camera.main;
        _controller = GetComponent<CharacterController>();
        _yaw = transform.eulerAngles.y;
        if (_cam != null) _pitch = _cam.transform.localEulerAngles.x;
        _smoothYaw = _yaw;
        _smoothPitch = _pitch;
    }

    private void Update()
    {
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        var inputDir = new Vector3(input.x, 0, input.y).normalized;
        var worldInputDir = transform.TransformDirection(inputDir);
        var currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        var targetVelocity = worldInputDir * currentSpeed;
        _velocity = Vector3.SmoothDamp(_velocity, targetVelocity, ref _smoothV, MoveSmoothTime);
        _verticalVelocity -= gravity * Time.deltaTime;
        _velocity = new Vector3(_velocity.x, _verticalVelocity, _velocity.z);

        var flags = _controller.Move(_velocity * Time.deltaTime);
        if (flags == CollisionFlags.Below)
        {
            _jumping = false;
            _lastGroundedTime = Time.time;
            _verticalVelocity = 0;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var timeSinceLastTouchedGround = Time.time - _lastGroundedTime;
            if (_controller.isGrounded || (!_jumping && timeSinceLastTouchedGround < 0.15f))
            {
                _jumping = true;
                _verticalVelocity = jumpForce;
            }
        }

        _yaw += Input.GetAxisRaw("Mouse X") * MouseSensitivity;
        _pitch -= Input.GetAxisRaw("Mouse Y") * MouseSensitivity;
        _pitch = Mathf.Clamp(_pitch, _pitchMinMax.x, _pitchMinMax.y);
        _smoothPitch = Mathf.SmoothDampAngle(_smoothPitch, _pitch, ref _pitchSmoothV, RotationSmoothTime);
        _smoothYaw = Mathf.SmoothDampAngle(_smoothYaw, _yaw, ref _yawSmoothV, RotationSmoothTime);
        _cam.transform.localEulerAngles = Vector3.right * _smoothPitch;
        transform.eulerAngles = Vector3.up * _smoothYaw;
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        var eulerRot = rot.eulerAngles;
        var delta = Mathf.DeltaAngle(_smoothYaw, eulerRot.y);
        _yaw += delta;
        _smoothYaw += delta;
        _velocity = toPortal.TransformVector(fromPortal.InverseTransformVector(_velocity));
        transform.eulerAngles = Vector3.up * _smoothYaw;
        Physics.SyncTransforms();
    }
}