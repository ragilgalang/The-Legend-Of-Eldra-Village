namespace Eldara.AI.BehaviourTree
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    /// <summary>
    /// Base class untuk semua node dalam Behaviour Tree.
    /// </summary>
    public abstract class BTNode
    {
        public NodeState State { get; protected set; }

        public abstract NodeState Evaluate();
    }
}
