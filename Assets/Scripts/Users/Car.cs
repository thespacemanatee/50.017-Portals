using Core;
using UnityEngine;

namespace Users
{
    public class Car : PortalUser
    {
        private Rigidbody _rigidbody;
        // Start is called before the first frame update
        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                _rigidbody.AddForce(Vector3.forward * 10, ForceMode.Impulse);
            }
        }
    }
}
