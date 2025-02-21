using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The target transform the camera will follow (typically the player).")]
    private Transform target;

    [SerializeField]
    [Tooltip("The vertical offset of the camera from the target.")]
    private float heightOffset = 5f;

    [SerializeField]
    [Tooltip("The distance of the camera from the target.")]
    private float distance = 7f;

    [SerializeField]
    [Tooltip("The speed at which the camera adjusts its position.")]
    private float followSpeed = 10f;

    [SerializeField]
    [Tooltip("The speed at which the camera adjusts its rotation.")]
    private float rotationSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("ThirdPersonCamera: No target assigned to the camera.");
            return;
        }

        FollowTarget();
    }

    private void FollowTarget()
    {
        // Calculate the desired camera position
        Vector3 targetPosition = target.position;
        Vector3 backwardDirection = -target.forward; // Camera should be behind the player
        Vector3 desiredPosition = targetPosition + backwardDirection * distance + Vector3.up * heightOffset;

        // Smoothly interpolate the camera's position
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to always look at the player
        Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Sets the target for the camera to follow.
    /// </summary>
    /// <param name="newTarget">The new target transform.</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}

