using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public enum PositionIndex : int
// {
//     rShldrBend = 0,
//     rForearmBend,
//     rHand,
//     rThumb2,
//     rMid1,

//     lShldrBend,
//     lForearmBend,
//     lHand,
//     lThumb2,
//     lMid1,

//     lEar,
//     lEye,
//     rEar,
//     rEye,
//     Nose,

//     rThighBend,
//     rShin,
//     rFoot,
//     rToe,

//     lThighBend,
//     lShin,
//     lFoot,
//     lToe,

//     abdomenUpper,

//     //Calculated coordinates
//     hip,
//     head,
//     neck,
//     spine,

//     Count,
//     None,
// }
// public static partial class EnumExtend
// {
//     public static int Int(this PositionIndex i)
//     {
//         return (int)i;
//     }
// }

public class ComputeDistance : MonoBehaviour
{
    public BVHDriver bvhDriver;
    public VNectModel vNectModel;

    public class JointPoint
    {
        public Vector2 Pos2D = new Vector2();
        public float score2D;

        public Vector3 Pos3D = new Vector3();
        public Vector3 Now3D = new Vector3();
        public Vector3[] PrevPos3D = new Vector3[6];
        public float score3D;

        // Bones
        public Transform Transform = null;
        public Quaternion InitRotation;
        public Quaternion Inverse;
        public Quaternion InverseRotation;

        public JointPoint Child = null;
        public JointPoint Parent = null;

        // For Kalman filter
        public Vector3 P = new Vector3();
        public Vector3 X = new Vector3();
        public Vector3 K = new Vector3();
    }
    public class Skeleton
    {
        public GameObject LineObject;
        public LineRenderer Line;
        public GameObject SphereObject;

        public JointPoint start = null;
        public JointPoint end = null;
        public JointPoint spherePos = null;
    }
    void Start()
    {

    }

    void Update()
    {
        var bvhPos = bvhDriver.getBvhPos();
        var jointPoints = vNectModel.JointPoints;
        if (bvhDriver.getIsLoaded())
        {
            Debug.Log(bvhPos["Hips"]);
        }

        if (vNectModel.getIsLoaded())
        {
            // Debug.Log(jointPoints[PositionIndex.rShldrBend.Int()].Pos3D);
        }

        if (bvhDriver.getIsLoaded() && vNectModel.getIsLoaded())
        {
            Debug.Log("Both loaded");
        }

    }
}
