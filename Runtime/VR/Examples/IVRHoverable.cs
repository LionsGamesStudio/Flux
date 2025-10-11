namespace FluxFramework.VR
{
    /// <summary>
    /// Defines a contract for objects that can react to being hovered over by a VR interactor.
    /// </summary>
    public interface IVRHoverable
    {
        /// <summary>
        /// Called by an interactor when its pointer begins hovering over this object.
        /// </summary>
        /// <param name="controller">The controller that is now hovering.</param>
        void OnHoverEnter(FluxVRController controller);

        /// <summary>
        /// Called by an interactor when its pointer stops hovering over this object.
        /// </summary>
        /// <param name="controller">The controller that was previously hovering.</param>
        void OnHoverExit(FluxVRController controller);
    }
}