using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Eular : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 acceeleration;
    public float mass = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        velocity = new Vector3(0, 0, 0);
        acceeleration = new Vector3(0, -9.8f, 0);
    }

    public void Euler()
    {
        velocity += acceeleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }

    public void RungeKutta2()
    {
        // 計算中點速度與位置
        Vector3 midVelocity = velocity + 0.5f * acceeleration * Time.deltaTime;
        Vector3 midPosition = transform.position + 0.5f * velocity * Time.deltaTime;

        // 用中點估計最後的速度與位置
        velocity += acceeleration * Time.deltaTime;
        transform.position += midVelocity * Time.deltaTime;
    }

    // public Vector3 CaculateAcceleration(Vector3 position, Vector3 velocity)
    // {
    //     return new Vector3(0, -9.8f, 0);
    // }

    // public void RungeKutta4()
    // {
    //     float dt = Time.deltaTime;

    //     Vector3 k1Velocity = dt * CaculateAcceleration(transform.position, velocity);
    //     Vector3 k1Position = dt * velocity;

    //     Vector3 k2Velocity = dt * CaculateAcceleration(transform.position + 0.5f * k1Position, velocity + 0.5f * k1Velocity);
    //     Vector3 k2Position = dt * (velocity + 0.5f * k1Velocity);

    //     Vector3 k3Velocity = dt * CaculateAcceleration(transform.position + 0.5f * k2Position, velocity + 0.5f * k2Velocity);
    //     Vector3 k3Position = dt * (velocity + 0.5f * k2Velocity);

    //     Vector3 k4Velocity = dt * CaculateAcceleration(transform.position + k3Position, velocity + k3Velocity);
    //     Vector3 k4Position = dt * (velocity + k3Velocity);

    //     velocity += (k1Velocity + 2.0f * k2Velocity + 2.0f * k3Velocity + k4Velocity) / 6.0f;
    //     transform.position += (k1Position + 2.0f * k2Position + 2.0f * k3Position + k4Position) / 6.0f;

    // }

    // Update is called once per frame
    void Update()
    {
        velocity += acceeleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }
}
