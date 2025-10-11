using UnityEngine;
using UnityEngine.XR;

namespace FluxFramework.VR
{
    /// <summary>
    /// A dependency-free component that tracks the position and/or rotation of an XR device
    /// and applies it to the GameObject's transform. This is a custom implementation
    /// that uses the modern UnityEngine.XR.InputDevices API for better performance.
    /// </summary>
    public class CustomTrackedPoseDriver : MonoBehaviour
    {
        public enum TrackingType
        {
            RotationAndPosition,
            RotationOnly,
            PositionOnly
        }
        
        public enum TrackedPose
        {
            LeftEye,
            RightEye,
            Center,
            Head,
            LeftHand,
            RightHand
        }
        
        [Tooltip("The specific XR pose to track (e.g., the Headset or a Controller).")]
        public TrackedPose poseToTrack = TrackedPose.Center;
        
        [Tooltip("Specifies whether to track position, rotation, or both.")]
        public TrackingType trackingType = TrackingType.RotationAndPosition;
        
        [Tooltip("If true, tracking is updated in the regular Update loop.")]
        public bool updateInUpdate = true;
        
        [Tooltip("If true, tracking is updated just before rendering to reduce latency. Recommended for the camera.")]
        public bool updateInBeforeRender = false;
        
        private InputDevice _device;
        private XRNode _node;

        void OnEnable()
        {
            _node = GetXRNode(poseToTrack);
            _device = InputDevices.GetDeviceAtXRNode(_node);
            if (!_device.isValid)
            {
                InputDevices.deviceConnected += OnDeviceConnected;
            }
        }

        void OnDisable()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
        }

        private void OnDeviceConnected(InputDevice device)
        {
            if (device.role == _device.role)
            {
                _device = device;
            }
        }

        void Update()
        {
            if (updateInUpdate)
            {
                UpdatePose();
            }
        }
        
        void OnBeforeRender()
        {
            if (updateInBeforeRender)
            {
                UpdatePose();
            }
        }
        
        private void UpdatePose()
        {
            if (!_device.isValid)
            {
                // Try to re-acquire the device if it has disconnected and reconnected
                _device = InputDevices.GetDeviceAtXRNode(_node);
                if (!_device.isValid) return;
            }
            
            // Apply Position
            if (trackingType != TrackingType.RotationOnly)
            {
                if (_device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
                {
                    transform.localPosition = position;
                }
            }
            
            // Apply Rotation
            if (trackingType != TrackingType.PositionOnly)
            {
                if (_device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
                {
                    transform.localRotation = rotation;
                }
            }
        }
        
        private XRNode GetXRNode(TrackedPose pose)
        {
            switch (pose)
            {
                case TrackedPose.LeftEye: return XRNode.LeftEye;
                case TrackedPose.RightEye: return XRNode.RightEye;
                case TrackedPose.Center: return XRNode.CenterEye;
                case TrackedPose.Head: return XRNode.Head;
                case TrackedPose.LeftHand: return XRNode.LeftHand;
                case TrackedPose.RightHand: return XRNode.RightHand;
                default: return XRNode.CenterEye;
            }
        }
    }
}