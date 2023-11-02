using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveObjectWithMouse : MonoBehaviour
{

    private Vector3 objMoveOffset;
    private bool isDragging = false;
    void OnMouseDown()
    {
        // Debug.Log("Mouse Down");
        objMoveOffset = transform.position - GetMouseWorldPos();
        isDragging = true;
    }

    void OnMouseUp()
    {
        // Debug.Log("Mouse Up");
        isDragging = false;
    }

    Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return new Vector3(hit.point.x, 0, hit.point.z);
        }
        return Vector3.zero;
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isDragging)
        {
            transform.position = GetMouseWorldPos() + objMoveOffset;
        }
    }
}
