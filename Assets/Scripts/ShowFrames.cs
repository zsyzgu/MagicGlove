using UnityEngine;
using System.Collections;

public class ShowFrames : MonoBehaviour {
    int frames = 0;
    float times = 0f;
    
	void Start () {
	
	}
	
	void Update () {
        times += Time.deltaTime;
        frames++;
	    if (times >= 1f) {
            float rate = frames / times;
            Debug.Log(rate);
            times = 0f;
            frames = 0;
        }
	}
}
