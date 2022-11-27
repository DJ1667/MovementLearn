using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] float maxSpeed = 10f; //速度因子

    Vector3 velocity; //速度

    [SerializeField, Range(0f, 100f)] float maxAcceleration = 10f; //最大加速度

    [SerializeField] Rect allowedArea = new Rect(-5f, -5f, 10f, 10f); //限制移动范围
    
    [SerializeField, Range(0f, 1f)]
    float bounciness = 0.5f;//弹性


    void Update()
    {
        var playerInput = new Vector2(0, 0);
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");

        // playerInput.Normalize(); //防止超过1，但是归一化会导致始终为1，这种方式并不太好
        playerInput = Vector2.ClampMagnitude(playerInput, 1); //这种方式更好，防止超过1的取1，不到1的取原始值

        // transform.localPosition = new Vector3(playerInput.x, 0.5f, playerInput.y);

        //(1)缺点 帧率越大速度越快
        // var displacement = new Vector3(playerInput.x, 0, playerInput.y);
        // transform.localPosition += displacement;

        //(2)加入Time.deltaTime防止帧率影响速度
        // var velocity =  new Vector3(playerInput.x, 0, playerInput.y) * maxSpeed;
        // var displacement = velocity * Time.deltaTime;
        // transform.localPosition += displacement;

        //(3)引入加速度 让速度变化平滑而不是突变
        // var  acceleration = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;//加速度
        // velocity += acceleration * Time.deltaTime;
        // var displacement = velocity * Time.deltaTime;
        // transform.localPosition += displacement;

        //(4)引入最大加速度
        Vector3 desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed; //期望速度
        float maxSpeedChange = maxAcceleration * Time.deltaTime;
        //(4.1.1)
        // if (velocity.x < desiredVelocity.x)
        // {
        //     // velocity.x += maxSpeedChange;
        //     //防止速度超过阈值
        //     velocity.x = Mathf.Min(velocity.x+maxSpeedChange, desiredVelocity.x);
        // }
        // else if(velocity.x> desiredVelocity.x)
        // {
        //     velocity.x = Mathf.Max(velocity.x - maxSpeedChange, desiredVelocity.x);
        // }
        //(4.1.2) 更简洁的写法
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
        var displacement = velocity * Time.deltaTime;
        //(4.2.1)
        //transform.localPosition += displacement;
        //(4.2.2) 增加区域限制
        var newPosition = transform.localPosition + displacement;
        // (不符合条件直接忽略运动---这种方式不好)
        // if (!allowedArea.Contains(new Vector2(newPosition.x,newPosition.z)))
        // {
        //     newPosition = transform.localPosition;
        // }
        //(4.2.3) 平滑过度  防止直接忽略运动
        // if (!allowedArea.Contains(new Vector2(newPosition.x,newPosition.z)))
        // {
        //     newPosition.x = Mathf.Clamp(newPosition.x, allowedArea.xMin, allowedArea.xMax);
        //     newPosition.z = Mathf.Clamp(newPosition.z, allowedArea.yMin, allowedArea.yMax);
        // }
        //(4.2.4) 防止物体到达墙体但速度仍保持导致回头困难 需要重置速度
        // if (newPosition.x < allowedArea.xMin)
        // {
        //     newPosition.x = allowedArea.xMin;
        //     velocity.x = 0f;
        // }else if (newPosition.x > allowedArea.xMax)
        // {
        //     newPosition.x = allowedArea.xMax;
        //     velocity.x = 0f;
        // }
        // if (newPosition.z < allowedArea.yMin)
        // {
        //     newPosition.z = allowedArea.yMin;
        //     velocity.z = 0f;
        // }else if (newPosition.z > allowedArea.yMax)
        // {
        //     newPosition.z = allowedArea.yMax;
        //     velocity.z = 0f;
        // }
        //(4.2.5) 考虑该物体是完美弹跳球的情况 给一个反向弹跳速度
        if (newPosition.x < allowedArea.xMin)
        {
            newPosition.x = allowedArea.xMin;
            //(4.2.5.1)
            // velocity.x = -velocity.x;
            //(4.2.5.2) //增加弹性因子
            velocity.x = -velocity.x * bounciness;
        }
        else if (newPosition.x > allowedArea.xMax)
        {
            newPosition.x = allowedArea.xMax;
            //(4.2.5.1)
            // velocity.x = -velocity.x;
            //(4.2.5.2) //增加弹性因子
            velocity.x = -velocity.x * bounciness;
        }
        if (newPosition.z < allowedArea.yMin)
        {
            newPosition.z = allowedArea.yMin;
            //(4.2.5.1)
            // velocity.z = -velocity.z;
            //(4.2.5.2) //增加弹性因子
            velocity.z = -velocity.z * bounciness;
        }
        else if (newPosition.z > allowedArea.yMax)
        {
            newPosition.z = allowedArea.yMax;
            //(4.2.5.1)
            // velocity.z = -velocity.z;
            //(4.2.5.2) //增加弹性因子
            velocity.z = -velocity.z * bounciness;
        }
        transform.localPosition = newPosition;
    }
}