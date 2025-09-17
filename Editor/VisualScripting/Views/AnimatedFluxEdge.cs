using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// A custom GraphView Edge that can play a visual "flow" animation.
/// The animation consists of a moving arrow and a temporary color change,
/// which are guaranteed to be synchronized.
/// 
/// INTENDED USAGE from the GraphView:
/// 1. OnTokenTraverse finds the corresponding edge(s).
/// 2. It checks the 'Launched' property.
/// 3. If 'Launched' is false, it calls 'TriggerFlowAnimation()'.
/// 4. The 'ResetVisuals()' method should be called on all edges when execution stops (e.g., exiting Play Mode)
///    to reset the 'Launched' state for the next run.
/// </summary>
public class AnimatedFluxEdge : Edge
{
    private readonly VisualElement m_Arrow;
    private IVisualElementScheduledItem m_AnimationItem;
    
    // A single state flag that controls all active visual effects (arrow and color).
    private bool _isAnimating = false;
    
    /// <summary>
    /// Tracks if the flow animation has been triggered during the current execution.
    /// This is reset to false by calling ResetVisuals().
    /// </summary>
    public bool Launched { get; private set; } = false;

    private readonly Color _defaultColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
    private readonly Color _flowingColor = new Color(0.3f, 0.8f, 1f, 1f);

    public float Speed { get; set; } = 150f;
    private const int ArrowSize = 34;

    private float _totalPathLength;

    public AnimatedFluxEdge()
    {
        m_Arrow = new VisualElement
        {
            name = "arrow",
            style =
            {
                width = ArrowSize,
                height = ArrowSize,
                position = Position.Absolute,
                visibility = Visibility.Hidden
            }
        };

        var arrowIcon = EditorGUIUtility.IconContent("d_tab_next@2x").image as Texture2D;
        if (arrowIcon != null)
        {
            m_Arrow.style.backgroundImage = new StyleBackground(arrowIcon);
        }
        else
        {
            m_Arrow.style.backgroundColor = new StyleColor(Color.red);
        }
        m_Arrow.style.unityBackgroundImageTintColor = new StyleColor(Color.cyan);

        Add(m_Arrow);
        m_Arrow.BringToFront();

        edgeControl.RegisterCallback<GeometryChangedEvent>(OnEdgeControlGeometryChanged);
    }

    private void OnEdgeControlGeometryChanged(GeometryChangedEvent evt)
    {
        // Pre-calculate the total path length whenever the edge geometry changes.
        _totalPathLength = 0;
        if (edgeControl != null && edgeControl.controlPoints != null && edgeControl.controlPoints.Length > 1)
        {
            for (int i = 0; i < edgeControl.controlPoints.Length - 1; i++)
            {
                _totalPathLength += Vector2.Distance(edgeControl.controlPoints[i], edgeControl.controlPoints[i + 1]);
            }
        }
        if (_isAnimating) ResetVisuals();
    }

    public void TriggerFlowAnimation()
    {
        // The _isAnimating flag acts as a lock to prevent spamming the animation.
        if (_isAnimating || _totalPathLength <= 0) return;
        
        // Engage the lock, update the state, and mark this edge as launched.
        _isAnimating = true;
        Launched = true;
        
        // --- DIRECT ACTION for Color ---
        // We apply the color directly here to guarantee an immediate visual update,
        // bypassing any potential delays from the repaint cycle.
        SetEdgeColor(_flowingColor);

        // Start the single animation scheduler, which is now the "master clock".
        var animationStartTime = EditorApplication.timeSinceStartup;
        m_Arrow.style.visibility = Visibility.Visible;
        m_AnimationItem = schedule.Execute(() => UpdateAnimation(animationStartTime)).Every(16);
    }

    private void UpdateAnimation(double startTime)
    {
        double timeElapsed = EditorApplication.timeSinceStartup - startTime;
        float distanceToTravel = (float)timeElapsed * Speed;

        // If the animation is finished...
        if (distanceToTravel >= _totalPathLength)
        {
            // ...place the arrow exactly at the end...
            UpdateArrowPosition(_totalPathLength); 
            // ...and call the single, unified stop method.
            StopAnimation();
        }
        else
        {
            UpdateArrowPosition(distanceToTravel);
        }
    }
    
    /// <summary>
    /// This method remains as a "state guardian".
    /// If the UI repaints for any reason, it ensures the edge color
    /// correctly reflects the _isAnimating state.
    /// </summary>
    public override bool UpdateEdgeControl()
    {
        base.UpdateEdgeControl();
        Color targetColor = _isAnimating ? _flowingColor : _defaultColor;
        SetEdgeColor(targetColor);
        return true;
    }

    /// <summary>
    /// A single method to stop ALL active visual effects.
    /// This ensures perfect synchronization between the arrow and the color.
    /// </summary>
    private void StopAnimation()
    {
        // Stop the animation scheduler.
        m_AnimationItem?.Pause();
        m_AnimationItem = null;
        
        // Hide the arrow.
        m_Arrow.style.visibility = Visibility.Hidden;
        
        // Release the animation lock / reset the state.
        _isAnimating = false;
        
        // --- DIRECT ACTION for Color Reset ---
        // We also apply the default color directly here for a guaranteed reset.
        SetEdgeColor(_defaultColor);
    }

    /// <summary>
    /// Public reset method called externally.
    /// This fully resets the edge to its initial state for the next execution.
    /// </summary>
    public void ResetVisuals()
    {
        StopAnimation();
        // Also reset the Launched flag.
        Launched = false;
    }

    /// <summary>
    /// Applies a solid color to the edge line.
    /// </summary>
    private void SetEdgeColor(Color newColor)
    {
        if (edgeControl != null)
        {
            edgeControl.inputColor = newColor;
            edgeControl.outputColor = newColor;
        }
    }
    
    /// <summary>
    /// Finds the correct segment and position for a given traveled distance.
    /// </summary>
    private void UpdateArrowPosition(float distance)
    {
        if (edgeControl == null || edgeControl.controlPoints == null || edgeControl.controlPoints.Length < 2) return;

        float pathLengthTraversed = 0f;
        for (int i = 0; i < edgeControl.controlPoints.Length - 1; i++)
        {
            Vector2 startPoint = edgeControl.controlPoints[i];
            Vector2 endPoint = edgeControl.controlPoints[i + 1];
            float segmentLength = Vector2.Distance(startPoint, endPoint);

            if (distance <= pathLengthTraversed + segmentLength)
            {
                float distanceIntoSegment = distance - pathLengthTraversed;
                float progress = segmentLength > 0 ? distanceIntoSegment / segmentLength : 0;
                
                Vector2 position = Vector2.Lerp(startPoint, endPoint, progress);
                Vector2 tangent = (endPoint - startPoint).normalized;
                
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                m_Arrow.transform.position = position - new Vector2(ArrowSize / 2f, ArrowSize / 2f);
                m_Arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
                
                return;
            }

            pathLengthTraversed += segmentLength;
        }
    }
}