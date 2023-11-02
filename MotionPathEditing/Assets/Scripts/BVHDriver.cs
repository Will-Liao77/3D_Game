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
    private Animator anim;

    // load bvh1
    private BVHParser bp = null;
    private bool isBVHLoaded1 = false;
    private Dictionary<string, Quaternion> bvhT1;
    private Dictionary<string, Quaternion> unityT1 = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> bvhOffset1;
    private Dictionary<string, string> bvhHireachy1;
    private float scaleRatio1 = 0.0f;
    private int frameIdx1;


    // load bvh2
    private BVHParser bp2 = null;
    private bool isBVHLoaded2 = false;
    private Dictionary<string, Quaternion> bvhT2;
    private Dictionary<string, Quaternion> unityT2 = new Dictionary<string, Quaternion>();
    private Dictionary<string, Vector3> bvhOffset2;
    private Dictionary<string, string> bvhHireachy2;
    private float scaleRatio2 = 0.0f;
    private int frameIdx2;

    private List<Vector3> points = new List<Vector3>();
    private List<Vector3> pointsTangent = new List<Vector3>();
    private List<float> arcLegnth = new List<float>();
    private List<Vector3> bvhRootPos = new List<Vector3>();
    private List<Vector3> bvhRootRot = new List<Vector3>();
    private List<burningmime.curves.CubicBezier> curves = new List<burningmime.curves.CubicBezier>();
    private List<GameObject> controlPoints = new List<GameObject>();
    private List<GameObject> controlPointsLine = new List<GameObject>();


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
            if (bp != null)
            {
                bp = null;
                bvhT1.Clear();
                bvhOffset1.Clear();
                bvhHireachy1.Clear();
            }
            string bvhData = File.ReadAllText(filename);
            bp = new BVHParser(bvhData);
            frameRate = 1f / bp.frameTime;

            UnityEngine.Application.targetFrameRate = (Int16)frameRate;
            bvhT1 = bp.getKeyFrame(0);
            bvhOffset1 = bp.getOffset(1.0f);
            bvhHireachy1 = bp.getHierachy();

            anim = targetAvatar.GetComponent<Animator>();
            // GetModelQuatertion();
            GetRootBonePosAndRot();

            frameIdx1 = 0;

            isBVHLoaded1 = true;
            return;
        }

        throw new ArgumentException("Failed to Load BVH File 1");
    }

    public void parseFile2()
    {
        // string filename = "G:/3D_Game/MotionPathEditing/bvh_sample_files/walk_loop.bvh";
        string filename = OpenFileByDll();
        if (filename != null)
        {
            string bvhData = File.ReadAllText(filename);
            bp2 = new BVHParser(bvhData);
            frameRate = 1f / bp2.frameTime;

            UnityEngine.Application.targetFrameRate = (Int16)frameRate;
            bvhT2 = bp2.getKeyFrame(0);
            bvhOffset2 = bp2.getOffset(1.0f);
            bvhHireachy2 = bp2.getHierachy();

            anim = targetAvatar.GetComponent<Animator>();
            // GetModelQuatertion();
            // GetRootBonePosAndRot();
            // VisualizePoint();
            // unityT = new Dictionary<HumanBodyBones, Quaternion>();


            frameIdx2 = 0;

            isBVHLoaded2 = true;
            return;
        }

        throw new ArgumentException("Failed to Load BVH File 2");
    }

    // private Dictionary<string, Quaternion> bvhT;
    private void ClearLines(int n_bvh)
    {
        GameObject[] lines = GameObject.FindGameObjectsWithTag("line" + n_bvh.ToString());
        foreach (GameObject line in lines)
        {
            Destroy(line);
        }

        GameObject[] spheres = GameObject.FindGameObjectsWithTag("sphere" + n_bvh.ToString());
        foreach (GameObject sphere in spheres)
        {
            Destroy(sphere);
        }
    }

    private void DrawModel(Dictionary<string, Vector3> bvhPos, int n_bvh)
    {
        if (n_bvh == 1)
        {
            foreach (string bname in bvhHireachy1.Keys)
            {
                // 父關節位置 bvhPos[bvhHireachy[bname]], 子關節位置 bvhPos[bname]

                // draw bvh skeleton in Scene
                Color color = new Color(1.0f, 0.0f, 0.0f);
                Debug.DrawLine(bvhPos[bname] * scaleRatio1, bvhPos[bvhHireachy1[bname]] * scaleRatio1, color);

                // draw bvh skeleton in Game
                GameObject lineObj = new GameObject("line");
                lineObj.tag = "line1";
                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                Material material = new Material(Shader.Find("Standard"));
                lineRenderer.material = material;
                lineRenderer.sharedMaterial.SetColor("_Color", Color.red);
                // lineRenderer.startColor = Color.red;
                // lineRenderer.endColor = Color.red;
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, bvhPos[bname] * scaleRatio1);
                lineRenderer.SetPosition(1, bvhPos[bvhHireachy1[bname]] * scaleRatio1);

                GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereObj.tag = "sphere1";
                sphereObj.transform.position = bvhPos[bname] * scaleRatio1;
                sphereObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }
        else
        {
            foreach (string bname in bvhHireachy2.Keys)
            {
                // draw bvh skeleton in Scene
                Color color = new Color(0.0f, 0.0f, 1.0f);
                Debug.DrawLine(bvhPos[bname] * scaleRatio2, bvhPos[bvhHireachy2[bname]] * scaleRatio2, color);

                // draw bvh skeleton in Game
                GameObject lineObj = new GameObject("line");
                lineObj.tag = "line2";
                LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
                Material material = new Material(Shader.Find("Standard"));
                lineRenderer.material = material;
                lineRenderer.sharedMaterial.SetColor("_Color", Color.blue);
                // lineRenderer.startColor = Color.blue;
                // lineRenderer.endColor = Color.blue;
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, bvhPos[bname] * scaleRatio2);
                lineRenderer.SetPosition(1, bvhPos[bvhHireachy2[bname]] * scaleRatio2);

                GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereObj.tag = "sphere2";
                sphereObj.transform.position = bvhPos[bname] * scaleRatio2;
                sphereObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            }
        }

    }

    // for unity chan animation
    private void SetModelQuatertion(Dictionary<string, Quaternion> unityRot)
    {
        GameObject model = GameObject.Find("unitychan (1)");
        if (model == null)
        {
            Debug.Log("Can't find model");
            return;
        }

        // foreach (Transform joint in model.GetComponentsInChildren<Transform>())
        // {
        //     if (joint != model)
        //     {
        //         string jointName = joint.name;

        //         if (jointName.Contains("Character1_"))
        //         {
        //             jointName = jointName.Replace("Character1_", "");

        //             if (jointName == "Spine" || jointName == "Spine1")
        //             {
        //                 joint.localRotation = Quaternion.identity;
        //             }
        //             if (jointName == "Hips")
        //             {
        //                 Debug.Log(jointName);

        //                 joint.localRotation = unityRot[jointName];
        //             }
        //             // Debug.Log(jointName);
        //             if (bvhHireachy1.ContainsKey(jointName))
        //             {
        //                 // joint.rotation = unityRot[jointName];
        //                 joint.localRotation = unityRot[jointName];
        //             }
        //         }
        //     }
        // }
        foreach (Transform joint in model.GetComponentsInChildren<Transform>())
        {
            if (joint != model)
            {
                string jointName = joint.name;

                if (jointName.Contains("Character1_"))
                {
                    jointName = jointName.Replace("Character1_", "");
                    // Debug.Log(jointName);
                    if (jointName == "Spine" || jointName == "Spine1")
                    {
                        joint.localRotation = Quaternion.identity;
                        continue;
                    }
                    if (jointName == "LeftLowLeg" && bvhHireachy1.ContainsKey(jointName))
                    {
                        // joint.rotation = unityRot[jointName];
                        jointName = "RightLowLeg";
                        joint.localRotation = unityRot[jointName];
                    }
                    if (jointName == "LeftUpLeg" && bvhHireachy1.ContainsKey(jointName))
                    {
                        // joint.rotation = unityRot[jointName];
                        jointName = "RightUpLeg";
                        joint.localRotation = unityRot[jointName];
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
                    if (jointName == "Spine" || jointName == "Spine1")
                    {
                        joint.localRotation = Quaternion.identity;
                        continue;
                    }
                    if (bvhHireachy1.ContainsKey(jointName))
                    {
                        Quaternion rot = joint.localRotation;
                        // Debug.Log(jointName);
                        // Debug.Log("Quternion: " + rot);
                        unityT1.Add(jointName, rot);

                    }
                    else if (jointName == "Hips")
                    {
                        Quaternion rot = joint.localRotation;
                        // Debug.Log(jointName);
                        // Debug.Log("Quternion: " + rot);
                        unityT1.Add(jointName, rot);
                    }
                }
            }
        }
    }

    private void CreateObjPoint(int pointCount)
    {
        for (int index = 0; index < pointCount; index++)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            // Debug.Log(bvhRootPos[index]);
            // Debug.Log("index: " + index);
            point.transform.localPosition = bvhRootPos[index];
            point.name = "Cp" + (index);
            point.tag = "controlPoint";
            point.SetActive(true);
            point.AddComponent(mouseEventScript.GetClass());
            // point.transform.position = new Vector3(ga, 0.0f, gap);
            point.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            controlPoints.Add(point);

            GameObject lineObj = new GameObject("controlPointLine" + index);
            lineObj.tag = "controlPointLine";
            controlPointsLine.Add(lineObj);

            // Debug.Log("index: " + (index + 1));
            // Debug.Log(pointCount);
            // if (index > 0)
            // {
            //     controlPoints[index - 1].transform.parent = GameObject.FindGameObjectWithTag("controlPoint").transform;
            //     controlPointsLine[index - 1].transform.parent = GameObject.FindGameObjectWithTag("controlPointLine").transform;

            // }
            // if (index == (pointCount - 1))
            // {
            //     controlPoints[index].transform.parent = GameObject.FindGameObjectWithTag("controlPoint").transform;
            //     controlPointsLine[index].transform.parent = GameObject.FindGameObjectWithTag("controlPointLine").transform;

            // }
        }
    }

    // get hips pos and rot each frame
    private void GetRootBonePosAndRot()
    {
        int frameNum = bp.frames;

        BVHParser.BVHBone.BVHChannel[] channels = bp.root.channels;
        bvhRootPos.Clear();
        bvhRootRot.Clear();
        // int n_point = frameNum / 10;
        for (int i = 0; i < frameNum; i++)
        {
            Vector3 pos = new Vector3(channels[0].values[i], 0, channels[2].values[i]);
            Vector3 rot = new Vector3(channels[3].values[i], channels[4].values[i], channels[5].values[i]);
            bvhRootPos.Add(pos);
            bvhRootRot.Add(rot);
            // Debug.Log("i: " + i + " pos: " + pos);
        }
        curves.Clear();
        Debug.Log("bvhRootPos: " + bvhRootPos.Count);
        // curves.AddRange(CurveFit.Fit(CurvePreprocess.RdpReduce(bvhRootPos, 5.0f), 0.5f));
        curves.AddRange(CurveFit.Fit(bvhRootPos, 0.5f));
        Debug.Log("curves: " + curves.Count);
        VisualizeControlPoint();
        DrawCurves();
    }

    private void VisualizeControlPoint()
    {
        int maxControlPoint = (curves.Count * 4 - (curves.Count - 1));
        Debug.Log("maxControlPoint before " + maxControlPoint);
        if (bvhRootPos.Count < maxControlPoint)
        {
            maxControlPoint = bvhRootPos.Count;
        }
        Debug.Log("maxControlPoint after" + maxControlPoint);

        CreateObjPoint(maxControlPoint);
        Debug.Log("controlPoints: " + controlPoints.Count);
        for (int i = 0; i < controlPoints.Count; i += 3)
        {
            int offset = 0;
            if (i / 4 > 0) offset = 1;
            int curveIndex = i == 0 ? 0 : (i - 1) / 3;

            controlPoints[i + 1 - offset].transform.position = curves[curveIndex].p1;// * 0.02f;
            controlPoints[i + 2 - offset].transform.position = curves[curveIndex].p2;// * 0.02f;
            controlPoints[i + 3 - offset].transform.position = curves[curveIndex].p3;// * 0.02f;
            // Debug.Log("curves[curveIndex].p1" + curves[curveIndex].p1);
            // Debug.Log("curves[curveIndex].p2" + curves[curveIndex].p2);
            // Debug.Log("curves[curveIndex].p3" + curves[curveIndex].p3);
            if (i == 0)
            {
                controlPoints[i].transform.position = curves[curveIndex].p0;// * 0.02f;
                // Debug.Log("curves[curveIndex].p0" + curves[curveIndex].p0);
                i++;
            }
        }
    }

    private void DrawCurves()
    {
        int n_Samples = 1000 / curves.Count;
        for (int i = 0; i < curves.Count; i++)
        {
            List<Vector3> drawPoints = new List<Vector3>();
            points.Clear();
            pointsTangent.Clear();
            arcLegnth.Clear();
            for (int j = 0; j < n_Samples; ++j)
            {
                float t = (float)j / ((float)n_Samples - 1);
                drawPoints.Add(curves[i].Sample(t));
                points.Add(curves[i].Sample(t));
                pointsTangent.Add(curves[i].Tangent(t));
                if (arcLegnth.Count == 0)
                {
                    arcLegnth.Add(0);
                }
                else
                {
                    arcLegnth.Add(arcLegnth[j - 1] + Vector3.Distance(points[j], points[j - 1]));
                }
            }
            LineRenderer lineRenderer = controlPointsLine[i].GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = controlPointsLine[i].AddComponent<LineRenderer>();
            }
            Material material = new Material(Shader.Find("Standard"));
            lineRenderer.material = material;
            lineRenderer.sharedMaterial.SetColor("_Color", Color.yellow);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            // lineRenderer.startColor = Color.white;
            // lineRenderer.endColor = Color.white;
            lineRenderer.positionCount = drawPoints.Count;
            lineRenderer.SetPositions(drawPoints.ToArray());
        }
    }

    private void Start()
    {

    }

    private void Update()
    {
        if (isBVHLoaded1)
        {
            // getKeyFrame 獲取當前幀在本地座標下的旋轉四元數
            // bvhOffset bvh中所有關節的初始偏移量
            // int nextFrameIdx1 = frameIdx1 + 1;
            // bool isLastFrame = false;
            // if (nextFrameIdx1 < bp.frames - 1)
            // {
            //     nextFrameIdx1++;
            // }
            // else
            // {
            //     isLastFrame = true;
            //     nextFrameIdx1 = 1;
            // }
            Dictionary<string, Quaternion> currFrame = bp.getKeyFrame(frameIdx1);//frameIdx 2871
            // Dictionary<string, Quaternion> nextFrame = bp.getKeyFrame(nextFrameIdx1);
            if (frameIdx1 < bp.frames - 1)
            {
                frameIdx1++;
            }
            else
            {
                frameIdx1 = 0;
            }

            // draw bvh skeleton
            Dictionary<string, Vector3> bvhPos = new Dictionary<string, Vector3>();
            Dictionary<string, Quaternion> unityRot = new Dictionary<string, Quaternion>();
            foreach (string bname in currFrame.Keys)
            {
                // Debug.Log(bname);
                if (bname == "pos")
                {
                    bvhPos.Add(bp.root.name, new Vector3(currFrame["pos"].x, currFrame["pos"].y, currFrame["pos"].z));
                }
                else
                {
                    if (bvhHireachy1.ContainsKey(bname) && bname != bp.root.name)
                    {
                        // Quaternion keyFrameLerp = Quaternion.Lerp(currFrame[bvhHireachy1[bname]], nextFrame[bvhHireachy1[bname]], 0.5f);
                        // Vector3 curpos = bvhPos[bvhHireachy1[bname]] + keyFrameLerp * bvhOffset1[bname];
                        Vector3 curpos = bvhPos[bvhHireachy1[bname]] + currFrame[bvhHireachy1[bname]] * bvhOffset1[bname];
                        bvhPos.Add(bname, curpos);
                    }
                }
                // if (bname != "pos")
                // {

                //     // bvh 沒有T2所以T3等同T4因此unityModel位置等於UnityT * T3
                //     // Quaternion unityCurRot = unityT[bname] * Quaternion.Euler(curpos);// T5
                //     if (bname == "Hips")
                //     {
                //         Quaternion unityCurRot = currFrame[bname] * unityT1[bname];
                //         unityRot.Add(bname, unityCurRot);
                //     }
                //     else
                //     {
                //         Quaternion unityCurRot = currFrame[bvhHireachy1[bname]] * unityT1[bname]; // T3, T4
                //         unityRot.Add(bname, unityCurRot);
                //     }

                // }
            }
            Vector3 modelHipsPos = anim.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 modelRightUpLegPos = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
            Vector3 bvhHipsPos = bvhPos[bp.root.name];
            Vector3 bvhRightUpLegPos = bvhPos["RightUpLeg"];
            scaleRatio1 = Vector3.Distance(modelRightUpLegPos, modelHipsPos) / Vector3.Distance(bvhRightUpLegPos, bvhHipsPos);

            // anim.GetBoneTransform(HumanBodyBones.Hips).position = new Vector3(bvhPos[bp.root.name].x + 150.0f, bvhPos[bp.root.name].y, bvhPos[bp.root.name].z) * scaleRatio;

            ClearLines(1);
            DrawModel(bvhPos, 1);
            // SetModelQuatertion(unityRot);
        }
        if (isBVHLoaded2)
        {
            Dictionary<string, Quaternion> currFrame = bp2.getKeyFrame(frameIdx2);//frameIdx 2871
            if (frameIdx2 < bp2.frames - 1)
            {
                frameIdx2++;
            }
            else
            {
                frameIdx2 = 0;
            }

            // draw bvh skeleton
            Dictionary<string, Vector3> bvhPos = new Dictionary<string, Vector3>();
            Dictionary<string, Quaternion> unityRot = new Dictionary<string, Quaternion>();
            foreach (string bname in currFrame.Keys)
            {
                // Debug.Log(bname);
                if (bname == "pos")
                {
                    bvhPos.Add(bp2.root.name, new Vector3(currFrame["pos"].x - 50.0f, currFrame["pos"].y, currFrame["pos"].z));
                }
                else
                {
                    if (bvhHireachy2.ContainsKey(bname) && bname != bp2.root.name)
                    {
                        Vector3 curpos = bvhPos[bvhHireachy2[bname]] + currFrame[bvhHireachy2[bname]] * bvhOffset2[bname];
                        bvhPos.Add(bname, curpos);
                    }
                }
                // if (bname != "pos")
                // {

                //     // bvh 沒有T2所以T3等同T4因此unityModel位置等於UnityT * T3
                //     // Quaternion unityCurRot = unityT[bname] * Quaternion.Euler(curpos);// T5
                //     if (bname == "Hips")
                //     {
                //         Quaternion unityCurRot = currFrame[bname] * unityT2[bname];
                //         unityRot.Add(bname, unityCurRot);
                //     }
                //     else
                //     {
                //         Quaternion unityCurRot = currFrame[bvhHireachy2[bname]] * unityT2[bname]; // T3, T4
                //         unityRot.Add(bname, unityCurRot);
                //     }

                // }
            }
            Vector3 modelHipsPos = anim.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 modelRightUpLegPos = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg).position;
            Vector3 bvhHipsPos = bvhPos[bp2.root.name];
            Vector3 bvhRightUpLegPos = bvhPos["RightUpLeg"];
            scaleRatio2 = Vector3.Distance(modelRightUpLegPos, modelHipsPos) / Vector3.Distance(bvhRightUpLegPos, bvhHipsPos);

            // anim.GetBoneTransform(HumanBodyBones.Hips).position = new Vector3(bvhPos[bp.root.name].x + 150.0f, bvhPos[bp.root.name].y, bvhPos[bp.root.name].z) * scaleRatio;

            ClearLines(2);
            DrawModel(bvhPos, 2);
            // SetModelQuatertion(unityRot);
        }
    }
}
