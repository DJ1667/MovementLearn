using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Physics
{
    public class MovingSphere : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] float maxSpeed = 10f; //速度因子

        [SerializeField, Range(0f, 100f)] float maxAcceleration = 10f; //最大加速度

        [SerializeField, Range(0f, 100f)] float maxAirAcceleration = 1f; //最大空中加速度

        [SerializeField, Range(0f, 10f)] float jumpHeight = 2f; //跳跃高度

        [SerializeField, Range(0, 5)] int maxAirJumps = 0; //可跳跃次数

        [SerializeField, Range(0f, 90f)] float maxGroundAngle = 25f; //最大地面角度 如果接触点的法线角度大于这个角度则认为接触了地面

        Rigidbody body;

        [Space(10)] Vector3 velocity; //速度
        Vector3 desiredVelocity; //期望速度

        bool desiredJump; //期望跳跃
        bool onGround; //是否在地面上
        int jumpPhase; //跳跃次数

        float minGroundDotProduct; //最大地面角度的Cos值
        Vector3 contactNormal; //接触点的法线

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            //(4.7.2)
            OnValidate();
        }

        //(4.7.2) 判定是否在地面的条件可配置
        void OnValidate()
        {
            //我们以度数指定角度，但Mathf.Cos期望它以弧度表示。我们可以通过乘以Mathf.Deg2Rad来转换它
            minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        }

        void Update()
        {
            var playerInput = new Vector2(0, 0);
            playerInput.x = Input.GetAxis("Horizontal");
            playerInput.y = Input.GetAxis("Vertical");

            // desiredJump = Input.GetButtonDown("Jump");
            desiredJump |= Input.GetButtonDown("Jump"); //使用or运算 保存上一次期望跳跃的状态

            playerInput = Vector2.ClampMagnitude(playerInput, 1); //这种方式更好，防止超过1的取1，不到1的取原始值

            desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed; //期望速度
            float maxSpeedChange = maxAcceleration * Time.deltaTime;

            //(1) 这种直接改位置的相当于直接瞬移  如果使用物理的话应该要么通过向它施加力，要么通过调整它的速度。
            // velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            // velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
            // var displacement = velocity * Time.deltaTime;
            // var newPosition = transform.localPosition + displacement;
            // transform.localPosition = newPosition;

            //(2) 使用刚体的速度来控制移动
            // velocity = body.velocity;
            // velocity.x =
            //     Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            // velocity.z =
            //     Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
            // body.velocity = velocity;
        }

        private void FixedUpdate()
        {
            //(3) 使用物理的固定的时间步长
            velocity = body.velocity;

            UpdateState();

            //(4.6.1)
            // float maxSpeedChange = maxAcceleration * Time.deltaTime;
            //(4.6.2) 加一个空中的加速度 模拟空气运动 限制在空中可以快速变换运动方向的问题
            // float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
            // float maxSpeedChange = acceleration * Time.deltaTime;
            // velocity.x =
            //     Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
            // velocity.z =
            //     Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);
            //(4.9)
            AdjustVelocity();

            if (desiredJump)
            {
                desiredJump = false;
                Jump();
            }

            body.velocity = velocity;

            //(4.2) 每个物理步骤都从调用所有FixedUpdate方法开始，然后 PhysX 执行它的操作，最后调用碰撞方法。因此，如果有任何活动碰撞，FixedUpdate调用onGround的时间将设置为在最后一步期间
            onGround = false;
        }

        void Jump()
        {
            if (onGround || jumpPhase < maxAirJumps)
            {
                jumpPhase += 1;
                //(4.5.1)
                // velocity.y += Mathf.Sqrt(-2 * UnityEngine.Physics.gravity.y * jumpHeight);
                //(4.5.2) 限制向上速度
                // var jumpSpeed = Mathf.Sqrt(-2f * UnityEngine.Physics.gravity.y * jumpHeight);
                // if (velocity.y > 0)
                //     jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0);
                // velocity.y = jumpSpeed;
                //(4.8)  为了在其法向量的方向上跳离地面 每个斜坡测试车道上产生不同方向的跳跃
                var jumpSpeed = Mathf.Sqrt(-2f * UnityEngine.Physics.gravity.y * jumpHeight);
                float alignedSpeed = Vector3.Dot(velocity, contactNormal); //检查与接触法线对齐的速度,通过速度在法线方向上的投影来计算
                if (alignedSpeed > 0)
                    jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0);
                velocity += contactNormal * jumpSpeed;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            // onGround = true;
            EvaluateCollision(collision);
        }

        //(4.1) 这种写法有弊端，当同时接触地面与墙壁，然后离开墙壁时就无法跳跃。 （Tips 可以通过Layer来过滤）
        // void OnCollisionExit () {
        //     onGround = false;
        // }

        //(4.2) 解决（4.1）的问题 （这是不通过layer过滤的方法）
        void OnCollisionStay(Collision collision)
        {
            // onGround = true;
            EvaluateCollision(collision);
        }

        //(4.3) 解决接触墙壁也可以跳跃的问题（不能跳墙） 
        void EvaluateCollision(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 normal = collision.GetContact(i).normal;
                //法线是球体应该被推动的方向，它直接远离碰撞表面。假设它是一个平面，向量与平面的法向量相匹配。如果平面是水平的，那么它的法线会笔直向上，所以它的 Y 分量应该恰好为 1。
                //如果是这种情况，那么我们就会接触到地面。但是让我们宽容一点，接受 0.9 或更大的 Y 分量。
                //(4.7.1)
                // onGround |= normal.y >= 0.9f;
                //(4.7.2)
                // onGround |= normal.y >=minGroundDotProduct;

                //(4.8)  为了在其法向量的方向上跳离地面 每个斜坡测试车道上产生不同方向的跳跃
                if (normal.y >= minGroundDotProduct)
                {
                    onGround = true;
                    //(4.8)
                    //contactNormal = normal;
                    //(4.10) 
                    contactNormal += normal;
                }
            }
        }

        //（4.4） 设置可跳跃次数
        void UpdateState()
        {
            if (onGround)
            {
                jumpPhase = 0;
                //(4.10)
                contactNormal.Normalize();
            }
            //（4.8） 在空中仍然直线跳跃
            else
            {
                contactNormal = Vector3.up;
            }
        }

        //(4.9)
        //将速度投影到平面上以获得新的速度
        //通过获取向量和法线的点积，然后从原始速度向量中减去按其比例缩放的法线来做到这一点
        Vector3 ProjectOnContactPlane(Vector3 vector)
        {
            return vector - contactNormal * Vector3.Dot(vector, contactNormal);
        }

        //(4.9) 沿斜坡移动 将移动量拆分到对应的轴上去
        void AdjustVelocity()
        {
            float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
            float maxSpeedChange = acceleration * Time.deltaTime;

            Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
            Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

            float currentX = Vector3.Dot(velocity, xAxis);
            float currentZ = Vector3.Dot(velocity, zAxis);

            float newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
            float newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

            velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
        }

        //(4.10) 处理有多个地面法线的问题
        void ClearState()
        {
            onGround = false;
            contactNormal = Vector3.zero;
        }
    }
}