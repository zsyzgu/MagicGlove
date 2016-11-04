using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using HandRecoginition;

public class MainCamera : MonoBehaviour {
    WebCamTexture webCamera;
    Texture2D output;
    List<GameObject> objList = new List<GameObject>();
    PointRecognition recognition = new PointRecognition();

    void Start () {
        StartCoroutine(startWebCamera());
        //deal();
    }

    void Update() {
        deal();
    }

    void OnGUI() {
        if (output != null) {
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), output);
        }
    }

    void deal() {
        if (webCamera != null) {
            //int[,] mat = recognition.getMatFromImage();
            int[,] mat = getMatFromCamera();
            Vector3[] points = null;
            Point[] coords = null;
            recognition.recognize(mat, out points, out coords);

            showOutput(points, coords);
            //showBalls(points, coords);
            //saveImage(points, coords);
            //saveImage(mat);
        }
    }
    
    private int[,] getMatFromCamera() {
        int[,] mat = null;
        if (webCamera == null) {
            return mat;
        }
        int W = webCamera.width;
        int H = webCamera.height;
        mat = new int[W, H];
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                mat[x, y] = (int)(webCamera.GetPixel(x, y).grayscale * 255);
            }
        }

        return mat;
    }

    private void showOutput(Vector3[] points, Point[] coords) {
        int W = webCamera.width;
        int H = webCamera.height;
        if (output == null) {
            output = new Texture2D(W, H);
        }
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                output.SetPixel(x, y, webCamera.GetPixel(x, y));
            }
        }
        for (int i = 0; i < points.Length; i++) {
            for (int x = (int)points[i].x - 1; x <= (int)points[i].x + 1; x++) {
                for (int y = (int)points[i].y - 1; y <= (int) points[i].y + 1; y++) {
                    if (0 <= x && x < W && 0 <= y && y < H) {
                        output.SetPixel(x, y, Color.red);
                        if (coords[i].X == 0 && coords[i].Y == 0) {
                            output.SetPixel(x, y, Color.blue);
                        }
                    }
                }
            }
        }
        output.Apply();
    }

    private void showBalls(Vector3[] points, Point[] coords) {
        for (int i = 0; i < objList.Count; i++) {
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
        }
    }

    private void saveImage(Vector3[] points, Point[] coords) {
        Mat oup = new Mat("input.jpg", LoadMode.Color);
        for (int i = 0; i < points.Length; i++) {
            oup.Circle(new Point(points[i].x, points[i].y), 0, Scalar.Red, 2);
            if (coords[i].X == 0 && coords[i].Y == 0) {
                oup.Circle(new Point(points[i].x, points[i].y), 0, Scalar.Blue, 2);
            }
        }

        Cv.SaveImage("output.jpg", oup.ToCvMat());
    }

    private void saveImage(int[,] mat) {
        Mat oup = new Mat("input.jpg", LoadMode.GrayScale);
        int W = oup.Width;
        int H = oup.Height;
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                oup.Set<int>(H - y - 1, x, mat[x, y]);
            }
        }

        Cv.SaveImage("output.jpg", oup.ToCvMat());
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
