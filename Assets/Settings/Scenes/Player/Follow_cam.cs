using UnityEngine;
using UnityEngine.UIElements;

public class Follow_cam : MonoBehaviour
{
    private Transform player;
    public float UP_X=0;
    public float LW_X=0;
    public float UP_Y=0;
    public float LW_Y=0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.Find("Player_0").transform;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 screenPos = Camera.main.WorldToViewportPoint(player.position);
        Vector3 pos = transform.position;

        if(pos.x <= LW_X)
        {
            if (screenPos.x > 0.6)
            {
                pos.x += 15 * Time.deltaTime;
            }
        }
        else if(pos.x >= UP_X)
        {
            if(screenPos.x < 0.4)
            {
                pos.x -= 15 * Time.deltaTime;    
            }
        }
        else
        {
            if(screenPos.x < 0.45)
            {
                pos.x -= 16 * Time.deltaTime;
            }
            else if(screenPos.x > 0.55)
            {
                pos.x += 16 * Time.deltaTime;
            }
        }
        if(pos.y < LW_Y)
        {
            if (screenPos.y >= 0.6)
            {
                pos.y += 15 * Time.deltaTime;
            }
        }
        else if(pos.y >= UP_Y)
        {
            if(screenPos.y < 0.4)
            {
                pos.y -= 15 * Time.deltaTime;
            }
        }
        else
        {
            if(screenPos.y < 0.25)
            {
                pos.y -= 16 * Time.deltaTime;
            }
            else if(screenPos.y > 0.65)
            {
                pos.y += 16 * Time.deltaTime;
            }
        }
        transform.position = pos;
    }
}
