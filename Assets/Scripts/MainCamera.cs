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
        deal();
    }

    void OnGUI() {
        if (output != null) {
            GUI.DrawTexture(new UnityEngine.Rect(0, 0, Screen.width, Screen.height), output);
        }
    }

    void deal() {
        if (webCamera != null) {
            //int[,] mat = new PointRecognition().getMatFromImage();
            int[,] mat = getMatFromCamera();
            int[] vec = new PointRecognition().recognize(mat);
            
            showOutput(vec);
            saveImage();
            saveData(vec);
            dataId++;
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
        for (int x = 0; x < webCamera.width; x++) {
            for (int y = 0; y < webCamera.height; y++) {
                output.SetPixel(x, y, webCamera.GetPixel(x, y));
            }
        }
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
        dataWriter.WriteLine();
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
