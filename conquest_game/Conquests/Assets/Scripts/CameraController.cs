using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed;
    public float scrollModifier;

    public void HandleInput()
    {
        transform.position += Input.GetAxis("Vertical") * Vector3.up * speed * Time.deltaTime;
        transform.position += Input.GetAxis("Horizontal") * Vector3.right * speed * Time.deltaTime;
        transform.position += Input.GetAxis("Mouse ScrollWheel") * Vector3.forward * speed * Time.deltaTime * scrollModifier;
        Vector3 pos = transform.position;
        pos.z = Mathf.Clamp(pos.z, -3000f, -500f);
        transform.position = pos;
    }
}
