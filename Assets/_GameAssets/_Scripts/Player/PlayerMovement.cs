using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : CachedNetTransform
{
    [System.Flags] enum InputFlag { Empty = 0b0, Crouch = 0b1, Jump = 0b10, Sprint = 0b100 }

    [SerializeField] float maxWalkSpeed, maxCrouchSpeed, crouchAmmount, maxRunSpeed, jumpHeight, horizontalSens, verticalSens, timeBetweenJumps;
    [SerializeField] CinemachineVirtualCamera playerVCam;

    [SyncVar(hook = nameof(OnFreezePlayerSet))] public bool freezePlayer;
    [SyncVar(hook = nameof(OnSpectatorMovSet))] public bool spectatorMov;

    bool _FreezeInputs;
    public bool FreezeInputs
    {
        get => _FreezeInputs;

        set
        {
            _FreezeInputs = value;

            if (value)
            {
                PovComponent.m_VerticalAxis.m_MaxSpeed = 0;
                CmdSendPlayerInputs(Vector2.zero, Vector2.zero, 0x00);
            }
            else
            {
                PovComponent.m_VerticalAxis.m_MaxSpeed = 300;
            }
        }
    }

    CharacterController _CharCtrl;
    CharacterController CharCtrl
    {
        get
        {
            if (_CharCtrl == null) _CharCtrl = GetComponent<CharacterController>();
            return _CharCtrl;
        }
    }

    CinemachinePOV _PovComponent;
    CinemachinePOV PovComponent
    {
        get
        {
            if (isLocalPlayer && _PovComponent == null) _PovComponent = playerVCam.GetCinemachineComponent<CinemachinePOV>();
            return _PovComponent;
        }
    }

    public float CameraXAxis => xAxisRotaion;
    public bool PlayerIsMoving => playerMovInput.magnitude > 0;
    public bool PlayerIsRunning => CheckInput(inputFlags, InputFlag.Sprint);

    bool jumped;
    InputFlag inputFlags;
    float startHeight, playerSpeed, cameraRotInput, lastCameraRotInput, xAxisRotaion;
    double jumpTimer;
    Vector2 playerMovInput, lastPlayerMovInput;
    Vector3 velocity;

    void OnFreezePlayerSet(bool oldValue, bool newValue)
    {
        if (isLocalPlayer) PovComponent.m_VerticalAxis.m_MaxSpeed = newValue ? 0 : 300;
        ToggleCharacterController(!newValue);
    }

    void OnSpectatorMovSet(bool oldValue, bool newValue)
    {
        velocity = Vector3.zero;
        ToggleCharacterController(!newValue);
    }

    void Start()
    {
        startHeight = CharCtrl.height;
        playerSpeed = maxWalkSpeed;
        ToggleCharacterController(false);
    }

    void Update()
    {
        if (freezePlayer) return;
        if (isLocalPlayer) CheckForInput();

        if (!isServer) return;

        Move();
        Rotate();
        Lean();
    }

    [Client]
    void CheckForInput()
    {
        if (FreezeInputs) return;
        playerMovInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        cameraRotInput = Input.GetAxis("Mouse X");

        bool newInput = lastPlayerMovInput != playerMovInput || lastCameraRotInput != cameraRotInput;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            inputFlags |= InputFlag.Crouch;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            inputFlags ^= InputFlag.Crouch;
            newInput = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputFlags |= InputFlag.Jump;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            inputFlags ^= InputFlag.Jump;
            newInput = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            inputFlags |= InputFlag.Sprint;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            inputFlags ^= InputFlag.Sprint;
            newInput = true;
        }

        if (newInput) CmdSendPlayerInputs(playerMovInput, new Vector2(cameraRotInput, Input.GetAxis("Mouse Y")), inputFlags);
        lastCameraRotInput = cameraRotInput;
        lastPlayerMovInput = playerMovInput;
    }

    [Command]
    void CmdSendPlayerInputs(Vector2 movAxis, Vector2 rotAxis, InputFlag _inputFlags)
    {
        movAxis.x = Mathf.Clamp(movAxis.x, -1, 1);
        movAxis.y = Mathf.Clamp(movAxis.y, -1, 1);
        rotAxis.x = Mathf.Clamp(rotAxis.x, -1, 1);

        playerMovInput = movAxis;
        cameraRotInput = rotAxis.x;
        xAxisRotaion = Mathf.Clamp(rotAxis.y, -70, 70);
        inputFlags = _inputFlags;
    }

    void Move()
    {
        Crouch();
        Jump();

        if (spectatorMov)
        {
            NoclipMovement();
            return;
        }

        if (CharCtrl.isGrounded)
        {
            if (jumped)
            {
                jumped = false;
                jumpTimer = NetworkTime.time + timeBetweenJumps;
            }

            if (velocity.y < 0) velocity.y = -.5f;
            if (CheckInput(inputFlags, InputFlag.Sprint)) playerSpeed = maxRunSpeed;
        }

        velocity = new Vector3(playerMovInput.x, velocity.y, playerMovInput.y);
        velocity = MyTransform.TransformDirection(velocity);

        if (CharCtrl.enabled) CharCtrl.Move(playerSpeed * Time.deltaTime * velocity);
    }

    // TODO: Fix spectator camera movement
    void NoclipMovement()
    {
        if (CheckInput(inputFlags, InputFlag.Sprint)) playerSpeed = maxRunSpeed * Time.deltaTime;
        else playerSpeed = maxWalkSpeed;

        velocity = new Vector3(playerVCam.transform.forward.x * playerMovInput.x, playerVCam.transform.forward.y * velocity.y, playerVCam.transform.forward.z * playerMovInput.y);
        MyTransform.position += playerSpeed * Time.deltaTime * velocity;
    }

    void Jump()
    {
        if (spectatorMov)
        {
            velocity.y = CheckInput(inputFlags, InputFlag.Jump) ? 1 : velocity.y;
            return;
        }

        if (NetworkTime.time < jumpTimer) return;
        if (CheckInput(inputFlags, InputFlag.Jump) && CharCtrl.isGrounded)
        {
            velocity.y += Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);
            jumped = true;
        }

        velocity.y += Physics.gravity.y * Time.deltaTime;
        //if (CharCtrl.enabled) CharCtrl.Move(velocity * Time.deltaTime);
    }

    void Crouch()
    {
        if (spectatorMov)
        {
            velocity.y = CheckInput(inputFlags, InputFlag.Crouch) ? -1 : velocity.y;
            return;
        }

        float newHeight = startHeight;

        if (CheckInput(inputFlags, InputFlag.Crouch))
        {
            playerSpeed = maxCrouchSpeed;
            newHeight = crouchAmmount * startHeight;
        }
        else
        {
            playerSpeed = maxWalkSpeed;
        }

        float lastHeight = CharCtrl.height;

        CharCtrl.height = Mathf.Lerp(CharCtrl.height, newHeight, 5.0f * Time.deltaTime);
        MyTransform.position += new Vector3(0, (CharCtrl.height - lastHeight) * crouchAmmount, 0);
    }

    void Lean()
    {
        if (spectatorMov) return;
    }

    void Rotate()
    {
        float rotationX = MyTransform.localEulerAngles.y + cameraRotInput * horizontalSens * Time.deltaTime;
        MyTransform.rotation = Quaternion.AngleAxis(rotationX, Vector3.up);
    }

    [Server]
    public void ForceMoveCharacter(Vector3 pos, Quaternion rotation)
    {
        MyTransform.position = pos + Vector3.up;
        MyTransform.rotation = rotation;
        CharCtrl.enabled = true;
    }

    [Server]
    public void ToggleCharacterController(bool toggle) => CharCtrl.enabled = toggle;

    bool CheckInput(InputFlag flag, InputFlag inputToCheck) => (flag & inputToCheck) == inputToCheck;
}
