using StarterAssets;
using UnityEngine;

public class PlayerHeadBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTarget;

    [Header("Bobbing Frequency")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float crouchBobSpeed = 10f;

    [Header("Bobbing Amount")]
    [SerializeField] private float bobHorizontalAmount = 0.05f;
    [SerializeField] private float bobVerticalAmount = 0.05f;

    private FirstPersonController _controller;
    private StarterAssetsInputs _input;

    private float _timer;
    private float _defaultCameraX;

    private void Start()
    {
        _controller = GetComponent<FirstPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
    
        if (cameraTarget != null)
        {
            _defaultCameraX = cameraTarget.localPosition.x;
        }
    }

    private void LateUpdate()
    {
        if (cameraTarget == null || _controller == null) return;

        float speed = _controller.HorizontalVelocity.magnitude;

        if (speed < 0.1f || !_controller.Grounded)
        {
            _timer = 0f;

            Vector3 resetPos = cameraTarget.localPosition;
            resetPos.x = Mathf.Lerp(resetPos.x, _defaultCameraX, Time.deltaTime * 8f);
            cameraTarget.localPosition = resetPos;
            
            return;
        }

        float currentBobSpeed = walkBobSpeed;
        float speedMultiplier = 1f;

        if (_input.crouch)
        {
            currentBobSpeed = crouchBobSpeed;
        }
        else if (_input.sprint)
        {
            currentBobSpeed = sprintBobSpeed;
            speedMultiplier = 1.3f;
        }

        _timer += Time.deltaTime * currentBobSpeed;

        float offsetX = Mathf.Cos(_timer) * bobHorizontalAmount * speedMultiplier;
        float offsetY = Mathf.Sin(_timer * 2f) * bobVerticalAmount * speedMultiplier;

        Vector3 finalLocalPosition = cameraTarget.localPosition;
        finalLocalPosition.x = _defaultCameraX + offsetX;
        finalLocalPosition.y += offsetY;

        cameraTarget.localPosition = finalLocalPosition;
    }
}
