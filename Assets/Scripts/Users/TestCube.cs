using Core;
using UnityEngine;

namespace Users
{
    public class TestCube : PortalUser
    {
        public float maxSpeed = 1;
        float speed;
        float targetSpeed;
        float smoothV;
        private bool isMoving;

        void Start()
        {
            targetSpeed = maxSpeed;
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                isMoving = true;
            }

            if (isMoving)
            {
                transform.position += transform.forward * Time.deltaTime * speed;

                if (Input.GetKeyDown(KeyCode.C))
                {
                    targetSpeed = (targetSpeed == 0) ? maxSpeed : 0;
                }

                speed = Mathf.SmoothDamp(speed, targetSpeed, ref smoothV, .5f);
            }
        }
    }
}