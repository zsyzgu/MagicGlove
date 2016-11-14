using UnityEngine;
using Leap;
using System.Collections;

public class Finger : MonoBehaviour {
    const int DIMENSION = 63;
    const int SPHERE_NUM = 21;
    const int CYLINER_NUM = 15;
    private Controller handController;
    private GameObject[] spheres = new GameObject[SPHERE_NUM];
    private GameObject[] cylinders = new GameObject[CYLINER_NUM];
    private Predict predict;
    
	void Start () {
        handController = new Controller();
        predict = new Predict();
	}
	
	void Update () {
        drawHand(getFingerData());
	}

    public void draw(int[] vec) {
        drawHand(predict.getFinderData(vec));
    }

    public float[] getFingerData() {
        float[] ret = null;

        foreach (Hand hand in handController.Frame().Hands) {
            if (hand.IsRight) {
                if (ret == null) {
                    ret = new float[DIMENSION];
                }
                ret[0] = hand.PalmPosition.x;
                ret[1] = hand.PalmPosition.y;
                ret[2] = hand.PalmPosition.z;
                for (int i = 0; i < hand.Fingers.Count; i++) {
                    Leap.Finger finger = hand.Fingers[i];
                    Bone bone1 = finger.Bone(Bone.BoneType.TYPE_DISTAL);
                    Bone bone2 = finger.Bone(Bone.BoneType.TYPE_INTERMEDIATE);
                    Bone bone3 = finger.Bone(Bone.BoneType.TYPE_PROXIMAL);
                    Bone bone4 = finger.Bone(Bone.BoneType.TYPE_METACARPAL);
                    ret[i * 12 + 3] = bone1.PrevJoint.x;
                    ret[i * 12 + 4] = bone1.PrevJoint.y;
                    ret[i * 12 + 5] = bone1.PrevJoint.z;
                    ret[i * 12 + 6] = bone2.PrevJoint.x;
                    ret[i * 12 + 7] = bone2.PrevJoint.y;
                    ret[i * 12 + 8] = bone2.PrevJoint.z;
                    ret[i * 12 + 9] = bone3.PrevJoint.x;
                    ret[i * 12 + 10] = bone3.PrevJoint.y;
                    ret[i * 12 + 11] = bone3.PrevJoint.z;
                    ret[i * 12 + 12] = bone4.PrevJoint.x;
                    ret[i * 12 + 13] = bone4.PrevJoint.y;
                    ret[i * 12 + 14] = bone4.PrevJoint.z;
                }
            }
        }

        return ret;
    }

    void drawLine(int id, int u, int v) {
        Vector3 p0 = spheres[u].transform.position;
        Vector3 p1 = spheres[v].transform.position;

        cylinders[id].transform.position = (p1 - p0) / 2.0f + p0;
        Vector3 v3T = cylinders[id].transform.localScale;
        v3T.y = (p1 - p0).magnitude / 2.0f;
        cylinders[id].transform.localScale = v3T;
        cylinders[id].transform.rotation = Quaternion.FromToRotation(Vector3.up, p1 - p0);
    }

    public void drawHand(float[] fingerData) {
        if (fingerData == null) {
            for (int i = 0; i < SPHERE_NUM; i++) {
                if (spheres[i] != null) {
                    Destroy(spheres[i]);
                    spheres[i] = null;
                }
            }
            for (int i = 0; i < CYLINER_NUM; i++) {
                if (cylinders[i] != null) {
                    Destroy(cylinders[i]);
                    cylinders[i] = null;
                }
            }
        } else {
            for (int i = 0; i < SPHERE_NUM; i++) {
                if (spheres[i] == null) {
                    spheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }
                spheres[i].transform.position = new Vector3(fingerData[i * 3], fingerData[i * 3 + 1], fingerData[i * 3 + 2]);
                spheres[i].transform.localScale = new Vector3(15.0f, 15.0f, 15.0f);
                spheres[i].GetComponent<Renderer>().material.color = Color.red;
            }
            for (int i = 0; i < CYLINER_NUM; i++) {
                if (cylinders[i] == null) {
                    cylinders[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cylinders[i].transform.localScale = new Vector3(5.0f, 15.0f, 5.0f);
                }
            }
            int cnt = 0;
            for (int i = 1; i < SPHERE_NUM; i++) {
                if (i % 4 != 0) {
                    drawLine(cnt++, i, i + 1);
                }
            }
        }
    }
}
