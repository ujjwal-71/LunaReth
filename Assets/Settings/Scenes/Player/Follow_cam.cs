using UnityEngine;
using UnityEngine.UIElements;

public class Follow_cam : MonoBehaviour
{
    private Transform player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player_0").transform;
    }

    // Update is called once per frame
        void Update()
        {
            Vector3 pos = transform.position;
            pos.x = player.position.x;
            transform.position = pos;
        }
}
