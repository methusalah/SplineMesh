using UnityEngine;
using UnityEditor;

public class DemoCamera : MonoBehaviour {
    public GameObject startExample;
    public GameObject endExample;
    public float speed;

    private void Start() {
        transform.position = new Vector3(
            startExample.transform.position.x,
            transform.position.y,
            transform.position.z);
    }

    private void Update() {
        transform.position -= Vector3.right * speed * Time.deltaTime;
        if(transform.position.x < endExample.transform.position.x) {
            transform.position = new Vector3(
                startExample.transform.position.x,
                transform.position.y,
                transform.position.z);
        }
    }
}