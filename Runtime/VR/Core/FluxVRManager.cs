using UnityEngine;
using UnityEngine.XR;
using FluxFramework.Core;
using FluxFramework.Attributes;
using FluxFramework.VR.Events;
using System.Collections.Generic;
using System.Linq;

namespace FluxFramework.VR
{
    /// <summary>
    /// A singleton-like manager for the VR system. It handles the lifecycle of XR devices,
    /// spawns FluxVRControllers, and provides global reactive properties for the HMD and play area.
    /// </summary>
    public class FluxVRManager : FluxMonoBehaviour
    {
        [Header("VR Configuration")]
        [Tooltip("If true, the VR system will be initialized automatically on Start.")]
        [SerializeField] private bool autoInitializeVR = true;
        
        [Tooltip("Sets the tracking space type for the XR rig (RoomScale is recommended).")]
        [SerializeField] private VRTrackingSpace trackingSpace = VRTrackingSpace.RoomScale;
        
        // --- Reactive Property References ---
        private IReactiveProperty<Vector3> _hmdPositionProp;
        private IReactiveProperty<Quaternion> _hmdRotationProp;
        private IReactiveProperty<bool> _hmdIsTrackedProp;
        private IReactiveProperty<Bounds> _playAreaBoundsProp;
        private IReactiveProperty<int> _activeControllerCountProp;

        private readonly Dictionary<XRNode, FluxVRController> _controllers = new Dictionary<XRNode, FluxVRController>();

        protected override void Awake()
        {
            base.Awake();
            
            _hmdPositionProp = FluxManager.Instance.GetOrCreateProperty<Vector3>("vr.hmd.position");
            _hmdRotationProp = FluxManager.Instance.GetOrCreateProperty<Quaternion>("vr.hmd.rotation");
            _hmdIsTrackedProp = FluxManager.Instance.GetOrCreateProperty<bool>("vr.hmd.isTracked");
            _playAreaBoundsProp = FluxManager.Instance.GetOrCreateProperty<Bounds>("vr.playarea.bounds");
            _activeControllerCountProp = FluxManager.Instance.GetOrCreateProperty<int>("vr.controllers.count");
        }

        protected virtual void Start()
        {
            if (autoInitializeVR)
            {
                InitializeVR();
            }
            
            InputDevices.deviceConnected += OnDeviceChanged;
            InputDevices.deviceDisconnected += OnDeviceChanged;
        }

        protected virtual void LateUpdate()
        {
            UpdateHMDTracking();
            UpdateControllerCount();
        }

        protected override void OnDestroy()
        {
            InputDevices.deviceConnected -= OnDeviceChanged;
            InputDevices.deviceDisconnected -= OnDeviceChanged;
            base.OnDestroy();
        }

        private void InitializeVR()
        {
            XRDevice.SetTrackingSpaceType((TrackingSpaceType)trackingSpace);
            RefreshAllControllers();
            
            string deviceName = XRSettings.loadedDeviceName;
            bool isActive = XRSettings.isDeviceActive;
            PublishEvent(new VRInitializedEvent(isActive, deviceName));
            Debug.Log("[FluxFramework] FluxVRManager Initialized.", this);
        }

        private void UpdateHMDTracking()
        {
            var headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (headDevice.isValid)
            {
                headDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 position);
                _hmdPositionProp.Value = position;
                
                headDevice.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation);
                _hmdRotationProp.Value = rotation;
                
                headDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked);
                _hmdIsTrackedProp.Value = tracked;
            }
            else if (_hmdIsTrackedProp.Value)
            {
                _hmdIsTrackedProp.Value = false;
            }
        }

        private void UpdateControllerCount()
        {
            if (_activeControllerCountProp.Value != _controllers.Count)
            {
                _activeControllerCountProp.Value = _controllers.Count;
            }
        }

        private void OnDeviceChanged(InputDevice device)
        {
            Debug.Log($"[FluxFramework] VR device configuration changed for: {device.name}. Refreshing controllers.");
            // A simple and robust way to handle both connections and disconnections.
            RefreshAllControllers();
        }

        private void RefreshAllControllers()
        {
            var foundDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, foundDevices);

            var foundNodes = new HashSet<XRNode>();

            // Add or update controllers for currently connected devices
            foreach (var device in foundDevices)
            {
                XRNode node = GetNodeForDevice(device);
                if (node == XRNode.GameController) continue; // Skip unknown/generic controllers
                
                foundNodes.Add(node);

                if (!_controllers.ContainsKey(node))
                {
                    var controllerGO = new GameObject($"FluxVR Controller ({node})");
                    controllerGO.transform.SetParent(transform, false);
                    
                    var controller = controllerGO.AddComponent<FluxVRController>();
                    controller.Initialize(device, node);
                    _controllers[node] = controller;
                    
                    PublishEvent(new VRControllerConnectedEvent(node, device.name));
                }
            }

            // Remove controllers for devices that are no longer connected
            var nodesToRemove = _controllers.Keys.Except(foundNodes).ToList();
            foreach (var node in nodesToRemove)
            {
                if (_controllers.TryGetValue(node, out var controllerToDestroy) && controllerToDestroy != null)
                {
                    Destroy(controllerToDestroy.gameObject);
                }
                _controllers.Remove(node);
                PublishEvent(new VRControllerDisconnectedEvent(node));
            }
        }

        private XRNode GetNodeForDevice(InputDevice device)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left)) return XRNode.LeftHand;
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right)) return XRNode.RightHand;
            return XRNode.GameController; // A generic fallback
        }
        
        public FluxVRController GetController(XRNode hand) => _controllers.TryGetValue(hand, out var controller) ? controller : null;
        public bool IsVRActive => XRSettings.enabled && XRSettings.isDeviceActive;
    }

    public enum VRTrackingSpace
    {
        Stationary = 0,
        RoomScale = 1
    }
}