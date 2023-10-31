using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Windows.Forms;
using burningmime.curves;
using UnityEditor;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class BVHDriver : MonoBehaviour
{
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. Bone names should be identical to those in the BVH file and unique. All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAvatar;
    [Tooltip("This is the path to the BVH file that should be loaded. Bone offsets are currently being ignored by this loader.")]
    public float frameRate = 60.0f;
    [Tooltip("If the BVH first frame is T(if not,make sure the defined skeleton is T).")]
    public bool FirstT;
    public MonoScript mouseEventScript;
    private BVHParser bp = null;
    private Animator anim;
    private bool isBVHLoaded = false;
    private Dictionary<string, Quaternion> bvhT;
    private Dictionary<string, Quaternion> unityT = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> bvhOffset;
    private Dictionary<string, string> bvhHireachy;
    private int frameIdx;
    private float scaleRatio = 0.0f;
    private List<Vector3> bvhRootPos = new List<Vector3>();
    private List<Vector3> bvhRootRot = new List<Vector3>();
    private List<burningmime.curves.CubicBezier> curves = new List<burningmime.curves.CubicBezier>();
    private List<GameObject> controlPoints = new List<GameObject>();


    public string OpenFileByDll()
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Filter = "BVH Files (*.bvh)|*.bvh";


        dialog.InitialDirectory = @"C:\";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            string path = dialog.FileName;
            Debug.Log(path);
            return path;
        }
        // throw new ArgumentException("Failed to Load BVH File" + dialog.FileName);
        return null;
    }

    // This function doesn't call any Unity API functions and should be safe to call from another thread
    public void parseFile()
    {
        // string filename = "G:/3D_Game/MotionPathEditing/bvh_sample_files/walk_loop.bvh";
        string filename = OpenFileByDll();
        if (filename != null)
        {
            string bvhData = File.ReadAllText(filename);
            bp = new BVHParser(bvhData);
            frameRate = 1f / bp.frameTime;

            UnityEngine.Application.targetFrameRate = (Int16)frameRate;
            bvhT = bp.getKeyFrame(0);
            bvhOffset = bp.getOffset(1.0f);
            bvhHireachy = bp.getHierachy();

            anim = targetAvatar.GetComponent<Animator>();
            GetModelQuatertion();
            GetRootBonePosAndRot();
            // VisualizePoint();
            // unityT = new Dictionary<HumanBodyBones, Quaternion>();


            frameIdx = 0;

            isBVHLoaded = true;
            return;
        }

        throw new ArgumentException("Failed to Load BVH File");
    }

    // private Dictionary<string, Quaternion> bvhT;
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

    private void SetModelQuatertion(Dictionary<string, Quaternion> unityRot)
    {
        GameObject model = GameObject.Find("unitychan (1)");
        if (model == null)
        {
            Debug.Log("Can't find model");
            return;
        }

        Debug.Log("Find model");
        foreach (Transform joint in model.GetComponentsInChildren<Transform>())
        {
            if (joint != model)
            {
                string jointName = joint.name;

                if (jointName.Contains("Character1_"))
                {
                    jointName = jointName.Replace("Character1_", "");
                    // Debug.Log(jointName);
                    if (bvhHireachy.ContainsKey(jointName))
                    {
                        joint.rotation = unityRot[jointName];
                        // joint.Rotate(unityRot[jointName]);
                    }
                }
            }
        }
    }

    private void GetModelQuatertion()
    {
        GameObject model = GameObject.Find("unitychan (1)");
        if (model == null)
        {
            Debug.Log("Can't find model");
            return;
        }

        Debug.Log("Find model");
        foreach (Transform joint in model.GetComponentsInChildren<Transform>())
        {
            if (joint != model)
            {
                string jointName = joint.name;

                if (jointName.Contains("Character1_"))
                {
                    jointName = jointName.Replace("Character1_", "");
                    // Debug.Log(jointName);
                    if (bvhHireachy.ContainsKey(jointName))
                    {
                        Quaternion rot = joint.rotation;

                        // Debug.Log(jointName);
                        // Debug.Log("Quternion: " + rot);
                        unityT.Add(jointName, rot);

                    }
                    else if (jointName == "Hips")
                    {
                        Quaternion rot = joint.rotation;

                        // Debug.Log(jointName);
                        // Debug.Log("Quternion: " + rot);
                        unityT.Add(jointName, rot);
                    }
                }
            }
        }
    }

    private int AmountKeyFramePoint()
    {
        int amount = 0;
        amount = bp.frames / 10;
        return amount;
    }

    private void CreateObjPoint(int pointCount)
    {
        // z - axis gap
        // float gap = 0.0f;
        for (int index = 0; index < pointCount; index++)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // GameObject point = new GameObject("p" + index);
            point.name = "Cp" + (index + 1);
            point.tag = "point";
            point.SetActive(true);
            point.AddComponent(mouseEventScript.GetClass());
            // point.transform.position = new Vector3(0.0f, 0.0f, gap);
            point.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            controlPoints.Add(point);

            // Debug.Log("index: " + (index + 1));
            // Debug.Log(pointCount);
            if ((index + 1) == (pointCount + 1))
            {
                controlPoints[index - 1].transform.parent = GameObject.FindGameObjectWithTag("point").transform;
            }
            else if (controlPoints.Count > 1)
            {
                controlPoints[index - 1].transform.parent = GameObject.FindGameObjectWithTag("point").transform;
            }
            // gap += 1.0f;
        }
    }

    private void GetRootBonePosAndRot()
    {
        int frameNum = bp.frames;

        BVHParser.BVHBone.BVHChannel[] channels = bp.root.channels;
        bvhRootPos.Clear();
        bvhRootRot.Clear();

        for (int i = 0; i < frameNum; i++)
        {
            Vector3 pos = new Vector3(channels[0].values[i], channels[1].values[i], channels[2].values[i]);
            Vector3 rot = new Vector3(channels[5].values[i], channels[3].values[i], channels[4].values[i]);
            bvhRootPos.Add(pos);
            bvhRootRot.Add(rot);
        }
        curves.Clear();
        curves.AddRange(CurveFit.Fit(bvhRootPos, 2.0f));
        VisualizeControlPoint();
        //DrawMultiCurve();
    }

    private void VisualizeControlPoint()
    {
        int maxControlPoint = (curves.Count * 4 - (curves.Count - 1));
        CreateObjPoint(maxControlPoint);

        // if (controlPoints.Count < curves.Count)
        // {
        //     for (int i = 0; i < maxControlPoint; i++)
        //     {
        //         if (i > 1)
        //         {
        //             GameObject parent = GameObject.FindGameObjectWithTag("point");
        //             if (parent != null)
        //             {
        //                 Debug.Log("HIIIIIIIIIIIIIIIIIIIIIIIIIIII");
        //                 controlPoints[i - 1].transform.parent = parent.transform;
        //             }
        //         }
        //     }
        // }
        // Debug.Log(controlPoints.Count);
        // Debug.Log(maxControlPoint);
        for (int i = 0; i < controlPoints.Count; i += 3)
        {
            int offset = 0;
            if (i / 4 > 0) offset = 1;
            int curveIndex = i == 0 ? 0 : (i - 1) / 3;
            // Debug.Log("i: " + i);
            // Debug.Log("offset: " + offset);
            // Debug.Log("curveIndex: " + curveIndex);
            controlPoints[i + 1 - offset].transform.position = curves[curveIndex].p1;
            controlPoints[i + 2 - offset].transform.position = curves[curveIndex].p2;
            controlPoints[i + 3 - offset].transform.position = curves[curveIndex].p3;
            if (i == 0)
            {
                controlPoints[i].transform.position = curves[curveIndex].p0;
                i++;
            }
        }
    }

    private void Start()
    {
        // parseFile();
        // UnityEngine.Application.targetFrameRate = (Int16)frameRate;
        // bvhT = bp.getKeyFrame(0);
        // bvhOffset = bp.getOffset(1.0f);
        // bvhHireachy = bp.getHierachy();
        // anim = targetAvatar.GetComponent<Animator>();
        // unityT = new Dictionary<HumanBodyBones, Quaternion>();
        // frameIdx = 0;
    }

    private void Update()
    {
        if (isBVHLoaded)
        {
            // print("frameIdx: " + frameIdx + " bp.frames: " + bp.frames);
            // getKeyFrame 獲取當前幀在世界座標下的旋轉四元數
            Dictionary<string, Quaternion> currFrame = bp.getKeyFrame(frameIdx);//frameIdx 2871
            if (frameIdx < bp.frames - 1)
            {
                frameIdx++;
            }
            else
            {
                frameIdx = 0;
            }

            // draw bvh skeleton
            Dictionary<string, Vector3> bvhPos = new Dictionary<string, Vector3>();
            Dictionary<string, Quaternion> unityRot = new Dictionary<string, Quaternion>();
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
                        Vector3 curpos = bvhPos[bvhHireachy[bname]] + currFrame[bvhHireachy[bname]] * bvhOffset[bname]; // T3, T4
                        Quaternion unityCurRot = currFrame[bvhHireachy[bname]] * unityT[bvhHireachy[bname]];
                        // bvh 沒有T2所以T3等同T4因此unityModel位置等於UnityT * T3
                        // Vector3 unityCurRot = unityT[bvhHireachy[bname]] * curpos; // T5
                        bvhPos.Add(bname, curpos);
                        unityRot.Add(bname, unityCurRot);
                    }
                }
            }

            // compute scaleRatio
            Vector3 modelHipsPos = anim.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 modelRightUpLegPos = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
            Vector3 bvhHipsPos = bvhPos[bp.root.name];
            Vector3 bvhRightUpLegPos = bvhPos["RightUpLeg"];
            scaleRatio = Vector3.Distance(modelRightUpLegPos, modelHipsPos) / Vector3.Distance(bvhRightUpLegPos, bvhHipsPos);

            anim.GetBoneTransform(HumanBodyBones.Hips).position = new Vector3(bvhPos[bp.root.name].x + 150.0f, bvhPos[bp.root.name].y, bvhPos[bp.root.name].z) * scaleRatio;

            ClearLines();
            DrawModel(bvhPos);
            SetModelQuatertion(unityRot);
        }
    }
}
