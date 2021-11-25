using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : CachedNetTransform
{
    [SerializeField] float maxWalkSpeed, maxCrouchSpeed, crouchAmmount, maxRunSpeed, jumpHeight, horizontalSens;
    [SerializeField] CinemachineVirtualCamera playerVCam;

    [SyncVar(hook = nameof(OnFreezePlayerSet))] public bool FreezePlayer;

    CharacterController _CharCtrl;
    CharacterController CharCtrl
    {
        get
        {
            if (_CharCtrl == null) _CharCtrl = GetComponent<CharacterController>();
            return _CharCtrl;
        }
    }

    BitFlag8 inputFlags;
    float startHeight, playerSpeed, cameraRotInput, lastCameraRotInput;
    Vector2 playerMovInput, lastPlayerMovInput;
    Vector3 velocity;

    CinemachinePOV _PovComponent;
    CinemachinePOV PovComponent
    {
        get
        {
            if (isLocalPlayer && _PovComponent == null) _PovComponent = playerVCam.GetCinemachineComponent<CinemachinePOV>();
            return _PovComponent;
        }
    }

    void OnFreezePlayerSet(bool oldValue, bool newValue)
    {
        if (isLocalPlayer) PovComponent.m_VerticalAxis.m_MaxSpeed = newValue ? 0 : 300;
    }

    void Start()
    {
        startHeight = CharCtrl.height;
        playerSpeed = maxWalkSpeed;
        CharCtrl.enabled = false;
    }

    void Update()
    {
        if (FreezePlayer) return;
        if (isLocalPlayer) CheckForInput();

        Move();
        Jump();
        Rotate();
        Lean();
    }

    void CheckForInput()
    {
        bool newInput = false;
        playerMovInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        cameraRotInput = Input.GetAxis("Mouse X");

        newInput = lastPlayerMovInput != playerMovInput || lastCameraRotInput != cameraRotInput;

        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            inputFlags += 0;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            inputFlags -= 0;
            newInput = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputFlags += 1;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            inputFlags -= 1;
            newInput = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            inputFlags += 2;
            newInput = true;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            inputFlags -= 2;
            newInput = true;
        }

        if (newInput) CmdSendPlayerInputs(playerMovInput, cameraRotInput, inputFlags.GetByte());
        lastCameraRotInput = cameraRotInput;
        lastPlayerMovInput = playerMovInput;
    }

    [Command]
    void CmdSendPlayerInputs(Vector2 movAxis, float rotAxis, byte _inputFlags)
    {
        playerMovInput = movAxis;
        cameraRotInput = rotAxis;
        inputFlags = new BitFlag8(_inputFlags);
    }

    void Move()
    {
        Crouch();

        if (CharCtrl.isGrounded && velocity.y < 0) velocity.y = -.5f;
        if (inputFlags == 2) playerSpeed = maxRunSpeed * Time.deltaTime;

        velocity = new Vector3(playerMovInput.x, velocity.y, playerMovInput.y);
        velocity = MyTransform.TransformDirection(velocity);

        if (CharCtrl.enabled) CharCtrl.Move(velocity * playerSpeed * Time.deltaTime);
    }

    void Jump()
    {
        if (inputFlags == 1 && CharCtrl.isGrounded)
            velocity.y += Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);

        velocity.y += Physics.gravity.y * Time.deltaTime;
        if (CharCtrl.enabled) CharCtrl.Move(velocity * Time.deltaTime);
    }

    void Crouch()
    {
        float newHeight = startHeight;

        if (inputFlags == 0)
        {
            playerSpeed = maxCrouchSpeed * Time.deltaTime;
            newHeight = crouchAmmount * startHeight;
        }
        else
        {
            playerSpeed = maxWalkSpeed * Time.deltaTime;
        }

        float lastHeight = CharCtrl.height;

        CharCtrl.height = Mathf.Lerp(CharCtrl.height, newHeight, 5.0f * Time.deltaTime);
        MyTransform.position += new Vector3(0, (CharCtrl.height - lastHeight) * crouchAmmount, 0);
    }

    void Lean()
    {

    }

    void Rotate()
    {
        float rotation = MyTransform.localEulerAngles.y + cameraRotInput * horizontalSens * Time.deltaTime;
        MyTransform.rotation = Quaternion.AngleAxis(rotation, Vector3.up);
    }

    public void ForceMoveCharacter(Vector3 pos, Quaternion rotation)
    {
        MyTransform.position = pos + Vector3.up;
        MyTransform.rotation = rotation;
        CharCtrl.enabled = true;
    }

    [TargetRpc]
    public void RpcToggleFreezePlayer(NetworkConnection target, bool toggle)
    {
        if (target.connectionId != connectionToServer.connectionId) return;
        FreezePlayer = toggle;
    }
}
