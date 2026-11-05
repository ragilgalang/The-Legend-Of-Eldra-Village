using System;
using System.Collections.Generic;

namespace Eldara.AI.BehaviourTree
{
    /// <summary>
    /// Node Selector (OR Gate): Mengevaluasi child satu per satu dari kiri ke kanan.
    /// Berhenti dan mengembalikan SUCCESS jika ada satu child yang SUCCESS.
    /// Mengembalikan RUNNING jika ada child yang RUNNING.
    /// Mengembalikan FAILURE hanya jika SEMUA child menghasilkan FAILURE.
    /// </summary>
    public class BTSelector : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();

        public BTSelector(params BTNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public void AddChild(BTNode node) => children.Add(node);

        public override NodeState Evaluate()
        {
            foreach (var node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.RUNNING:
                        State = NodeState.RUNNING;
                        return State;
                    case NodeState.SUCCESS:
                        State = NodeState.SUCCESS;
                        return State;
                    case NodeState.FAILURE:
                        continue;
                }
            }

            State = NodeState.FAILURE;
            return State;
        }
    }

    /// <summary>
    /// Node Sequence (AND Gate): Mengevaluasi child berurutan satu per satu.
    /// Mengembalikan FAILURE seketika jika ada satu child yang FAILURE.
    /// Mengembalikan RUNNING jika ada child yang masih RUNNING.
    /// Mengembalikan SUCCESS hanya jika SEMUA child berhasil (SUCCESS).
    /// </summary>
    public class BTSequence : BTNode
    {
        protected List<BTNode> children = new List<BTNode>();

        public BTSequence(params BTNode[] nodes)
        {
            children.AddRange(nodes);
        }

        public void AddChild(BTNode node) => children.Add(node);

        public override NodeState Evaluate()
        {
            bool anyRunning = false;

            foreach (var node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        State = NodeState.FAILURE;
                        return State;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        anyRunning = true;
                        continue;
                }
            }

            State = anyRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return State;
        }
    }

    /// <summary>
    /// Node Decorator: Membalikkan hasil child node (SUCCESS jadi FAILURE, FAILURE jadi SUCCESS).
    /// </summary>
    public class BTInverter : BTNode
    {
        protected BTNode child;

        public BTInverter(BTNode childNode)
        {
            child = childNode;
        }

        public override NodeState Evaluate()
        {
            switch (child.Evaluate())
            {
                case NodeState.RUNNING:
                    State = NodeState.RUNNING;
                    return State;
                case NodeState.SUCCESS:
                    State = NodeState.FAILURE;
                    return State;
                case NodeState.FAILURE:
                    State = NodeState.SUCCESS;
                    return State;
            }

            State = NodeState.SUCCESS;
            return State;
        }
    }

    /// <summary>
    /// Leaf Node Action: Mengeksekusi fungsi/aksi nyata (berjalan, animasi, dsb).
    /// </summary>
    public class BTAction : BTNode
    {
        private readonly Func<NodeState> action;

        public BTAction(Func<NodeState> actionFunc)
        {
            action = actionFunc;
        }

        public override NodeState Evaluate()
        {
            State = action != null ? action.Invoke() : NodeState.FAILURE;
            return State;
        }
    }

    /// <summary>
    /// Leaf Node Condition: Mengecek kondisi logika (misal: apakah malam? apakah pemain dekat?).
    /// Mengembalikan SUCCESS jika kondisi bernilai true, dan FAILURE jika false.
    /// </summary>
    public class BTCondition : BTNode
    {
        private readonly Func<bool> condition;

        public BTCondition(Func<bool> conditionFunc)
        {
            condition = conditionFunc;
        }

        public override NodeState Evaluate()
        {
            bool result = condition != null && condition.Invoke();
            State = result ? NodeState.SUCCESS : NodeState.FAILURE;
            return State;
        }
    }
}
