using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;

public class BVHDriver : MonoBehaviour
{
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. Bone names should be identical to those in the BVH file and unique. All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAvatar;
    [Tooltip("This is the path to the BVH file that should be loaded. Bone offsets are currently being ignored by this loader.")]
    public string filename;
    [Tooltip("If the flag above is disabled, the frame rate given in the BVH file will be overridden by this value.")]
    public float frameRate = 60.0f;
    [Tooltip("If the BVH first frame is T(if not,make sure the defined skeleton is T).")]
    public bool FirstT = true;

    // [Serializable]
    // public struct BoneMap
    // {
    //     public string bvh_name;
    //     public HumanBodyBones humanoid_bone;
    // }
    // [Tooltip("If the flag above is disabled, the frame rate given in the BVH file will be overridden by this value.")]
    // public BoneMap[] bonemaps; // the corresponding bones between unity and bvh
    private BVHParser bp = null;
    private Animator anim;

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parseFile()
    {
        string bvhData = File.ReadAllText(filename);
        bp = new BVHParser(bvhData);
        frameRate = 1f / bp.frameTime;
    }

    private Dictionary<string, Quaternion> bvhT;
    private Dictionary<string, Vector3> bvhOffset;
    private Dictionary<string, string> bvhHireachy;
    private Dictionary<HumanBodyBones, Quaternion> unityT;

    private int frameIdx;
    private float scaleRatio = 0.0f;

    private void ClearLines()
    {
        GameObject[] lines = GameObject.FindGameObjectsWithTag("line");
        foreach (GameObject line in lines)
        {
            Destroy(line);
        }

        GameObject[] spheres = GameObject.FindGameObjectsWithTag("sphere");
        foreach (GameObject sphere in spheres)
        {
            Destroy(sphere);
        }
    }

    private void DrawModel(Dictionary<string, Vector3> bvhPos)
    {
        // lineRenderer.positionCount = bvhHireachy.Count * 2;
        foreach (string bname in bvhHireachy.Keys)
        {
            // 父關節位置 bvhPos[bvhHireachy[bname]], 子關節位置 bvhPos[bname]

            // draw bvh skeleton in Scene
            Color color = new Color(1.0f, 0.0f, 0.0f);
            Debug.DrawLine(bvhPos[bname] * scaleRatio, bvhPos[bvhHireachy[bname]] * scaleRatio, color);

            // draw bvh skeleton in Game
            GameObject lineObj = new GameObject("line");
            lineObj.tag = "line";
            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, bvhPos[bname] * scaleRatio);
            lineRenderer.SetPosition(1, bvhPos[bvhHireachy[bname]] * scaleRatio);

            GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObj.tag = "sphere";
            sphereObj.transform.position = bvhPos[bname] * scaleRatio;
            sphereObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        }
    }

    private void Start()
    {
        parseFile();
        Application.targetFrameRate = (Int16)frameRate;

        bvhT = bp.getKeyFrame(0);
        bvhOffset = bp.getOffset(1.0f);
        bvhHireachy = bp.getHierachy();

        anim = targetAvatar.GetComponent<Animator>();
        unityT = new Dictionary<HumanBodyBones, Quaternion>();

        // lineRenderer = gameObject.AddComponent<LineRenderer>();
        // lineRenderer.startColor = Color.red;
        // lineRenderer.endColor = Color.red;
        // lineRenderer.startWidth = 0.02f;
        // lineRenderer.endWidth = 0.02f;

        // foreach (BoneMap bm in bonemaps)
        // {
        //     unityT.Add(bm.humanoid_bone, anim.GetBoneTransform(bm.humanoid_bone).rotation);
        // }

        // float unity_leftleg = (anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position - anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position).sqrMagnitude +
        //     (anim.GetBoneTransform(HumanBodyBones.LeftFoot).position - anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position).sqrMagnitude;
        // float bvh_leftleg = 0.0f;

        // foreach (BoneMap bm in bonemaps)
        // {
        //     if (bm.humanoid_bone == HumanBodyBones.LeftUpperLeg || bm.humanoid_bone == HumanBodyBones.LeftLowerLeg)
        //     {
        //         bvh_leftleg = bvh_leftleg + bvhOffset[bm.bvh_name].sqrMagnitude;
        //     }
        // }

        // scaleRatio = unity_leftleg / bvh_leftleg;

        frameIdx = 0;
    }

    private void Update()
    {
        Dictionary<string, Quaternion> currFrame = bp.getKeyFrame(frameIdx);//frameIdx 2871
        if (frameIdx < bp.frames - 1)
        {
            frameIdx++;
        }
        else
        {
            frameIdx = 0;
        }

        // foreach (BoneMap bm in bonemaps)
        // {
        //     if (FirstT)
        //     {
        //         Transform currBone = anim.GetBoneTransform(bm.humanoid_bone);
        //         currBone.rotation = (currFrame[bm.bvh_name] * Quaternion.Inverse(bvhT[bm.bvh_name])) * unityT[bm.humanoid_bone];
        //     }
        //     else
        //     {
        //         Transform currBone = anim.GetBoneTransform(bm.humanoid_bone);
        //         currBone.rotation = currFrame[bm.bvh_name] * unityT[bm.humanoid_bone];
        //     }
        // }

        // draw bvh skeleton
        Dictionary<string, Vector3> bvhPos = new Dictionary<string, Vector3>();
        foreach (string bname in currFrame.Keys)
        {
            if (bname == "pos")
            {
                bvhPos.Add(bp.root.name, new Vector3(currFrame["pos"].x, currFrame["pos"].y, currFrame["pos"].z));
            }
            else
            {
                if (bvhHireachy.ContainsKey(bname) && bname != bp.root.name)
                {
                    Vector3 curpos = bvhPos[bvhHireachy[bname]] + currFrame[bvhHireachy[bname]] * bvhOffset[bname];
                    bvhPos.Add(bname, curpos);
                }
            }
        }

        // compute scaleRatio
        Vector3 modelHipsPos = anim.GetBoneTransform(HumanBodyBones.Hips).position;
        Vector3 modelRightUpLegPos = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
        Vector3 bvhHipsPos = bvhPos[bp.root.name];
        Vector3 bvhRightUpLegPos = bvhPos["RightUpLeg"];
        scaleRatio = Vector3.Distance(modelRightUpLegPos, modelHipsPos) / Vector3.Distance(bvhRightUpLegPos, bvhHipsPos);

        // anim.GetBoneTransform(HumanBodyBones.Hips).position = bvhPos[bp.root.name] * scaleRatio;
        ClearLines();
        DrawModel(bvhPos);
    }
}
