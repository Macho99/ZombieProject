using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class PlayerLocomotion : NetworkBehaviour
{
    public enum MovementType { Stand, Crouch, Size }


    private SimpleKCC simpleKCC;
    private BasicCamController camController;
    private float jumpForce;
    private float rotateXSpeed;
    private bool isJump;
    Animator animator;
    private CapsuleCollider myCollider;
    private MovementType movementType;
    public SimpleKCC Kcc { get { return simpleKCC; } }
    [Networked] public float fallingTime { get; private set; }
    [Networked] public float moveSpeed { get; private set; }
    [Networked] public Vector2 inputDirection { get; private set; }
    [Networked] public float jumpVelocity { get; private set; }
    [Networked] public Vector3 moveDirection { get; private set; }
    [Networked] public float CamerRotX { get; set; }

    private PlayerMove[] moves;
    public bool IsGround()
    {
        return simpleKCC.IsGrounded;
    }
    public bool HasJump()
    {
        return simpleKCC.HasJumped;
    }


    private void Awake()
    {
        myCollider = GetComponentInChildren<CapsuleCollider>();
        animator = GetComponent<Animator>();
        simpleKCC = GetComponent<SimpleKCC>();
        camController = GetComponentInChildren<BasicCamController>();
        jumpForce = 15f;
        rotateXSpeed = 30f;
        moves = new PlayerMove[(int)MovementType.Size];
        moves[(int)MovementType.Stand] = new PlayerStandMove(1.8f, 4f, 2f);
        moves[(int)MovementType.Crouch] = new PlayerCrouchMove(1f, 1f);
        movementType = MovementType.Stand;
    }
    public override void Spawned()
    {

        CamerRotX = camController.FollowTarget.eulerAngles.x;

        moveSpeed = 0f;
        simpleKCC.SetGravity(Physics.gravity.y * 2f);
        fallingTime = 0f;
    }
    public override void FixedUpdateNetwork()
    {
        if (IsGround())
            fallingTime = 0f;

        if (!IsGround())
            Debug.Log(Kcc.RealVelocity.y.ToString("F1"));
    }
    public override void Render()
    {
        camController.FollowTarget.localRotation = Quaternion.Euler(CamerRotX, 0f, 0f);
        animator.SetFloat("InputDirX", inputDirection.x, 0.05f, Time.deltaTime);
        animator.SetFloat("InputDirZ", inputDirection.y, 0.05f, Time.deltaTime);
        animator.SetFloat("MoveSpeed", moves[(int)movementType].MoveSpeed);
        animator.SetBool("IsGround", simpleKCC.IsGrounded);

    }
    public void Move()
    {

        simpleKCC.Move(moveDirection * moveSpeed, jumpVelocity);
        if (simpleKCC.HasJumped)
        {
            jumpVelocity = 0f;
            return;
        }
    }
    public void SetMove(NetworkInputData input)
    {
        if (!simpleKCC.IsGrounded)
            return;

        inputDirection = input.inputDirection;

        moveDirection = moves[(int)movementType].SetMove(transform, input);
        moveSpeed = moves[(int)movementType].MoveSpeed;
    }
    public void SetMove(Vector3 Direction)
    {

        moveDirection = moves[(int)movementType].SetMove(transform, Direction);
        moveSpeed = moves[(int)movementType].MoveSpeed;
    }


    public void Rotate(NetworkInputData input)
    {
        float rotY = input.mouseDelta.x * rotateXSpeed * Runner.DeltaTime;
        simpleKCC.AddLookRotation(new Vector2(0f, rotY));
        CamerRotX = camController.RotateX(input);
    }
    public void Rotate(Vector3 direction)
    {
        Quaternion newForward = Quaternion.RotateTowards(Quaternion.Euler(transform.forward), Quaternion.Euler(direction), Runner.DeltaTime);

        //simpleKCC.AddLookRotation(new Vector2(0f,));

        transform.rotation = newForward;
    }
    public void StopMove()
    {
        moveDirection = Vector3.zero;
        moves[(int)movementType].StopMove();
    }
    public void TriggerJump()
    {
        if (simpleKCC.IsGrounded)
        {
            jumpVelocity = jumpForce;
            isJump = true;

        }
    }
    public void ChangeMoveType(MovementType newMoveType)
    {
        this.movementType = newMoveType;

        if (movementType == MovementType.Stand)
        {
            animator.SetBool("IsCrouch", false);
        }
        else
        {
            animator.SetBool("IsCrouch", true);
        }

        simpleKCC.SetHeight(moves[(int)movementType].Height);
        StopMove();

    }
    public bool CanChanged(MovementType newMoveType)
    {
        if (this.movementType == newMoveType) return false;

        if (simpleKCC.IsGrounded == false) return false;


        if (newMoveType == MovementType.Stand)
        {
            float distance = moves[(int)newMoveType].Height - moves[(int)movementType].Height;

            Debug.DrawRay(transform.position + Vector3.up * moves[(int)movementType].Height, Vector3.up * distance, Color.red, 1f);
            if (Physics.Raycast(transform.position + Vector3.up * moves[(int)movementType].Height, Vector3.up, distance))
            {
                return false;
            }
            else
            {
                return true;

            }
        }



        return true;
    }
}
