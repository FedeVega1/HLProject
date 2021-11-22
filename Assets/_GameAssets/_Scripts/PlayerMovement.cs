using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : CachedCharacterController
{
    [SerializeField] float maxWalkSpeed, maxCrouchSpeed, crouchAmmount, maxRunSpeed, jumpHeight, horizontalSens;

    public bool FreezePlayer { get; set; }

    float startHeight, playerSpeed;
    Vector3 velocity;

    void Start()
    {
        startHeight = CharCtrl.height;
        playerSpeed = maxWalkSpeed;
    }

    void Update()
    {
        if (FreezePlayer) return;
        Crouch();
        Move();
        Jump();
        Rotate();
        Lean();
    }

    void Move()
    {
        if (CharCtrl.isGrounded && velocity.y < 0) velocity.y = -.5f;
        if (Input.GetKey(KeyCode.LeftShift)) playerSpeed = maxRunSpeed * Time.deltaTime;

        velocity = new Vector3(Input.GetAxis("Horizontal"), velocity.y, Input.GetAxis("Vertical"));
        velocity = MyTransform.TransformDirection(velocity);

        CharCtrl.Move(velocity * playerSpeed * Time.deltaTime);
    }

    void Jump()
    {
        print(CharCtrl.isGrounded);
        if (Input.GetKeyDown(KeyCode.Space) && CharCtrl.isGrounded)
            velocity.y += Mathf.Sqrt(jumpHeight * -2 * Physics.gravity.y);

        velocity.y += Physics.gravity.y * Time.deltaTime;
        CharCtrl.Move(velocity * Time.deltaTime);
    }

    void Crouch()
    {
        float newHeight = startHeight;

        if (Input.GetKey(KeyCode.LeftControl))
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
        float rotation = MyTransform.localEulerAngles.y + Input.GetAxis("Mouse X") * horizontalSens * Time.deltaTime;
        MyTransform.rotation = Quaternion.AngleAxis(rotation, Vector3.up);
    }
}
