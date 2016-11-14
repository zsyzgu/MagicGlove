using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using System.IO;
using HandRecoginition;

public class MainCamera : MonoBehaviour {
    const int R = 37;
    const int C = 65;
    const string DIR = "/../Data/";
    WebCamTexture webCamera;
    Texture2D output;

    int dataId = 0;
    StreamWriter dataWriter;

    void Start () {
        StartCoroutine(startWebCamera());
        dataWriter = new StreamWriter(Application.dataPath + DIR + "data.txt", true);
    }

    void Update() {
        if (webCamera != null) {
            //int[,] mat = new PointRecognition().getMatFromImage();
            int[,] mat = getMatFromCamera();
            int[] vec = new PointRecognition().recognize(mat);

            showOutput(vec);
            if (dataId % 20 == 0) {
                saveImage();
            }
            saveData(vec);
            dataId++;
        }
    }

    void OnGUI() {
        if (output != null) {
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), output);
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
        Color[] colors = webCamera.GetPixels();
        int cnt = 0;
        for (int y = 0; y < H; y++) {
            for (int x = 0; x < W; x++) {
                mat[x, y] = (int)(colors[cnt++].grayscale * 255);
            }
        }

        return mat;
    }

    private void drawPoint(int oX, int oY, Color color) {
        for (int x = oX - 1; x <= oX + 1; x++) {
            for (int y = oY - 1; y <= oY + 1; y++) {
                if (0 <= x && x < webCamera.width && 0 <= y && y < webCamera.height) {
                    output.SetPixel(x, y, color);
                }
            }
        }
    }

    private void showOutput(int[] vec) {
        if (output == null) {
            output = new Texture2D(webCamera.width, webCamera.height);
        }
        output.SetPixels(webCamera.GetPixels());
        for (int i = 0; i < vec.Length; i += 2) {
            if (vec[i] != -1) {
                int r = (i / 2) / C;
                int c = (i / 2) % C;
                if (r == R / 2 || c == C / 2) {
                    drawPoint(vec[i], vec[i + 1], Color.blue);
                } else {
                    drawPoint(vec[i], vec[i + 1], Color.red);
                }
            }
        }
        output.Apply();
    }

    private void saveImage() {
        byte[] bytes = output.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + DIR + dataId + ".png", bytes);
        output.EncodeToJPG();
    }

    private void saveData(int[] vec) {
        dataWriter.Write(dataId);
        for (int i = 0; i < vec.Length; i++) {
            dataWriter.Write(" " + vec[i]);
        }
        float[] finger = GetComponent<Finger>().getFingerData();
        if (finger == null) {
            for (int i = 0; i < 63; i++) {
                dataWriter.Write(" 0");
            }
        } else {
            for (int i = 0; i < finger.Length; i++) {
                dataWriter.Write(" " + finger[i]);
            }
        }
        dataWriter.WriteLine();
    }

    IEnumerator startWebCamera() {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (Application.HasUserAuthorization(UserAuthorization.WebCam)) {
            WebCamDevice[] devices = WebCamTexture.devices;
            string deviceName = devices[0].name;
            for (int i = 0; i < devices.Length; i++) {
                if (devices[i].name == "Logitech HD Webcam C525") {
                    deviceName = devices[i].name;
                }
            }
            webCamera = new WebCamTexture(deviceName);
            webCamera.Play();
        }
    }
}
