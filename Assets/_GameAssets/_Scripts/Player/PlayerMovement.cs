using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Mirror;
using UnityEngine.Rendering;

namespace HLProject.Characters
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : CachedNetTransform
    {
        [System.Flags] public enum InputFlag { Empty = 0b0, Crouch = 0b1, Jump = 0b10, Sprint = 0b100 }

        [SerializeField] float maxWalkSpeed, maxCrouchSpeed, crouchAmmount, maxRunSpeed, jumpHeight, horizontalSens, verticalSens, timeBetweenJumps;
        [SerializeField] float maxWeaponWeight, maxScopeSpeed, crouchingSpeed, spectatorSpeedMult = 1;
        [SerializeField] Vector2 cameraYLimits;
        [SerializeField] CinemachineVirtualCamera playerVCam;
        [SerializeField] Transform fakeCameraPivot;

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
                    //PovComponent.m_VerticalAxis.m_MaxSpeed = 0;
                    CmdSendPlayerInputs(Vector2.zero, Vector2.zero, 0x00);
                }
                else
                {
                    //PovComponent.m_VerticalAxis.m_MaxSpeed = 300;
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

        public float CameraXAxis => cameraRotInput.x;
        public bool PlayerIsMoving => playerMovInput.magnitude > 0;
        public bool PlayerIsRunning => canRun && CheckInput(inputFlags, InputFlag.Sprint);
        public bool PlayerIsCrouched => CheckInput(inputFlags, InputFlag.Crouch);

        public float CameraSensMult { get; set; }

        [SyncVar] bool onScope, canRun;
        [SyncVar] float currentWeaponWeight;
        [SyncVar] Vector3 shakeOffset, weaponRecoilOffset;

        bool jumped, canShakeCamera;
        InputFlag inputFlags;
        float startHeight, startYOffset, playerSpeed, yAxisRotation, shakeIntensity, shakeEndTime, targetHeight, startClientOffset, targetClientOffset;
        double jumpTimer;
        Quaternion currentShakeRotationTarget;
        Vector2 playerMovInput, lastPlayerMovInput, cameraRotInput, lastCameraRotInput;
        Vector3 velocity, shakeAmplitude, targetCenter;
        PlayerAnimationController animController;
        CinemachineTransposer clientVCamTransposer;

        void OnFreezePlayerSet(bool oldValue, bool newValue)
        {
            ToggleCharacterController(!newValue);
        }

        void OnSpectatorMovSet(bool oldValue, bool newValue)
        {
            velocity = Vector3.zero;
            ToggleCharacterController(!newValue);
        }

        void Start()
        {
            targetHeight = startHeight = CharCtrl.height;
            startYOffset = CharCtrl.center.y;
            targetCenter = CharCtrl.center;

            playerSpeed = maxWalkSpeed;
            CameraSensMult = 1;
            canRun = true;

            if (isLocalPlayer)
            {
                clientVCamTransposer = playerVCam.GetCinemachineComponent<CinemachineTransposer>();
                targetClientOffset = startClientOffset = clientVCamTransposer.m_FollowOffset.y;
            }

            if (!isServer) return;
            animController = GetComponent<PlayerAnimationController>();
            ToggleCharacterController(false);
        }

        void Update()
        {
            if (freezePlayer) return;
            if (isLocalPlayer)
            {
                CheckForInput();
                RotateClient();

                if (PlayerIsCrouched) targetClientOffset = startClientOffset - crouchAmmount;
                else targetClientOffset = startClientOffset;
                clientVCamTransposer.m_FollowOffset.y = Mathf.MoveTowards(clientVCamTransposer.m_FollowOffset.y, targetClientOffset, Time.deltaTime * crouchingSpeed);
            }

            if (!isServer) return;

            HandleShake();
            Move();
            RotateServer();
            Lean();
        }

        [Client]
        void CheckForInput()
        {
            if (FreezeInputs) return;
            playerMovInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            cameraRotInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

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

            if (newInput) CmdSendPlayerInputs(playerMovInput, cameraRotInput, inputFlags);
            lastCameraRotInput = cameraRotInput;
            lastPlayerMovInput = playerMovInput;
        }

        [Command]
        void CmdSendPlayerInputs(Vector2 movAxis, Vector2 rotAxis, InputFlag _inputFlags)
        {
            ProcessPlayerInputs(movAxis, rotAxis, _inputFlags);
        }

        [Server]
        public void ProcessPlayerInputs(Vector2 movAxis, Vector2 rotAxis, InputFlag _inputFlags)
        {
            movAxis.x = Mathf.Clamp(movAxis.x, -1, 1);
            movAxis.y = Mathf.Clamp(movAxis.y, -1, 1);
            rotAxis.x = Mathf.Clamp(rotAxis.x, -1, 1);

            playerMovInput = movAxis;
            if (animController != null) animController.SetPlayerDirectionMovement(playerMovInput);

            cameraRotInput = rotAxis;
            //xAxisRotaion = Mathf.Clamp(rotAxis.y, -70, 70);
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
                if (velocity.y < 0) velocity.y = -.5f;
                if (PlayerIsRunning) playerSpeed = maxRunSpeed;
            }

            if (onScope) playerSpeed = maxScopeSpeed;
            velocity = new Vector3(playerMovInput.x, velocity.y, playerMovInput.y);
            velocity = MyTransform.TransformDirection(velocity);

            if (CharCtrl.enabled) CharCtrl.Move(CalculateSpeedByWeight(playerSpeed) * Time.deltaTime * velocity);
        }

        void NoclipMovement()
        {
            if (CheckInput(inputFlags, InputFlag.Sprint)) playerSpeed = maxRunSpeed * spectatorSpeedMult;
            else playerSpeed = maxWalkSpeed * spectatorSpeedMult;

            velocity += fakeCameraPivot.forward * playerMovInput.y;
            velocity += fakeCameraPivot.right * playerMovInput.x;
            MyTransform.position += playerSpeed * Time.deltaTime * velocity;
        }

        void Jump()
        {
            if (spectatorMov)
            {
                velocity.y = CheckInput(inputFlags, InputFlag.Jump) ? spectatorSpeedMult : velocity.y;
                return;
            }

            if (onScope || NetworkTime.time < jumpTimer) return;

            if (CharCtrl.isGrounded)
            {
                if (jumped)
                {
                    jumped = false;
                    jumpTimer = NetworkTime.time + timeBetweenJumps;
                    animController.OnPlayerLands();
                }
                else if (CheckInput(inputFlags, InputFlag.Jump))
                {
                    LeanTween.delayedCall(.15f, () => velocity.y += Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y));
                    jumped = true;
                    jumpTimer = NetworkTime.time + .4f;
                    animController.OnPlayerJumps();
                }
            }

            velocity.y += Physics.gravity.y * Time.deltaTime;
            //if (CharCtrl.enabled) CharCtrl.Move(velocity * Time.deltaTime);
        }

        void Crouch()
        {
            if (spectatorMov)
            {
                velocity = Vector3.zero;
                if (CheckInput(inputFlags, InputFlag.Crouch)) velocity.y = -spectatorSpeedMult;
                return;
            }

            if (CheckInput(inputFlags, InputFlag.Crouch))
            {
                playerSpeed = maxCrouchSpeed;
                animController.TogglePlayerCrouch(true);

                targetHeight = startHeight - crouchAmmount;
                targetCenter.y = startYOffset - (crouchAmmount * .5f);
            }
            else if (playerSpeed != maxWalkSpeed)
            {
                animController.TogglePlayerCrouch(false);
                playerSpeed = maxWalkSpeed;

                targetHeight = startHeight;
                targetCenter.y = startYOffset;
            }

            CharCtrl.height = Mathf.MoveTowards(CharCtrl.height, targetHeight, Time.deltaTime * crouchingSpeed);
            CharCtrl.center = Vector3.MoveTowards(CharCtrl.center, targetCenter, Time.deltaTime * crouchingSpeed);
        }

        void Lean()
        {
            if (spectatorMov) return;
        }

        [Client]
        void RotateClient()
        {
            if (!isLocalPlayer) return;
            yAxisRotation += cameraRotInput.y * verticalSens * CameraSensMult * Time.deltaTime;
            yAxisRotation = Mathf.Clamp(yAxisRotation + shakeOffset.y + weaponRecoilOffset.y, cameraYLimits.x, cameraYLimits.y);

            playerVCam.transform.localEulerAngles = new Vector3(-yAxisRotation, 0, 0);
        }

        [Server]
        void RotateServer()
        {
            float rotationX = MyTransform.localEulerAngles.y + cameraRotInput.x * horizontalSens * CameraSensMult * Time.deltaTime;
            MyTransform.rotation = Quaternion.AngleAxis(rotationX + shakeOffset.x + weaponRecoilOffset.x, Vector3.up);

            yAxisRotation += cameraRotInput.y * verticalSens * Time.deltaTime;
            yAxisRotation = Mathf.Clamp(yAxisRotation + shakeOffset.y + weaponRecoilOffset.y, cameraYLimits.x, cameraYLimits.y);

            fakeCameraPivot.localEulerAngles = new Vector3(-yAxisRotation, 0, 0);
            //print(weaponRecoilOffset);
        }

        float CalculateSpeedByWeight(float maxSpeed) => (1 - (currentWeaponWeight / maxWeaponWeight)) * maxSpeed;

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

        [Server]
        public void ToggleScopeStatus(bool toggle) => onScope = toggle;

        [Server]
        public void GetCurrentWeaponWeight(float weight) => currentWeaponWeight = weight;

        [Server]
        public void TogglePlayerRunAbility(bool toggle) => canRun = toggle;

        [Client]
        public Volume GetLocalVolumeFromVCam() => playerVCam.GetComponentInChildren<Volume>();

        [Server]
        public void ShakeCamera(Vector3 amplitude, float intensity, float duration)
        {
            canShakeCamera = true;
            shakeIntensity = intensity;
            shakeAmplitude = amplitude;
            shakeEndTime = duration;
        }

        [Server]
        void HandleShake()
        {
            if (!canShakeCamera) return;
            shakeEndTime -= Time.deltaTime;

            if (shakeEndTime <= 0)
            {
                canShakeCamera = false;
                shakeOffset = Vector3.zero;
                return;
            }

            Vector3 noise = new Vector3(Random.Range(-.1f, .1f), Random.Range(-.1f, .1f), Random.Range(-.1f, .1f));
            shakeOffset = new Vector3(Mathf.Sin((float) shakeEndTime * shakeAmplitude.x * noise.x) * shakeIntensity, Mathf.Sin((float) shakeEndTime * shakeAmplitude.y * noise.y) * shakeIntensity, Mathf.Sin((float) shakeEndTime * shakeAmplitude.z * noise.z) * shakeIntensity);
        }

        [Server]
        public void IncreaseCameraRecoil(Vector3 ammount)
        {
            weaponRecoilOffset = ammount;
            RotateServer();
            //Debug.LogFormat("Increase! {0}", ammount);
        }

        [Server]
        public void ResetCameraRecoil()
        {
            weaponRecoilOffset = Vector3.zero;
            //print("Reset!");
        }
    }
}
