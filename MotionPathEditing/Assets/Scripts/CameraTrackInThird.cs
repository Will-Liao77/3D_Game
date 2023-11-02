using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraTrackInThird : MonoBehaviour
{
    private Vector3 cameraOffset = new Vector3(0, 1, 4);
    private Transform playerTransform;
    private Vector3 cameraPosition;
    // Start is called before the first frame update
    private float speed = 2.0f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        GameObject hipsPos = GameObject.FindGameObjectWithTag("hipsPos");
        if (hipsPos == null)
        {
            return;
        }
        playerTransform = hipsPos.transform;
        cameraPosition = playerTransform.position + cameraOffset;
        this.transform.position = Vector3.Lerp(this.transform.position, cameraPosition, speed * Time.deltaTime);
        Quaternion targetRotation = Quaternion.LookRotation(playerTransform.position - this.transform.position);
        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, speed * Time.deltaTime);
    }
}
