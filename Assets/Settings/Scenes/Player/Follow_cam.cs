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
        if(player.position.x>= 5 && player.position.x <=14)
        {
            pos.x = player.position.x - 5;
            transform.position = pos;
        }

        Vector3 screenPos = Camera.main.WorldToViewportPoint(player.position);
        

        if(screenPos.y > 0.7)
        {
            pos.y += 10f * Time.deltaTime;
            transform.position = pos;
        }
        else if(screenPos.y < 0.2)
        {
            pos.y -= 10f *  Time.deltaTime;
            transform.position = pos;
        }
    }
}
