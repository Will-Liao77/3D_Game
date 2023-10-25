using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts;
using System.Windows.Forms;

public class BVHDriver : MonoBehaviour
{
    [Header("Loader settings")]
    [Tooltip("This is the target avatar for which the animation should be loaded. Bone names should be identical to those in the BVH file and unique. All bones should be initialized with zero rotations. This is usually the case for VRM avatars.")]
    public Animator targetAvatar;
    [Tooltip("This is the path to the BVH file that should be loaded. Bone offsets are currently being ignored by this loader.")]
    public float frameRate = 60.0f;
    [Tooltip("If the BVH first frame is T(if not,make sure the defined skeleton is T).")]
    public bool FirstT;
    private BVHParser bp = null;
    private Animator anim;
    private bool isBVHLoaded = false;

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
            unityT = new Dictionary<HumanBodyBones, Quaternion>();

            frameIdx = 0;

            isBVHLoaded = true;
            return;
        }

        throw new ArgumentException("Failed to Load BVH File");
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
}
