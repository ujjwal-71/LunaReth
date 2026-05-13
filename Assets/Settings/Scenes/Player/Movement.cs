using UnityEngine;

public class Movement : MonoBehaviour
{
    private Rigidbody2D RB;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB = GetComponent<Rigidbody2D>();
    }

    public float thrust = 1;
    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.D))
        {
            RB.AddForceX(transform.up *thrust);
        }
    }
}
