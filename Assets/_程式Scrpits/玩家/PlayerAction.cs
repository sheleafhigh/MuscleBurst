﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAction : MonoBehaviour
{

    [Header("移動速度:"), SerializeField]
    private float speed = 0.25f;
    [Header("最大跳躍力:"), SerializeField]
    private float maxJumpPower = 15;
    [Header("最小跳躍力:"), SerializeField]
    private float minJumpPower = 2;
    [Header("跳躍最大高度:"), SerializeField]
    private float maxHigh = 5.5f;
    [Header("二段跳權限:"), SerializeField]
    private bool doubleJump;
    [Header("閃躲移動距離:"), SerializeField]
    private float dodgeDis;
    [Header("腳底Transform:"), SerializeField]
    private Transform buttomPos;
    [Header("腳底Collider:"), SerializeField]
    private BoxCollider2D sole;
    

    private Rigidbody2D rd2D;
    private Animator animator;
    private Collider2D platform;
    public bool jumping,maxHeight,canJumpDown,squat,attacking;//跳躍狀態，抵達跳躍最大高度，在平台上可下躍狀態，蹲下狀態，攻擊中狀態
    public bool controlLock,moveLock;//控制鎖，移動鎖
    private float lookX;
    private float FightWaitTime;//進入戰鬥狀態後的等待時間
    private float starthigh, jumphigh,nowhigh; //起跳位置 相差高度 現在高度


    #region 內建方法
    void Start()
    {
        lookX = transform.localScale.x;//存取自身的scale
        animator = GetComponent<Animator>();//存取Animator
        rd2D = GetComponent<Rigidbody2D>();//存取Rigidbody2D
    }
    
    void Update()
    {
        Move();
        //Debug.Log(rd2D.velocity.x);
        Squat();
        //Debug.Log(jumping);
        if (animator.GetBool("戰鬥待機"))
        {
            FightWaitTime += Time.deltaTime;
            if (FightWaitTime >= 3)//在戰鬥狀態待機超過3秒，則恢復一般站立動作
            {
                Fighting(1);
            }
        }
        //if (!Input.GetKey(KeyCode.S))
        //{
        //    animator.SetBool("蹲下", false);
        //}
        //Debug.Log(FightWaitTime);
    }
    private void FixedUpdate()
    {
        
        Idle();    
        Jump();

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Debug.Log("down");
        if (!sole.IsTouching(collision.collider)) { return; }
        if (collision.gameObject.CompareTag("地面") || collision.gameObject.CompareTag("可下躍平台"))
        {
            float offset = collision.collider.bounds.size.y / 2 - 0.2f;
            float collisionY = collision.transform.position.y;
            //Debug.Log(offset);
            //Debug.Log("腳底的Y"+buttomPos.position.y);
            //Debug.Log("碰撞到的Y" +( collisionY + offset));
            //Debug.Log(buttomPos.position.y > collision.transform.position.y + offset);
            if (buttomPos.position.y > collision.transform.position.y + offset) 
            {
                
                animator.SetBool("跳躍", false);
                animator.SetBool("落下", false); 
                jumping = false;
                maxHeight = false;

                starthigh = rd2D.transform.position.y; //重設起跳位置
                nowhigh = starthigh; //重設當前高度
                jumphigh = 0; //重設高度

                controlLock = false;

                Invoke("SoleUnShow", 0.2f);
            }
            
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("可下躍平台") )
        {
           
            platform = collision.collider;
            
            if (animator.GetBool("蹲下"))
            {
                canJumpDown = true;
            }
        }
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("可下躍平台")) 
        {
            platform = null;
            canJumpDown = false;
        } 
    }

    #endregion
    //控制鎖時間 輕攻擊0.05s，重攻擊0.3s，閃躲0.3s
    #region 玩家動作
    private void Idle()
    {
        if (rd2D.velocity.x == 0)//停止移動就回到站立動畫
        {
            animator.SetBool("跑步", false);
        }
    }
    private void Move()
    {
        if (moveLock) return;
        if (Input.GetKey(KeyCode.D))
        {
            Fighting(1);
            animator.SetBool("跑步", true);
            transform.Translate(transform.right * speed);
            //rd2D.AddForce(transform.right * speed);
            transform.localScale = new Vector2(lookX, 0.25f);//角色面向右
        }
        if (Input.GetKey(KeyCode.A))
        {
            Fighting(1);
            animator.SetBool("跑步", true);
            transform.Translate(transform.right * -speed);
            //rd2D.AddForce(transform.right * -speed);
            transform.localScale = new Vector2(-lookX, 0.25f);//角色面向左
        }
       
    }
    private void Squat()
    {
        if (jumping) return;
        if (squat)
        {
            animator.SetBool("蹲下", true);
            moveLock = true;
            if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector2(lookX, 0.25f);//角色面向右;
            else if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector2(-lookX, 0.25f);//角色面向左
        }   
        else if (Input.GetKey(KeyCode.S))
        {
            if (controlLock) return;
            animator.SetBool("蹲下", true);
            Fighting(1);
            moveLock = true;
            
            if (Input.GetKey(KeyCode.D)) transform.localScale = new Vector2(lookX, 0.25f);//角色面向右;
            else if (Input.GetKey(KeyCode.A)) transform.localScale = new Vector2(-lookX, 0.25f);//角色面向左
        }

        else if (Input.GetKeyUp(KeyCode.S) || !squat)
        {
            if (attacking)
            {
                moveLock = true;
            }
            else
            {
                StartCoroutine(MoveUnLock(0));
            }
            animator.SetBool("蹲下", false);
          
        }
    }
    /// <summary>
    /// 該方法須配合Event Trigger ; index=0 為 pointer down時使用，index=1 為 update selected時使用，index=2 為 pointer up時使用。
    /// </summary>
    /// <param name="index"></param>
    public void Jump(int index)
    {
        //Debug.Log(index);
        #region Mobile
        if(index ==0 && canJumpDown && platform != null)//平台下躍判定
        {
            platform.isTrigger = true;
            //Debug.Log("1");
            animator.SetBool("落下", true);
            SoleShow();
            Invoke("EndJumpDown", 0.5f);
            
        }    
        else if (index==0 && !jumping)
        {

            if (controlLock || animator.GetBool("蹲下")) return;
            controlLock = true;
            /// 2019/12/21 20-22 by wen
            jumping = true;
            starthigh = rd2D.transform.position.y; //紀錄起跳位置
            nowhigh = starthigh; //紀錄當前高度
            jumphigh = 0; //紀錄跳躍高度
            ///
            Fighting(1);
            animator.SetBool("跳躍", true);
            
            Invoke("SoleShow",0.2f);
            //rd2D.gravityScale = 0;
            rd2D.velocity = transform.up * maxJumpPower;
            animator.SetBool("落下", true);
            //rd2D.AddForce(transform.up * maxJumpPower, ForceMode2D.Impulse);

        }
        else if(index==1 && jumping && !maxHeight)
        {
            //if (rd2D.velocity.y > maxHigh|| rd2D.velocity.y <0)
            //{
            //    maxHeight = true;
            //}
            nowhigh = rd2D.transform.position.y; //紀錄當前高度
            jumphigh = nowhigh - starthigh; //紀錄跳躍高度
           
            //rd2D.gravityScale = 0;

            if (jumphigh > maxHigh )
            {
                maxHeight = true;
                
                rd2D.gravityScale = 10;
                //Debug.Log("2");
                animator.SetBool("落下", true);
            }
            
            rd2D.velocity = transform.up * maxJumpPower;
            //rd2D.velocity += Vector2.up * minJumpPower;
            //rd2D.AddForce(transform.up * minJumpPower, ForceMode2D.Impulse);  
        }
        if (index == 2 && jumping)
        {
            maxHeight = true;
            
            rd2D.gravityScale = 10;
            //Debug.Log("3");
            animator.SetBool("落下", true);
        }

        //放到外面-----------------------------------------
        nowhigh = rd2D.transform.position.y; //紀錄當前高度
        jumphigh = nowhigh - starthigh; //紀錄跳躍高度
        if ((jumphigh > maxHigh||rd2D.velocity.y < 0)&&jumping)
        {
            maxHeight = true;
           //Debug.Log("4");
            animator.SetBool("落下", true);
        }

       
        //-------------------------------------------------
        #endregion

    }
    private void Jump()
    {
        #region PC
        //if (Input.GetKeyDown(KeyCode.W) && !jumping)
        //{

        //    animator.SetBool("跳躍", true);
        //    jumping = true;
        //    rd2D.AddForce(transform.up * maxJumpPower, ForceMode2D.Impulse);

        //}
        //else if (Input.GetKey(KeyCode.W) && jumping && !maxHeight)
        //{

        //    if (rd2D.velocity.y > maxHigh)
        //    {
        //        maxHeight = true;
        //    }
        //    rd2D.AddForce(transform.up * minJumpPower, ForceMode2D.Impulse);
        //}
        //if (Input.GetKeyUp(KeyCode.W))
        //{
        //    maxHeight = true;
        //}
        #endregion
    }
    /// <summary>
    /// index=0 為輕攻擊，index=1 為重攻擊。
    /// </summary>
    /// <param name="index"></param>
    public void Attack(int index)
    {
        if (controlLock) return;
        controlLock = true;
        attacking = true;
        
        switch (index)
        {
            case 0:
                animator.SetTrigger("輕拳");
                break;
            case 1:
                animator.SetTrigger("重拳");
                break;
        }
        
        if(!animator.GetBool("蹲下")) Fighting(0);

    }
    private void Fighting(int index)
    {
        
        if (index == 0)
        {
            animator.SetBool("戰鬥待機", true);
            FightWaitTime = 0;
        }
        else
        {
            animator.SetBool("戰鬥待機", false);
            FightWaitTime = 0;
        }
       
    }
    public void Dodge()
    {
        if (controlLock || jumping) return;
        controlLock = true;
        moveLock = true;
        animator.SetTrigger("閃躲");
        
        //Debug.DrawLine(buttomPos.position, new Vector2(0, 5), Color.red, 0.1f, true);
        if (transform.localScale.x > 0 || Input.GetKey(KeyCode.D)) 
        {
            rd2D.AddForceAtPosition(transform.right * dodgeDis, transform.position,ForceMode2D.Impulse);
        }
        else if(transform.localScale.x < 0 || Input.GetKey(KeyCode.A))
        {
            rd2D.AddForceAtPosition(-transform.right * dodgeDis, transform.position,ForceMode2D.Impulse);
        }
    }
    
    private void SoleShow()
    {
        sole.enabled = true;
        
    }
    private void SoleUnShow()
    {
        sole.enabled = false;
    }
    private void EndAttacking()
    {
        attacking = false;
    }
    private void EndJumpDown()
    {
        platform.isTrigger = false;
        canJumpDown = false;
        //animator.SetBool("落下", false);

    }
    public void Ray()
    {
        RaycastHit2D hit= Physics2D.Raycast(buttomPos.position, transform.up, 5, 256);
        if (hit)
        {
            squat = true;
            //print(hit.collider);
        }
        else
        { 
            //print(hit.collider);
            squat = false;      
        }

    }
    public IEnumerator MoveUnLock(float time)
    {
        yield return new WaitForSeconds(time);
        moveLock = false;
    }
    public  IEnumerator ContorlUnLock(float time)
    {
        yield return new WaitForSeconds(time);
        controlLock = false;
    }
    #endregion
  
}
