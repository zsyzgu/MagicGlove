using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using HandRecoginition;

public class MainCamera : MonoBehaviour {
    WebCamTexture webCamera;
    List<GameObject> objList = new List<GameObject>();
    Mat src;
    PointRecognition pointRecognition = new PointRecognition();

    void Start () {
        StartCoroutine(startWebCamera());
        src = new Mat("2.jpg", LoadMode.GrayScale);
        //deal();
    }

    void Update() {
        deal();
    }

    void OnGUI() {
        if (webCamera != null) {
            //GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), webCamera);
        }
    }

    void deal() {
        if (webCamera != null) {

        }

        Vector3[] points = null;
        Point[] coords = null;
        pointRecognition.recognize(src, out points, out coords);

        /*for (int i = 0; i < objList.Count; i++) {
            GameObject.Destroy(objList[i]);
        }
        objList.Clear();

        for (int i = 0; i < points.Length; i++) {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            sphere.transform.position = points[i];
            if (coords[i].X == 0 && coords[i].Y == 0) {
                sphere.GetComponent<Renderer>().material.color = Color.red;
            }
            objList.Add(sphere);
        }*/

        Mat oup = (new Mat("2.jpg", LoadMode.Color));
        for (int i = 0; i < points.Length; i++) {
            oup.Circle(new Point(points[i].x, points[i].y), 0, Scalar.Red, 2);
            if (coords[i].X == 0 && coords[i].Y == 0) {
                oup.Circle(new Point(points[i].x, points[i].y), 0, Scalar.Blue, 2);
            }
        }

        Cv.SaveImage("image.jpg", oup.ToCvMat());
    }

    IEnumerator startWebCamera() {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
            WebCamDevice[] devices = WebCamTexture.devices;
            string deviceName = devices[0].name;
            if (devices.Length > 2 && devices[1].name == "Logitech HD Webcam C525") {
                deviceName = devices[1].name;
            }
            webCamera = new WebCamTexture(deviceName);
            webCamera.Play();
        }
    }
}
