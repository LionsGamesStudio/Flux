#if FLUX_VR_SUPPORT
using UnityEngine;
using TMPro;
using FluxFramework.UI;
using FluxFramework.Attributes;

namespace FluxFramework.VR.UI
{
    /// <summary>
    /// A VR-optimized text component that extends FluxText.
    /// It provides features like automatic scaling with distance and billboarding to face the camera.
    /// It can optionally convert a UI TextMeshProUGUI into a 3D TextMeshPro for better rendering in VR.
    /// </summary>
    public class FluxVRText : FluxText
    {
        [Header("VR Text Configuration")]
        [Tooltip("If true, the component will attempt to create and manage a 3D TextMeshPro component for rendering.")]
        [SerializeField] private bool use3DText = false;
        
        [Tooltip("If true, the text object will dynamically scale to maintain a consistent perceived size as the player moves.")]
        [SerializeField] private bool scaleWithDistance = true;
        
        [Tooltip("If true, the text will always rotate to face the VR camera.")]
        [SerializeField] private bool billboardToCamera = true;

        [Header("VR Scaling Options")]
        [Tooltip("The distance at which the text has a scale of 1.")]
        [SerializeField] private float optimalViewingDistance = 2f;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 2f;

        // --- Private State ---
        private Camera _vrCamera;
        private TextMeshPro _textMesh3D;
        private Vector3 _originalScale;
        
        /// <summary>
        /// Overrides the base Awake to initialize VR-specific components and logic.
        /// </summary>
        protected override void InitializeComponent()
        {
            base.InitializeComponent();
                        
            _vrCamera = Camera.main;
            _originalScale = transform.localScale;
            
            if (use3DText)
            {
                Setup3DText();
            }
        }
        
        /// <summary>
        /// Update is used for VR-specific visual features that need to be adjusted every frame.
        /// </summary>
        protected virtual void Update()
        {
            if (_vrCamera != null)
            {
                if (scaleWithDistance) UpdateDistanceScaling();
                if (billboardToCamera) UpdateBillboard();
            }
        }

        /// <summary>
        /// This is the core of the VR enhancement. It overrides the base SetText method.
        /// When the data binding updates the text, this method ensures that BOTH the original
        /// UI component (if needed) and the new 3D text component are synchronized.
        /// </summary>
        /// <param name="newText">The new text value from the binding.</param>
        public override void SetText(string newText)
        {
            // First, call the base implementation to update the underlying UI Text or TextMeshProUGUI.
            // This keeps the original functionality intact.
            base.SetText(newText);

            // Then, add the specific logic for this VR class: update the 3D text component.
            if (use3DText && _textMesh3D != null)
            {
                _textMesh3D.text = newText;
            }
        }
        
        private void Setup3DText()
        {
            // This method converts a UI TextMeshProUGUI into a world-space TextMeshPro component.
            var textCompUI = GetComponent<TextMeshProUGUI>();
            if (textCompUI != null)
            {
                // Find or create the 3D text child object.
                var existing3D = GetComponentInChildren<TextMeshPro>();
                if (existing3D != null && existing3D.transform != transform)
                {
                    _textMesh3D = existing3D;
                }
                else
                {
                    var go = new GameObject($"{gameObject.name} (3D Text)");
                    go.transform.SetParent(transform, false);
                    _textMesh3D = go.AddComponent<TextMeshPro>();
                }
                
                // Copy properties from the UI text to the 3D text.
                _textMesh3D.text = textCompUI.text;
                _textMesh3D.font = textCompUI.font;
                _textMesh3D.fontSize = textCompUI.fontSize * 0.1f; // Adjust font size for world space
                _textMesh3D.color = textCompUI.color;
                _textMesh3D.alignment = textCompUI.alignment;
                
                // Hide the original UI text component to avoid rendering both.
                textCompUI.enabled = false;
            }
        }
        
        private void UpdateDistanceScaling()
        {
            if (_vrCamera == null) return;
            float distance = Vector3.Distance(transform.position, _vrCamera.transform.position);
            float scaleFactor = Mathf.Clamp(distance / optimalViewingDistance, minScale, maxScale);
            transform.localScale = _originalScale * scaleFactor;
        }
        
        private void UpdateBillboard()
        {
            if (_vrCamera == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _vrCamera.transform.position, _vrCamera.transform.up);
        }
    }
}
#endif