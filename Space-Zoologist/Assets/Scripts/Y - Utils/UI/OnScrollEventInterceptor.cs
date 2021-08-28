using UnityEngine;
using UnityEngine.EventSystems;

public class OnScrollEventInterceptor : MonoBehaviour, IScrollHandler
{
    [SerializeField]
    [Tooltip("Reference to the game object to intercept the scroll event for. " +
        "This game object must have a component that implements IScrollHandler attached")]
    private GameObject interceptTargetObject;

    private IScrollHandler interceptTarget = null;
    public IScrollHandler InterceptTarget
    {
        get
        {
            // Try to get an object of the intercept type
            if (interceptTarget == null) interceptTarget = interceptTargetObject.GetComponent<IScrollHandler>();
            // If the target is still null after trying to get it, then throw an exception
            if (interceptTarget == null) throw new MissingComponentException(
                "Expected a component of type IScrollHandler on GameObject named " 
                + interceptTargetObject.name 
                + " but found none");
            return interceptTarget;
        }
        set => interceptTarget = value;
    }

    // Throw the event back to the intercept target
    public void OnScroll(PointerEventData data)
    {
        InterceptTarget.OnScroll(data);
    }
}
