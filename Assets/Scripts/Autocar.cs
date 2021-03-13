using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Autocar : MonoBehaviour
{
    [SerializeField] Camera fpcamera;
    
    [SerializeField] float range = 10f;

    [SerializeField] float ranged = 30f;
    public float thrust = 500f;
    public float turnSpeed = 30.0f;
    new private Rigidbody rigidbody;
    void Start()
    {
         rigidbody = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        rigidbody.AddForce(transform.forward * thrust, ForceMode.Force);
        Raycast();
    }
    private void Raycast()
    {

        RaycastHit hit;
        if (Physics.Raycast(fpcamera.transform.position, fpcamera.transform.forward, out hit, ranged))
        {
            Vector3 curRot = fpcamera.transform.rotation.eulerAngles;
            float yaw = curRot.y + 1;
            fpcamera.transform.rotation = Quaternion.Euler(0, yaw, 0);
            if (Physics.Raycast(fpcamera.transform.position, fpcamera.transform.forward, out hit, range))
            {
                
             
                float turn = -1f;
               
                transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);
                
                curRot = fpcamera.transform.rotation.eulerAngles;
                yaw = curRot.y - 1;
                fpcamera.transform.rotation = Quaternion.Euler(0, yaw, 0);
                curRot = fpcamera.transform.rotation.eulerAngles;
                yaw = curRot.y - 1;
                fpcamera.transform.rotation = Quaternion.Euler(0, yaw, 0);


            }


            else if (Physics.Raycast(fpcamera.transform.position, fpcamera.transform.forward, out hit, range))
            {
                rigidbody.AddForce(transform.forward * thrust, ForceMode.Force);
                // the horizontal axis controls the turn
                float turn = 1f;
                // turn the car
                transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);
                        // the vertical axis controls acceleration fwd/back

                curRot = fpcamera.transform.rotation.eulerAngles;
                yaw = curRot.y + 1;
                fpcamera.transform.rotation = Quaternion.Euler(0, yaw, 0);

            }

            else
            {
                return;
            }
        }
        else
        {
            return ;
        }
    }
  
}
